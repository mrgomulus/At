using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Base class for all manufacturer service implementations.
    /// Provides shared helpers and simulation support.
    /// </summary>
    public abstract class BaseManufacturerService : IManufacturerService
    {
        protected readonly IObdService _obdService;
        protected readonly Random _rng = new();

        public abstract Manufacturer Manufacturer { get; }
        public abstract string DisplayName { get; }

        protected BaseManufacturerService(IObdService obdService)
        {
            _obdService = obdService;
        }

        protected bool IsSimulation => _obdService.IsSimulationMode;

        protected async Task<string> SendUdsAsync(int ecuAddress, string serviceHex)
        {
            // Target ECU via header setting, then send UDS request
            await _obdService.SendCommandAsync($"ATSH{ecuAddress:X3}");
            return await _obdService.SendCommandAsync(serviceHex);
        }

        protected static string ParseAsciiBytes(string hexResponse)
        {
            try
            {
                var bytes = hexResponse.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var chars = bytes.Skip(3) // skip service/length bytes
                                 .Select(b => (char)Convert.ToByte(b, 16))
                                 .Where(c => c >= 0x20 && c < 0x7F);
                return new string(chars.ToArray()).Trim();
            }
            catch { return hexResponse; }
        }

        protected static byte[] ParseHexBytes(string hexResponse)
        {
            try
            {
                return hexResponse.Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => Convert.ToByte(b, 16))
                    .ToArray();
            }
            catch { return Array.Empty<byte>(); }
        }

        // Default simulation implementations
        public virtual async Task<List<EcuModule>> ScanAllEcusAsync()
        {
            await Task.Delay(IsSimulation ? 800 : 5000);
            return GetSimulatedEcuList();
        }

        public virtual async Task<List<OemDtcRecord>> ReadAllFaultCodesAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 300 : 1500);
            if (!IsSimulation) return new List<OemDtcRecord>();
            return GetSimulatedDtcs(ecuAddress);
        }

        public virtual async Task<bool> ClearFaultCodesAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(ecuAddress, "14 FF FF FF"); // UDS ClearDiagnosticInformation
                return true;
            }
            await Task.Delay(400);
            return true;
        }

        public virtual async Task<List<MeasuringBlock>> ReadMeasuringBlockAsync(int ecuAddress, int groupNumber)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (!IsSimulation) return new List<MeasuringBlock>();
            return GetSimulatedMeasuringBlocks(ecuAddress, groupNumber);
        }

        public virtual async Task<List<int>> GetAvailableMeasuringGroupsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            return Enumerable.Range(1, 20).ToList();
        }

        public virtual async Task<string> ReadAdaptationAsync(int ecuAddress, int channel)
        {
            if (!IsSimulation)
            {
                var resp = await SendUdsAsync(ecuAddress, $"21 {channel:X2}");
                return resp;
            }
            await Task.Delay(200);
            return (_rng.Next(0, 255)).ToString();
        }

        public virtual async Task<bool> WriteAdaptationAsync(int ecuAddress, int channel, string newValue)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(ecuAddress, $"2C 03 {channel:X2} {newValue}");
                return true;
            }
            await Task.Delay(400);
            return true;
        }

        public virtual async Task<string> ReadCodingAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                var resp = await SendUdsAsync(ecuAddress, "22 F1 87"); // UDS ReadDataByIdentifier
                return resp;
            }
            await Task.Delay(200);
            return $"{_rng.Next(0, 255):X2} {_rng.Next(0, 255):X2} {_rng.Next(0, 255):X2}";
        }

        public virtual async Task<bool> WriteCodingAsync(int ecuAddress, string newCoding)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(ecuAddress, $"2E F1 87 {newCoding}");
                return true;
            }
            await Task.Delay(500);
            return true;
        }

        public virtual async Task<byte[]> ReadLongCodingAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                var resp = await SendUdsAsync(ecuAddress, "22 F1 86");
                return ParseHexBytes(resp);
            }
            await Task.Delay(200);
            var bytes = new byte[8];
            _rng.NextBytes(bytes);
            return bytes;
        }

        public virtual async Task<bool> WriteLongCodingAsync(int ecuAddress, byte[] codingBytes)
        {
            if (!IsSimulation)
            {
                var hex = string.Join(" ", codingBytes.Select(b => b.ToString("X2")));
                await SendUdsAsync(ecuAddress, $"2E F1 86 {hex}");
                return true;
            }
            await Task.Delay(600);
            return true;
        }

        public virtual async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            return GetDefaultActuatorTests(ecuAddress);
        }

        public virtual async Task<string> RunActuatorTestAsync(int ecuAddress, int testId)
        {
            await Task.Delay(IsSimulation ? 1500 : 3000);
            return IsSimulation ? "Test completed successfully (simulated)" : "OK";
        }

        public virtual async Task<bool> LoginAsync(int ecuAddress, int accessCode)
        {
            if (!IsSimulation)
            {
                var codeHex = accessCode.ToString("X4");
                await SendUdsAsync(ecuAddress, $"27 01"); // RequestSeed
                await SendUdsAsync(ecuAddress, $"27 02 {codeHex}"); // SendKey
                return true;
            }
            await Task.Delay(300);
            return true;
        }

        public virtual async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 300 : 1000);
            return true;
        }

        public virtual async Task<bool> ResetThrottleBodyAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 400 : 2000);
            return true;
        }

        public virtual async Task<bool> PerformSteeringAngleCalibrationAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 500 : 3000);
            return true;
        }

        public virtual async Task<bool> PerformThrottleBasicSettingAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 500 : 5000);
            return true;
        }

        public virtual async Task<Dictionary<string, string>> ReadEcuIdentificationAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (IsSimulation) return GetSimulatedEcuIdentification(ecuAddress);

            var result = new Dictionary<string, string>();
            var resp1 = await SendUdsAsync(ecuAddress, "22 F1 80"); // Boot software id
            result["Boot Software"] = ParseAsciiBytes(resp1);
            var resp2 = await SendUdsAsync(ecuAddress, "22 F1 87"); // Application software id
            result["Application Software"] = ParseAsciiBytes(resp2);
            var resp3 = await SendUdsAsync(ecuAddress, "22 F1 90"); // VIN
            result["VIN"] = ParseAsciiBytes(resp3);
            var resp4 = await SendUdsAsync(ecuAddress, "22 F1 92"); // System supplier ECU SW number
            result["Supplier SW Nr"] = ParseAsciiBytes(resp4);
            return result;
        }

        // ── Simulation helpers ──────────────────────────────────────────────

        protected abstract List<EcuModule> GetSimulatedEcuList();

        protected virtual List<OemDtcRecord> GetSimulatedDtcs(int ecuAddress) => new()
        {
            new OemDtcRecord
            {
                Code = "P0171",
                OemCode = GetOemCodeForAddress(ecuAddress),
                Description = "System Too Lean (Bank 1)",
                OemDescription = "Fuel trim too lean, lambda controller at positive stop",
                EcuAddress = ecuAddress,
                EcuName = GetEcuNameForAddress(ecuAddress),
                IsActive = _rng.Next(2) == 0,
                IsStored = true,
                Frequency = $"{_rng.Next(1, 10)} occurrence(s)",
                Manufacturer = DisplayName
            },
            new OemDtcRecord
            {
                Code = "P0300",
                OemCode = GetOemCodeForAddress(ecuAddress, 1),
                Description = "Random/Multiple Cylinder Misfire Detected",
                OemDescription = "Engine misfires — check ignition system",
                EcuAddress = ecuAddress,
                EcuName = GetEcuNameForAddress(ecuAddress),
                IsActive = false,
                IsIntermittent = true,
                IsStored = true,
                Frequency = $"{_rng.Next(1, 5)} occurrence(s)",
                Manufacturer = DisplayName
            }
        };

        protected virtual List<MeasuringBlock> GetSimulatedMeasuringBlocks(int ecuAddress, int group) => new()
        {
            new MeasuringBlock
            {
                GroupNumber = group,
                Name = $"Group {group:D3}",
                Description = "Engine operating values",
                Fields = new List<MeasuringField>
                {
                    new() { FieldIndex = 1, Label = "Engine Speed", Value = _rng.Next(700, 3500).ToString(), Unit = "rpm" },
                    new() { FieldIndex = 2, Label = "Coolant Temp", Value = _rng.Next(80, 100).ToString(), Unit = "°C" },
                    new() { FieldIndex = 3, Label = "Throttle Angle", Value = (_rng.NextDouble() * 100).ToString("F1"), Unit = "%" },
                    new() { FieldIndex = 4, Label = "Lambda", Value = (0.97 + _rng.NextDouble() * 0.06).ToString("F3"), Unit = "λ" }
                }
            }
        };

        protected virtual List<ActuatorTest> GetDefaultActuatorTests(int ecuAddress) => new()
        {
            new ActuatorTest { TestId = 1, Name = "Fuel Pump", Description = "Activate fuel pump relay", EcuAddress = ecuAddress, Category = "Fuel System" },
            new ActuatorTest { TestId = 2, Name = "Cooling Fan", Description = "Activate radiator cooling fan", EcuAddress = ecuAddress, Category = "Cooling" },
            new ActuatorTest { TestId = 3, Name = "Purge Valve", Description = "Activate EVAP purge valve", EcuAddress = ecuAddress, Category = "Emissions" },
            new ActuatorTest { TestId = 4, Name = "EGR Valve", Description = "Open/close EGR valve", EcuAddress = ecuAddress, Category = "Emissions" },
            new ActuatorTest { TestId = 5, Name = "Idle Air Control", Description = "Cycle idle air control valve", EcuAddress = ecuAddress, Category = "Fuel System" },
            new ActuatorTest { TestId = 6, Name = "Horn", Description = "Sound vehicle horn", EcuAddress = ecuAddress, Category = "Body" },
            new ActuatorTest { TestId = 7, Name = "Headlights", Description = "Toggle headlight relay", EcuAddress = ecuAddress, Category = "Body" },
        };

        protected virtual Dictionary<string, string> GetSimulatedEcuIdentification(int ecuAddress) =>
            new()
            {
                ["ECU Address"] = $"0x{ecuAddress:X2}",
                ["ECU Name"] = GetEcuNameForAddress(ecuAddress),
                ["Part Number"] = $"{_rng.Next(1000000, 9999999):D7}",
                ["Hardware Version"] = $"H{_rng.Next(1, 9):D2}",
                ["Software Version"] = $"S{_rng.Next(1, 99):D4}",
                ["Coding"] = $"{_rng.Next(0, 65535):X4}",
                ["WSC"] = $"{_rng.Next(10000, 99999):D5} {_rng.Next(100, 999):D3} {_rng.Next(10000, 99999):D5}",
                ["ASAM/ODX Dataset"] = $"EV_{GetEcuNameForAddress(ecuAddress).Replace(" ", "")}_0001",
            };

        protected virtual string GetOemCodeForAddress(int addr, int offset = 0) =>
            $"{(addr * 1000 + offset + _rng.Next(100)):D5}";

        protected virtual string GetEcuNameForAddress(int addr) => addr switch
        {
            0x01 => "Engine Control Unit",
            0x02 => "Transmission Control Unit",
            0x03 => "ABS/ESP Module",
            0x08 => "HVAC Control",
            0x09 => "Central Electronics",
            0x15 => "Airbag Module",
            0x16 => "Steering Column Electronics",
            0x17 => "Instruments (Cluster)",
            0x19 => "CAN Gateway",
            0x25 => "Immobilizer",
            0x37 => "Navigation",
            0x46 => "Comfort System",
            0x52 => "Door Electronics (Driver)",
            0x53 => "Parking Brake",
            0x55 => "Headlight Control",
            0x65 => "Tire Pressure Monitor",
            0x76 => "Parking Assistance",
            _ => $"ECU 0x{addr:X2}"
        };

        // ─── Default implementations of new interface members ───────────────

        public virtual async Task<string> PerformEpbServiceAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 1200 : 3000);
            return IsSimulation ? "✔ EPB caliper service mode activated (Simulated)" : "✖ Not supported by this vehicle";
        }

        public virtual async Task<string> PerformDpfRegenerationAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 1500 : 5000);
            return IsSimulation ? "✔ DPF forced regeneration started — ~30 min (Simulated)" : "✖ DPF regen not supported";
        }

        public virtual async Task<string> RegisterBatteryAsync(int ecuAddress, int capacityAh, string batteryType)
        {
            await Task.Delay(IsSimulation ? 800 : 2000);
            return IsSimulation ? $"✔ Battery registered: {capacityAh} Ah, {batteryType} (Simulated)" : "✖ Battery registration not supported";
        }

        public virtual async Task<string> ResetAbsBleedAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 1000 : 3000);
            return IsSimulation ? "✔ ABS bleed cycle started (Simulated)" : "✖ ABS bleed not supported";
        }

        public virtual async Task<string> PerformGearboxAdaptationResetAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 800 : 2000);
            return IsSimulation ? "✔ Gearbox adaptation reset (Simulated)" : "✖ Not supported";
        }

        public virtual async Task<string> PerformInjectorCodingAsync(int ecuAddress, string[] codes)
        {
            await Task.Delay(IsSimulation ? 1000 : 3000);
            return IsSimulation ? $"✔ {codes.Length} injector code(s) programmed (Simulated)" : "✖ Injector coding not supported";
        }

        public virtual async Task<string> PerformTpmsReregistrationAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 800 : 2000);
            return IsSimulation ? "✔ TPMS relearn triggered — drive > 30 km/h (Simulated)" : "✖ TPMS relearn not supported";
        }

        public virtual async Task<string> ResetAdBlueAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 600 : 1500);
            return IsSimulation ? "✔ AdBlue quantity reset (Simulated)" : "✖ Not supported";
        }

        public virtual async Task<string> ResetDpfAshCounterAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 600 : 1500);
            return IsSimulation ? "✔ DPF ash counter reset (Simulated)" : "✖ Not supported";
        }

        public virtual async Task<Dictionary<string, string>> ReadEnhancedPidsAsync()
        {
            await Task.Delay(IsSimulation ? 400 : 1200);
            return new Dictionary<string, string>
            {
                ["Engine RPM"]         = $"{_rng.Next(700, 4000)} rpm",
                ["Vehicle Speed"]      = $"{_rng.Next(0, 130)} km/h",
                ["Coolant Temp"]       = $"{_rng.Next(80, 105)} °C",
                ["Engine Load"]        = $"{_rng.Next(10, 80):F1} %",
                ["Throttle Position"]  = $"{_rng.Next(5, 60):F1} %",
                ["Fuel Rail Pressure"] = $"{_rng.Next(300, 800):F1} bar",
                ["Intake Temp"]        = $"{_rng.Next(20, 45)} °C",
                ["Battery Voltage"]    = $"{11.8 + _rng.NextDouble():F2} V",
            };
        }
    }
}
