using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    public class ObdService : IObdService
    {
        private SerialPort? _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private ObdConnection? _connection;
        private readonly Random _rng = new();

        public bool IsConnected =>
            _port?.IsOpen == true ||
            (_tcpClient?.Connected == true && _networkStream != null) ||
            (IsSimulationMode && _connection?.IsConnected == true);

        public bool IsSimulationMode { get; set; } = false;
        public ObdConnection? CurrentConnection => _connection;

        public async Task<bool> ConnectAsync(ObdConnection connection)
        {
            _connection = connection;
            if (IsSimulationMode)
            {
                connection.IsConnected = true;
                connection.ElmVersion = "ELM327 v2.1 (Simulated)";
                connection.DeviceName = "OBD2Suite Simulator";
                await Task.Delay(500);
                return true;
            }

            return connection.Type switch
            {
                ConnectionType.WiFi      => await ConnectNetworkAsync(connection),
                ConnectionType.Bluetooth => await ConnectBluetoothAsync(connection),
                _                        => await ConnectSerialAsync(connection)
            };
        }

        // ── Serial / USB connection ──────────────────────────────────────────

        private async Task<bool> ConnectSerialAsync(ObdConnection connection)
        {
            try
            {
                _port = new SerialPort(connection.PortName, connection.BaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout  = connection.Timeout,
                    WriteTimeout = connection.Timeout,
                    NewLine      = "\r"
                };
                _port.Open();

                await InitElmAsync(connection);
                connection.IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _port?.Close();
                _port = null;
                connection.IsConnected = false;
                throw new InvalidOperationException($"Serial connection failed: {ex.Message}", ex);
            }
        }

        // ── WiFi / Network (TCP) connection ─────────────────────────────────

        private async Task<bool> ConnectNetworkAsync(ObdConnection connection)
        {
            try
            {
                _tcpClient = new TcpClient
                {
                    SendTimeout    = connection.Timeout,
                    ReceiveTimeout = connection.Timeout
                };
                using var cts = new CancellationTokenSource(connection.Timeout);
                await _tcpClient.ConnectAsync(connection.IpAddress, connection.NetworkPort, cts.Token);
                _networkStream = _tcpClient.GetStream();

                await InitElmAsync(connection);
                connection.IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _networkStream?.Dispose();
                _networkStream = null;
                _tcpClient?.Dispose();
                _tcpClient = null;
                connection.IsConnected = false;
                throw new InvalidOperationException(
                    $"Network connection to {connection.IpAddress}:{connection.NetworkPort} failed: {ex.Message}", ex);
            }
        }

        // ── Bluetooth connection ─────────────────────────────────────────────

        private async Task<bool> ConnectBluetoothAsync(ObdConnection connection)
        {
            // Many Bluetooth OBD adapters are paired as virtual COM ports.
            if (!string.IsNullOrWhiteSpace(connection.PortName))
            {
                // Reuse serial path — the virtual COM port IS the BT connection.
                try
                {
                    _port = new SerialPort(connection.PortName, 38400, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout  = connection.Timeout,
                        WriteTimeout = connection.Timeout,
                        NewLine      = "\r"
                    };
                    _port.Open();

                    await InitElmAsync(connection);
                    connection.IsConnected = true;
                    return true;
                }
                catch (Exception ex)
                {
                    _port?.Close();
                    _port = null;
                    connection.IsConnected = false;
                    throw new InvalidOperationException(
                        $"Bluetooth (virtual COM port {connection.PortName}) failed: {ex.Message}", ex);
                }
            }

            // Direct Bluetooth RFCOMM socket (address must be set).
            if (string.IsNullOrWhiteSpace(connection.BluetoothAddress))
                throw new InvalidOperationException(
                    "No Bluetooth address or virtual COM port configured.");

            return await ConnectBluetoothRfcommAsync(connection);
        }

        /// <summary>
        /// Opens a Bluetooth RFCOMM socket directly using the Windows Winsock
        /// AF_BTH address family (value 32) and the RFCOMM protocol (value 3).
        /// The remote end-point is encoded as a custom <see cref="SocketAddress"/>.
        /// </summary>
        private async Task<bool> ConnectBluetoothRfcommAsync(ObdConnection connection)
        {
            const AddressFamily AfBluetooth   = (AddressFamily)32;
            const System.Net.Sockets.ProtocolType RfcommProtocol = (System.Net.Sockets.ProtocolType)3;

            // Parse MAC (12 hex chars, any separator) → UInt64 little-endian
            var mac = connection.BluetoothAddress.Replace(":", "").Replace("-", "").Trim();
            if (mac.Length != 12 || !TryParseBtAddress(mac, out var btAddr))
                throw new InvalidOperationException(
                    $"Invalid Bluetooth address: '{connection.BluetoothAddress}'");

            // SPP service GUID: 00001101-0000-1000-8000-00805F9B34FB
            var sppGuid = new Guid("00001101-0000-1000-8000-00805F9B34FB");

            // Build sockaddr_bth (30 bytes):
            // [0..1]  = AF_BTH (32, little-endian)
            // [2..9]  = BT address (UInt64, little-endian)
            // [10..25]= service class GUID
            // [26..29]= port/channel (0 = auto-negotiate via SDP)
            var sa = new SocketAddress(AfBluetooth, 30);
            var addrBytes = BitConverter.GetBytes(btAddr);
            for (int i = 0; i < 8; i++) sa[2 + i] = addrBytes[i];
            var guidBytes = sppGuid.ToByteArray();
            for (int i = 0; i < 16; i++) sa[10 + i] = guidBytes[i];
            // Port bytes [26..29] remain 0 (let the stack negotiate the channel).

            var socket = new Socket(AfBluetooth, SocketType.Stream, RfcommProtocol);
            try
            {
                await Task.Run(() => socket.Connect(new BluetoothRfcommEndPoint(btAddr, sppGuid, sa)));

                // Wrap socket in a NetworkStream so we can reuse the stream I/O path.
                _networkStream = new NetworkStream(socket, ownsSocket: true);
                _tcpClient     = null;  // not used for raw socket path

                await InitElmAsync(connection);
                connection.IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                socket.Dispose();
                _networkStream = null;
                connection.IsConnected = false;
                throw new InvalidOperationException(
                    $"Bluetooth RFCOMM connection to {connection.BluetoothAddress} failed: {ex.Message}", ex);
            }
        }

        private static bool TryParseBtAddress(string hex, out ulong address)
        {
            address = 0;
            if (hex.Length != 12) return false;
            try
            {
                // Bluetooth addresses are 48-bit (6 bytes).  We store them in the
                // lower 6 bytes of a little-endian UInt64; bytes[6] and bytes[7]
                // remain 0 intentionally.
                var bytes = new byte[8];
                for (int i = 0; i < 6; i++)
                    bytes[i] = Convert.ToByte(hex.Substring((5 - i) * 2, 2), 16);
                address = BitConverter.ToUInt64(bytes, 0);
                return true;
            }
            catch { return false; }
        }

        // ── ELM327 initialisation (shared by all transports) ────────────────

        private async Task InitElmAsync(ObdConnection connection)
        {
            await SendRawAsync(ElmProtocol.ATZ);
            await Task.Delay(1000);
            if (_port != null) _port.DiscardInBuffer();

            var atzResp = await SendCommandAsync(ElmProtocol.ATZ, 2000);
            connection.ElmVersion = atzResp.Contains("ELM") ? atzResp.Trim() : "Unknown";

            await SendCommandAsync(ElmProtocol.ATE0);
            await SendCommandAsync(ElmProtocol.ATL0);
            await SendCommandAsync(ElmProtocol.ATH1);
            await SendCommandAsync(ElmProtocol.ATSP0);
            await SendCommandAsync(ElmProtocol.ATAT1);
        }

        // ── Disconnect ───────────────────────────────────────────────────────

        public async Task DisconnectAsync()
        {
            if (_connection != null) _connection.IsConnected = false;
            try
            {
                if (IsConnected)
                    await SendCommandAsync(ElmProtocol.ATPC);
            }
            catch { }

            _port?.Close();
            _port?.Dispose();
            _port = null;

            _networkStream?.Dispose();
            _networkStream = null;
            _tcpClient?.Dispose();
            _tcpClient = null;
        }

        // ── SendCommand / raw I/O ────────────────────────────────────────────

        public async Task<string> SendCommandAsync(string command, int timeoutMs = 2000)
        {
            if (IsSimulationMode) return await SimulateCommandAsync(command);

            if (_port != null && _port.IsOpen)
            {
                await SendRawAsync(command);
                return await ReadResponseAsync(timeoutMs);
            }
            if (_networkStream != null)
            {
                await SendRawStreamAsync(command);
                return await ReadResponseStreamAsync(timeoutMs);
            }
            throw new InvalidOperationException("Not connected");
        }

        private async Task SendRawAsync(string command)
        {
            var bytes = Encoding.ASCII.GetBytes(command + "\r");
            if (_port != null)
                await Task.Run(() => _port.Write(bytes, 0, bytes.Length));
            else if (_networkStream != null)
                await _networkStream.WriteAsync(bytes);
        }

        private async Task SendRawStreamAsync(string command)
        {
            if (_networkStream == null) return;
            var bytes = Encoding.ASCII.GetBytes(command + "\r");
            await _networkStream.WriteAsync(bytes);
        }

        private async Task<string> ReadResponseAsync(int timeoutMs)
        {
            if (_port == null) return "";
            var sb = new StringBuilder();
            var cts = new CancellationTokenSource(timeoutMs);
            await Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int ch = _port.ReadChar();
                        if (ch == '>')
                            break;
                        sb.Append((char)ch);
                    }
                }
                catch { }
            }, cts.Token);
            return sb.ToString();
        }

        private async Task<string> ReadResponseStreamAsync(int timeoutMs)
        {
            if (_networkStream == null) return "";
            var sb    = new StringBuilder();
            var buf   = new byte[256];
            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    int n = await _networkStream.ReadAsync(buf, cts.Token);
                    if (n == 0) break;
                    for (int i = 0; i < n; i++)
                    {
                        char ch = (char)buf[i];
                        if (ch == '>') return sb.ToString();
                        sb.Append(ch);
                    }
                }
            }
            catch { }
            return sb.ToString();
        }

        public async Task<List<DiagnosticTroubleCode>> ReadDtcsAsync()
        {
            if (IsSimulationMode) return await SimulateReadDtcsAsync();
            var dtcs = new List<DiagnosticTroubleCode>();
            var modes = new[] { (ElmProtocol.MODE_DTC, "Engine"), (ElmProtocol.MODE_PENDING_DTC, "Engine"), (ElmProtocol.MODE_PERMANENT_DTC, "Engine") };
            foreach (var (mode, ecu) in modes)
            {
                var resp = await SendCommandAsync($"{mode:X2}");
                if (ElmProtocol.IsError(resp)) continue;
                var codes = ElmProtocol.ParseDtcResponse(resp);
                foreach (var code in codes)
                {
                    dtcs.Add(new DiagnosticTroubleCode
                    {
                        Code = code,
                        Description = DtcDatabase.GetDescription(code),
                        EcuSource = ecu,
                        Category = DiagnosticTroubleCode.GetCategory(code),
                        Severity = DiagnosticTroubleCode.GetSeverity(code),
                        Status = mode == ElmProtocol.MODE_PENDING_DTC ? DtcStatus.Pending :
                                 mode == ElmProtocol.MODE_PERMANENT_DTC ? DtcStatus.Permanent : DtcStatus.Confirmed,
                        FreezeFrame = await ReadFreezeFrameAsync(code)
                    });
                }
            }
            return dtcs;
        }

        private async Task<FreezeFrameData?> ReadFreezeFrameAsync(string dtcCode)
        {
            try
            {
                var resp = await SendCommandAsync($"02{ElmProtocol.PID_ENGINE_RPM:X2}00");
                if (ElmProtocol.IsError(resp)) return null;
                return new FreezeFrameData
                {
                    EngineRpm = 800,
                    VehicleSpeed = 0,
                    CoolantTemp = 85,
                    ThrottlePosition = 0,
                    Timestamp = DateTime.Now
                };
            }
            catch { return null; }
        }

        public async Task<bool> ClearDtcsAsync()
        {
            if (IsSimulationMode) { await Task.Delay(500); return true; }
            var resp = await SendCommandAsync($"{ElmProtocol.MODE_CLEAR_DTC:X2}");
            return !ElmProtocol.IsError(resp);
        }

        public async Task<List<LiveDataParameter>> ReadLiveDataAsync()
        {
            if (IsSimulationMode) return await SimulateReadLiveDataAsync();
            var result = new List<LiveDataParameter>();
            foreach (var kvp in ElmProtocol.PidDefinitions)
            {
                var pid = kvp.Key;
                var (name, unit, min, max, formula, cat) = kvp.Value;
                var cmd = ElmProtocol.BuildCommand(ElmProtocol.MODE_CURRENT_DATA, pid);
                var resp = await SendCommandAsync(cmd);
                if (ElmProtocol.IsError(resp)) continue;
                var bytes = ElmProtocol.ParseDataBytes(resp, ElmProtocol.MODE_CURRENT_DATA, pid);
                if (bytes == null || bytes.Length == 0) continue;
                double value = CalculatePidValue(pid, bytes);
                result.Add(new LiveDataParameter
                {
                    Name = name,
                    PidHex = $"0x{pid:X2}",
                    PidByte = pid,
                    Value = value,
                    Unit = unit,
                    MinValue = min,
                    MaxValue = max,
                    FormulaDescription = formula,
                    Category = (Models.PidCategory)(int)cat,
                    IsSupported = true
                });
            }
            return result;
        }

        private double CalculatePidValue(byte pid, byte[] data)
        {
            double a = data.Length > 0 ? data[0] : 0;
            double b = data.Length > 1 ? data[1] : 0;
            double c = data.Length > 2 ? data[2] : 0;
            double d = data.Length > 3 ? data[3] : 0;
            return pid switch
            {
                ElmProtocol.PID_CALC_ENGINE_LOAD => a * 100.0 / 255.0,
                ElmProtocol.PID_COOLANT_TEMP => a - 40,
                ElmProtocol.PID_SHORT_TERM_FUEL_TRIM_1 or ElmProtocol.PID_LONG_TERM_FUEL_TRIM_1 => (a - 128) * 100.0 / 128.0,
                ElmProtocol.PID_FUEL_PRESSURE => a * 3,
                ElmProtocol.PID_INTAKE_MAP => a,
                ElmProtocol.PID_ENGINE_RPM => (256 * a + b) / 4.0,
                ElmProtocol.PID_VEHICLE_SPEED => a,
                ElmProtocol.PID_TIMING_ADVANCE => a / 2.0 - 64,
                ElmProtocol.PID_INTAKE_AIR_TEMP or ElmProtocol.PID_AMBIENT_AIR_TEMP => a - 40,
                ElmProtocol.PID_MAF_FLOW => (256 * a + b) / 100.0,
                ElmProtocol.PID_THROTTLE_POS or ElmProtocol.PID_RELATIVE_THROTTLE => a * 100.0 / 255.0,
                ElmProtocol.PID_RUNTIME_SINCE_START => 256 * a + b,
                ElmProtocol.PID_DISTANCE_WITH_MIL => 256 * a + b,
                ElmProtocol.PID_FUEL_LEVEL => a * 100.0 / 255.0,
                ElmProtocol.PID_BARO_PRESSURE => a,
                ElmProtocol.PID_CONTROL_MODULE_VOLTAGE => (256 * a + b) / 1000.0,
                ElmProtocol.PID_ENGINE_OIL_TEMP => a - 40,
                ElmProtocol.PID_HYBRID_BATTERY_REMAINING => a * 100.0 / 255.0,
                _ => a
            };
        }

        public async Task<VehicleInfo> ReadVehicleInfoAsync()
        {
            if (IsSimulationMode) return await SimulateReadVehicleInfoAsync();
            var info = new VehicleInfo();
            var vinResp = await SendCommandAsync($"09{ElmProtocol.INFO_VIN:X2}");
            if (!ElmProtocol.IsError(vinResp))
            {
                info.Vin = ParseVin(vinResp);
                DecodeVin(info);
            }
            var calResp = await SendCommandAsync($"09{ElmProtocol.INFO_CALIBRATION_ID:X2}");
            if (!ElmProtocol.IsError(calResp)) info.CalibrationId = ExtractAscii(calResp);
            var ecuResp = await SendCommandAsync($"09{ElmProtocol.INFO_ECU_NAME:X2}");
            if (!ElmProtocol.IsError(ecuResp)) info.EcuName = ExtractAscii(ecuResp);
            info.EcuModules = await ReadEcuModulesAsync();
            return info;
        }

        private string ParseVin(string response)
        {
            var clean = ElmProtocol.CleanResponse(response);
            var sb = new StringBuilder();
            var parts = clean.Split(' ');
            bool skipMode = false;
            foreach (var p in parts)
            {
                if (!skipMode && (p == "49" || p == "02")) { skipMode = true; continue; }
                if (byte.TryParse(p, System.Globalization.NumberStyles.HexNumber, null, out byte b) && b >= 0x20 && b <= 0x7E)
                    sb.Append((char)b);
            }
            return sb.ToString();
        }

        private string ExtractAscii(string response)
        {
            var clean = ElmProtocol.CleanResponse(response);
            var sb = new StringBuilder();
            foreach (var p in clean.Split(' '))
            {
                if (byte.TryParse(p, System.Globalization.NumberStyles.HexNumber, null, out byte b) && b >= 0x20 && b <= 0x7E)
                    sb.Append((char)b);
            }
            return sb.ToString().Trim();
        }

        private void DecodeVin(VehicleInfo info)
        {
            if (info.Vin.Length < 3) return;
            info.Year = info.Vin.Length >= 10 ? VinYearDecode(info.Vin[9]) : 0;
        }

        private int VinYearDecode(char c) => c switch
        {
            'A' => 1980, 'B' => 1981, 'C' => 1982, 'D' => 1983, 'E' => 1984, 'F' => 1985, 'G' => 1986,
            'H' => 1987, 'J' => 1988, 'K' => 1989, 'L' => 1990, 'M' => 1991, 'N' => 1992, 'P' => 1993,
            'R' => 1994, 'S' => 1995, 'T' => 1996, 'V' => 1997, 'W' => 1998, 'X' => 1999, 'Y' => 2000,
            '1' => 2001, '2' => 2002, '3' => 2003, '4' => 2004, '5' => 2005, '6' => 2006, '7' => 2007,
            '8' => 2008, '9' => 2009, _ => 2010
        };

        public async Task<List<EcuModule>> ReadEcuModulesAsync()
        {
            if (IsSimulationMode) return await SimulateReadEcuModulesAsync();
            return new List<EcuModule>
            {
                new EcuModule { Address = 0x7E0, Name = "Engine Control Module", Protocol = "ISO 15765-4 CAN", IsReachable = true },
                new EcuModule { Address = 0x7E1, Name = "Transmission Control Module", Protocol = "ISO 15765-4 CAN", IsReachable = true }
            };
        }

        public async Task<string> ReadEcuCodingAsync(int ecuAddress, int dataIdentifier)
        {
            if (IsSimulationMode) return await Task.FromResult($"{_rng.Next(0, 0xFFFF):X4}");
            var cmd = $"22{dataIdentifier:X4}";
            return await SendCommandAsync(cmd);
        }

        public async Task<bool> WriteEcuCodingAsync(int ecuAddress, int dataIdentifier, byte[] data)
        {
            if (IsSimulationMode) { await Task.Delay(300); return true; }
            var dataHex = BitConverter.ToString(data).Replace("-", "");
            var cmd = $"2E{dataIdentifier:X4}{dataHex}";
            var resp = await SendCommandAsync(cmd);
            return resp.Contains("6E") && !ElmProtocol.IsError(resp);
        }

        public async Task<string> ReadAdaptationAsync(int ecuAddress, int channelNumber)
        {
            if (IsSimulationMode) return await Task.FromResult(_rng.Next(0, 255).ToString());
            var cmd = $"21{channelNumber:X2}";
            return await SendCommandAsync(cmd);
        }

        public async Task<bool> WriteAdaptationAsync(int ecuAddress, int channelNumber, string value)
        {
            if (IsSimulationMode) { await Task.Delay(300); return true; }
            var cmd = $"2A{channelNumber:X2}{value}";
            var resp = await SendCommandAsync(cmd);
            return !ElmProtocol.IsError(resp);
        }

        // ---- Simulation Methods ----

        private async Task<string> SimulateCommandAsync(string command)
        {
            await Task.Delay(50 + _rng.Next(0, 100));
            var cmd = command.ToUpperInvariant().Trim();
            if (cmd == ElmProtocol.ATZ || cmd == "ATZ") return "ELM327 v2.1";
            if (cmd.StartsWith("AT")) return "OK";
            if (cmd == "0100") return "41 00 BE 3F A8 11";
            if (cmd == "0101") return "41 01 00 07 E5 00";
            if (cmd == "0104") return $"41 04 {(byte)(_rng.NextDouble() * 100):X2}";
            if (cmd == "0105") return $"41 05 {(byte)(80 + _rng.Next(-5, 15) + 40):X2}";
            if (cmd == "010C") { int rpm = 800 + _rng.Next(0, 4000); return $"41 0C {(rpm * 4 / 256):X2} {(rpm * 4 % 256):X2}"; }
            if (cmd == "010D") return $"41 0D {_rng.Next(0, 120):X2}";
            if (cmd == "010F") return $"41 0F {(byte)(25 + 40):X2}";
            if (cmd == "0110") { int maf = _rng.Next(200, 2000); return $"41 10 {maf / 256:X2} {maf % 256:X2}"; }
            if (cmd == "0111") return $"41 11 {(byte)(_rng.NextDouble() * 100 * 2.55):X2}";
            if (cmd == "011F") { int t = _rng.Next(0, 3600); return $"41 1F {t / 256:X2} {t % 256:X2}"; }
            if (cmd == "012F") return $"41 2F {(byte)(_rng.NextDouble() * 100 * 2.55):X2}";
            if (cmd == "0142") { int v = (int)(14.4 * 1000); return $"41 42 {v / 256:X2} {v % 256:X2}"; }
            if (cmd == "0146") return $"41 46 {(byte)(22 + 40):X2}";
            if (cmd == "015C") return $"41 5C {(byte)(95 + 40):X2}";
            return "NO DATA";
        }

        private async Task<List<DiagnosticTroubleCode>> SimulateReadDtcsAsync()
        {
            await Task.Delay(800);
            return new List<DiagnosticTroubleCode>
            {
                new DiagnosticTroubleCode
                {
                    Code = "P0171",
                    Description = DtcDatabase.GetDescription("P0171"),
                    EcuSource = "Engine",
                    Severity = DtcSeverity.Moderate,
                    Status = DtcStatus.Confirmed,
                    Category = DtcCategory.Powertrain,
                    IsMilOn = true,
                    FreezeFrame = new FreezeFrameData { EngineRpm = 2200, VehicleSpeed = 65, CoolantTemp = 90, ThrottlePosition = 45 }
                },
                new DiagnosticTroubleCode
                {
                    Code = "P0300",
                    Description = DtcDatabase.GetDescription("P0300"),
                    EcuSource = "Engine",
                    Severity = DtcSeverity.Severe,
                    Status = DtcStatus.Confirmed,
                    Category = DtcCategory.Powertrain,
                    IsMilOn = true
                },
                new DiagnosticTroubleCode
                {
                    Code = "P0420",
                    Description = DtcDatabase.GetDescription("P0420"),
                    EcuSource = "Emissions",
                    Severity = DtcSeverity.Moderate,
                    Status = DtcStatus.Pending,
                    Category = DtcCategory.Powertrain,
                    IsMilOn = false
                },
                new DiagnosticTroubleCode
                {
                    Code = "C0031",
                    Description = DtcDatabase.GetDescription("C0031"),
                    EcuSource = "ABS",
                    Severity = DtcSeverity.Severe,
                    Status = DtcStatus.Confirmed,
                    Category = DtcCategory.Chassis
                },
                new DiagnosticTroubleCode
                {
                    Code = "B0010",
                    Description = DtcDatabase.GetDescription("B0010"),
                    EcuSource = "Airbag",
                    Severity = DtcSeverity.Critical,
                    Status = DtcStatus.Confirmed,
                    Category = DtcCategory.Body
                }
            };
        }

        private async Task<List<LiveDataParameter>> SimulateReadLiveDataAsync()
        {
            await Task.Delay(200);
            return new List<LiveDataParameter>
            {
                new LiveDataParameter { Name = "Engine RPM", PidHex = "0x0C", Value = 800 + _rng.Next(0, 3200), Unit = "rpm", MinValue = 0, MaxValue = 8000, Category = Models.PidCategory.Engine },
                new LiveDataParameter { Name = "Vehicle Speed", PidHex = "0x0D", Value = _rng.Next(0, 120), Unit = "km/h", MinValue = 0, MaxValue = 255, Category = Models.PidCategory.Speed },
                new LiveDataParameter { Name = "Coolant Temperature", PidHex = "0x05", Value = 85 + _rng.Next(-5, 10), Unit = "°C", MinValue = -40, MaxValue = 215, Category = Models.PidCategory.Temperature },
                new LiveDataParameter { Name = "Engine Load", PidHex = "0x04", Value = 20 + _rng.NextDouble() * 60, Unit = "%", MinValue = 0, MaxValue = 100, Category = Models.PidCategory.Engine },
                new LiveDataParameter { Name = "Throttle Position", PidHex = "0x11", Value = _rng.NextDouble() * 100, Unit = "%", MinValue = 0, MaxValue = 100, Category = Models.PidCategory.Engine },
                new LiveDataParameter { Name = "MAF Flow Rate", PidHex = "0x10", Value = 5 + _rng.NextDouble() * 20, Unit = "g/s", MinValue = 0, MaxValue = 655, Category = Models.PidCategory.Engine },
                new LiveDataParameter { Name = "Fuel Level", PidHex = "0x2F", Value = 45 + _rng.NextDouble() * 10, Unit = "%", MinValue = 0, MaxValue = 100, Category = Models.PidCategory.Fuel },
                new LiveDataParameter { Name = "Intake Air Temp", PidHex = "0x0F", Value = 25 + _rng.Next(-5, 10), Unit = "°C", MinValue = -40, MaxValue = 215, Category = Models.PidCategory.Temperature },
                new LiveDataParameter { Name = "Timing Advance", PidHex = "0x0E", Value = 10 + _rng.NextDouble() * 20, Unit = "°", MinValue = -64, MaxValue = 63, Category = Models.PidCategory.Engine },
                new LiveDataParameter { Name = "Barometric Pressure", PidHex = "0x33", Value = 101 + _rng.Next(-3, 3), Unit = "kPa", MinValue = 0, MaxValue = 255, Category = Models.PidCategory.Pressure },
                new LiveDataParameter { Name = "Control Module Voltage", PidHex = "0x42", Value = 13.8 + _rng.NextDouble() * 0.6, Unit = "V", MinValue = 0, MaxValue = 20, Category = Models.PidCategory.Electrical },
                new LiveDataParameter { Name = "Ambient Air Temp", PidHex = "0x46", Value = 22 + _rng.Next(-5, 10), Unit = "°C", MinValue = -40, MaxValue = 215, Category = Models.PidCategory.Temperature },
                new LiveDataParameter { Name = "Engine Oil Temp", PidHex = "0x5C", Value = 95 + _rng.Next(-5, 15), Unit = "°C", MinValue = -40, MaxValue = 210, Category = Models.PidCategory.Temperature },
                new LiveDataParameter { Name = "Short Term Fuel Trim B1", PidHex = "0x06", Value = (_rng.NextDouble() * 10 - 5), Unit = "%", MinValue = -100, MaxValue = 99.2, Category = Models.PidCategory.Fuel },
                new LiveDataParameter { Name = "Long Term Fuel Trim B1", PidHex = "0x07", Value = (_rng.NextDouble() * 6 - 3), Unit = "%", MinValue = -100, MaxValue = 99.2, Category = Models.PidCategory.Fuel },
                new LiveDataParameter { Name = "O2 Sensor 1 Voltage", PidHex = "0x14", Value = 0.1 + _rng.NextDouble() * 0.9, Unit = "V", MinValue = 0, MaxValue = 1.275, Category = Models.PidCategory.Sensor },
                new LiveDataParameter { Name = "Intake Manifold Pressure", PidHex = "0x0B", Value = 30 + _rng.Next(0, 70), Unit = "kPa", MinValue = 0, MaxValue = 255, Category = Models.PidCategory.Pressure },
            };
        }

        private async Task<VehicleInfo> SimulateReadVehicleInfoAsync()
        {
            await Task.Delay(600);
            return new VehicleInfo
            {
                Vin = "1HGBH41JXMN109186",
                Make = "Honda",
                Model = "Civic",
                Year = 2021,
                EngineType = "1.5L DOHC VTEC Turbo",
                TransmissionType = "CVT",
                CalibrationId = "37805-5AA-A720",
                CalibrationVerificationNumber = "A1B2C3D4",
                EcuName = "Denso ECM",
                SoftwareVersion = "1.05.02",
                EcuModules = await SimulateReadEcuModulesAsync()
            };
        }

        private async Task<List<EcuModule>> SimulateReadEcuModulesAsync()
        {
            await Task.Delay(400);
            return new List<EcuModule>
            {
                new EcuModule
                {
                    Address = 0x7E0, Name = "Engine Control Module (ECM)", Protocol = "ISO 15765-4 CAN",
                    Variant = "Denso 37820-5AA", PartNumber = "37820-5AA-A040", SoftwareVersion = "1.05.02",
                    HardwareVersion = "1.00", CodingBytes = new byte[] { 0x01, 0x42, 0x00, 0x00 },
                    LongCodingString = "01420000", IsReachable = true,
                    AdaptationChannels = new List<AdaptationChannel>
                    {
                        new AdaptationChannel { ChannelNumber = 1, Name = "Idle Speed", CurrentValue = "750", NewValue = "750", Unit = "rpm", MinValue = 500, MaxValue = 1200 },
                        new AdaptationChannel { ChannelNumber = 2, Name = "Fuel Trim Offset", CurrentValue = "128", NewValue = "128", Unit = "", MinValue = 0, MaxValue = 255 },
                        new AdaptationChannel { ChannelNumber = 3, Name = "Throttle Adaptation", CurrentValue = "100", NewValue = "100", Unit = "%", MinValue = 0, MaxValue = 200 },
                    }
                },
                new EcuModule
                {
                    Address = 0x7E1, Name = "Transmission Control Module (TCM)", Protocol = "ISO 15765-4 CAN",
                    Variant = "Jatco CVT", PartNumber = "27600-5AN-A100", SoftwareVersion = "2.01.00",
                    HardwareVersion = "1.00", CodingBytes = new byte[] { 0x00, 0x10 },
                    LongCodingString = "0010", IsReachable = true,
                    AdaptationChannels = new List<AdaptationChannel>
                    {
                        new AdaptationChannel { ChannelNumber = 1, Name = "Shift Point Adjust", CurrentValue = "0", NewValue = "0", Unit = "", MinValue = -10, MaxValue = 10 },
                    }
                },
                new EcuModule
                {
                    Address = 0x7B0, Name = "ABS/VSA Control Module", Protocol = "ISO 15765-4 CAN",
                    Variant = "Bosch ABS 8.0", PartNumber = "57110-TBC-A030", SoftwareVersion = "1.20.00",
                    CodingBytes = new byte[] { 0x00, 0x01 }, LongCodingString = "0001", IsReachable = true
                },
                new EcuModule
                {
                    Address = 0x77C, Name = "SRS Airbag Module", Protocol = "ISO 15765-4 CAN",
                    Variant = "TRW SRS 4.0", PartNumber = "77960-TBC-A820", SoftwareVersion = "1.10.00",
                    CodingBytes = new byte[] { 0x04 }, LongCodingString = "04", IsReachable = true
                },
                new EcuModule
                {
                    Address = 0x7C4, Name = "Body Control Module (BCM)", Protocol = "ISO 15765-4 CAN",
                    Variant = "Honda BCM", PartNumber = "38800-TBC-A010", SoftwareVersion = "1.00.05",
                    CodingBytes = new byte[] { 0x02, 0x44, 0x10 }, LongCodingString = "024410", IsReachable = true
                }
            };
        }
    }

    /// <summary>
    /// Minimal <see cref="System.Net.EndPoint"/> that serialises to a
    /// Windows <c>sockaddr_bth</c> structure (30 bytes, AF_BTH = 32).
    /// Used for direct Bluetooth RFCOMM socket connections on Windows.
    /// </summary>
    internal sealed class BluetoothRfcommEndPoint : System.Net.EndPoint
    {
        private readonly SocketAddress _socketAddress;

        public BluetoothRfcommEndPoint(ulong btAddress, Guid serviceGuid, SocketAddress sa)
        {
            _socketAddress = sa;
        }

        public override AddressFamily AddressFamily => (AddressFamily)32;

        public override SocketAddress Serialize() => _socketAddress;

        public override System.Net.EndPoint Create(SocketAddress socketAddress) => this;
    }
}
