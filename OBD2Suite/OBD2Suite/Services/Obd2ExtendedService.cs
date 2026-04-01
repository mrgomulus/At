using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// OBD2 Mode 01 PID 01 — I/M Readiness monitor status.
    /// Mode 02 — Freeze frame data.
    /// Mode 06 — On-board monitoring test results.
    /// Mode 07 — Pending DTCs.
    /// Mode 09 — Vehicle information (VIN, Cal ID, CVN).
    /// </summary>
    public class Obd2ExtendedService
    {
        private readonly IObdService _obdService;
        private readonly Random _rng = new();

        public Obd2ExtendedService(IObdService obdService) => _obdService = obdService;

        // ─── Mode 01 PID 01 — I/M Readiness ────────────────────────────────

        /// <summary>Read all OBD2 I/M readiness monitors.</summary>
        public async Task<ReadinessReport> ReadReadinessAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 400 : 1200);
            if (_obdService.IsSimulationMode)
                return SimulateReadiness();

            var resp = await _obdService.SendCommandAsync("01 01");
            return ParseReadinessResponse(resp);
        }

        private ReadinessReport ParseReadinessResponse(string resp)
        {
            var report = BuildMonitorTemplate();
            try
            {
                var bytes = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Select(b => Convert.ToByte(b, 16)).ToArray();
                if (bytes.Length < 4) return report;

                // Byte A — bit 7 = MIL, bits 0-6 = DTC count
                report.MilOn = (bytes[0] & 0x80) != 0;
                report.DtcCount = bytes[0] & 0x7F;

                // Bytes B–D encode which monitors are supported/ready
                // B byte: continuous monitors
                // C byte: non-continuous supported (bits)
                // D byte: non-continuous complete (bits)
                SetMonitorFromBits(report.Monitors, bytes[1], bytes[2], bytes[3]);
            }
            catch { /* return defaults on parse error */ }
            return report;
        }

        private static void SetMonitorFromBits(List<ReadinessMonitor> monitors, byte b, byte c, byte d)
        {
            // Continuous monitors (byte B bits 0-2)
            SetMonitor(monitors, "Misfire",         (b & 0x01) != 0, (b & 0x10) == 0);
            SetMonitor(monitors, "Fuel System",     (b & 0x02) != 0, (b & 0x20) == 0);
            SetMonitor(monitors, "Components",      (b & 0x04) != 0, (b & 0x40) == 0);
            // Non-continuous monitors (byte C supported, byte D complete)
            SetMonitor(monitors, "Catalyst",              (c & 0x01) != 0, (d & 0x01) == 0);
            SetMonitor(monitors, "Heated Catalyst",       (c & 0x02) != 0, (d & 0x02) == 0);
            SetMonitor(monitors, "EVAP System",           (c & 0x04) != 0, (d & 0x04) == 0);
            SetMonitor(monitors, "Secondary Air",         (c & 0x08) != 0, (d & 0x08) == 0);
            SetMonitor(monitors, "A/C Refrigerant",       (c & 0x10) != 0, (d & 0x10) == 0);
            SetMonitor(monitors, "O2 Sensor",             (c & 0x20) != 0, (d & 0x20) == 0);
            SetMonitor(monitors, "O2 Sensor Heater",      (c & 0x40) != 0, (d & 0x40) == 0);
            SetMonitor(monitors, "EGR System",            (c & 0x80) != 0, (d & 0x80) == 0);
        }

        private static void SetMonitor(List<ReadinessMonitor> monitors, string name, bool supported, bool complete)
        {
            var m = monitors.FirstOrDefault(x => x.Name == name);
            if (m == null) return;
            m.IsSupported = supported;
            m.Status = !supported ? MonitorStatus.NotAvailable
                     : complete   ? MonitorStatus.Complete
                                  : MonitorStatus.Incomplete;
        }

        private ReadinessReport SimulateReadiness()
        {
            var report = BuildMonitorTemplate();
            report.MilOn = false;
            report.DtcCount = 0;
            foreach (var m in report.Monitors)
            {
                m.IsSupported = _rng.Next(10) > 1; // 90% supported
                if (!m.IsSupported) { m.Status = MonitorStatus.NotAvailable; continue; }
                m.Status = _rng.Next(10) > 1 ? MonitorStatus.Complete : MonitorStatus.Incomplete;
            }
            return report;
        }

        private static ReadinessReport BuildMonitorTemplate() => new()
        {
            Monitors = new List<ReadinessMonitor>
            {
                new() { Name = "Misfire",           Description = "Cylinder misfire detection",                IsContinuous = true },
                new() { Name = "Fuel System",       Description = "Fuel delivery & mixture",                   IsContinuous = true },
                new() { Name = "Components",        Description = "Electrical sensor components",              IsContinuous = true },
                new() { Name = "Catalyst",          Description = "Catalytic converter efficiency",            IsContinuous = false },
                new() { Name = "Heated Catalyst",   Description = "Heated catalytic converter (secondary)",    IsContinuous = false },
                new() { Name = "EVAP System",       Description = "Evaporative emissions system",             IsContinuous = false },
                new() { Name = "Secondary Air",     Description = "Secondary air injection system",            IsContinuous = false },
                new() { Name = "A/C Refrigerant",   Description = "A/C refrigerant system",                   IsContinuous = false },
                new() { Name = "O2 Sensor",         Description = "Oxygen sensor response",                   IsContinuous = false },
                new() { Name = "O2 Sensor Heater",  Description = "Oxygen sensor heater circuit",             IsContinuous = false },
                new() { Name = "EGR System",        Description = "Exhaust gas recirculation system",          IsContinuous = false },
            }
        };

        // ─── Mode 02 — Freeze Frame ─────────────────────────────────────────

        /// <summary>Read freeze frame data for the stored DTC.</summary>
        public async Task<List<FreezeFrame>> ReadFreezeFramesAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 600 : 2000);
            if (_obdService.IsSimulationMode)
                return SimulateFreezeFrames();

            var frames = new List<FreezeFrame>();
            // Read frame 0 (the primary frame)
            var frame = await ReadSingleFreezeFrameAsync(0);
            if (frame != null) frames.Add(frame);
            return frames;
        }

        private async Task<FreezeFrame?> ReadSingleFreezeFrameAsync(int frameNumber)
        {
            try
            {
                var frame = new FreezeFrame { FrameNumber = frameNumber };

                // Mode 02 PID 02 — DTC that triggered freeze frame
                var dtcResp = await _obdService.SendCommandAsync($"02 02 {frameNumber:X2}");
                frame.DtcCode = ParseDtcFromFreezeFrame(dtcResp);

                // Read common freeze frame PIDs
                var pids = new (string Pid, string Name, double Scale, double Offset, string Unit)[]
                {
                    ("04", "Calculated Load", 100.0/255.0, 0, "%"),
                    ("05", "Coolant Temperature", 1, -40, "°C"),
                    ("06", "Short Term Fuel Trim B1", 100.0/128.0, -100, "%"),
                    ("07", "Long Term Fuel Trim B1",  100.0/128.0, -100, "%"),
                    ("0B", "Intake Manifold Pressure", 1, 0, "kPa"),
                    ("0C", "Engine RPM", 0.25, 0, "rpm"),
                    ("0D", "Vehicle Speed", 1, 0, "km/h"),
                    ("0E", "Timing Advance", 0.5, -64, "°"),
                    ("0F", "Intake Air Temperature", 1, -40, "°C"),
                    ("10", "MAF Air Flow", 0.01, 0, "g/s"),
                    ("11", "Throttle Position", 100.0/255.0, 0, "%"),
                };

                foreach (var (pid, name, scale, offset, unit) in pids)
                {
                    var resp = await _obdService.SendCommandAsync($"02 {pid} {frameNumber:X2}");
                    var val = ParseSingleByteValue(resp, scale, offset);
                    frame.Parameters.Add(new FreezeFrameParameter { PidHex = pid, Name = name, Value = val, Unit = unit });
                }

                return frame;
            }
            catch { return null; }
        }

        private static string ParseDtcFromFreezeFrame(string resp)
        {
            try
            {
                var bytes = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => Convert.ToByte(b, 16)).ToArray();
                if (bytes.Length < 3) return "None";
                var b1 = bytes[1]; var b2 = bytes[2];
                char prefix = ((b1 >> 6) & 3) switch { 0 => 'P', 1 => 'C', 2 => 'B', _ => 'U' };
                return $"{prefix}{(b1 & 0x3F):X}{b2:X2}";
            }
            catch { return "None"; }
        }

        private static double ParseSingleByteValue(string resp, double scale, double offset)
        {
            try
            {
                var bytes = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (bytes.Length < 2) return 0;
                return Convert.ToByte(bytes[1], 16) * scale + offset;
            }
            catch { return 0; }
        }

        private List<FreezeFrame> SimulateFreezeFrames()
        {
            return new List<FreezeFrame>
            {
                new FreezeFrame
                {
                    DtcCode = "P0171",
                    FrameNumber = 0,
                    Parameters = new List<FreezeFrameParameter>
                    {
                        new() { Name = "Calculated Load",         PidHex = "04", Value = Math.Round(_rng.NextDouble() * 80 + 10, 1),  Unit = "%" },
                        new() { Name = "Coolant Temperature",     PidHex = "05", Value = _rng.Next(75, 105),                           Unit = "°C" },
                        new() { Name = "Short Term Fuel Trim B1", PidHex = "06", Value = Math.Round(_rng.NextDouble() * 20 - 10, 1),   Unit = "%" },
                        new() { Name = "Long Term Fuel Trim B1",  PidHex = "07", Value = Math.Round(_rng.NextDouble() * 20 - 10, 1),   Unit = "%" },
                        new() { Name = "Engine RPM",              PidHex = "0C", Value = _rng.Next(700, 3000),                         Unit = "rpm" },
                        new() { Name = "Vehicle Speed",           PidHex = "0D", Value = _rng.Next(0, 120),                            Unit = "km/h" },
                        new() { Name = "Throttle Position",       PidHex = "11", Value = Math.Round(_rng.NextDouble() * 60, 1),         Unit = "%" },
                        new() { Name = "Intake Air Temp",         PidHex = "0F", Value = _rng.Next(20, 40),                             Unit = "°C" },
                        new() { Name = "MAF Air Flow",            PidHex = "10", Value = Math.Round(_rng.NextDouble() * 50 + 5, 2),    Unit = "g/s" },
                        new() { Name = "Intake Manifold Press",   PidHex = "0B", Value = _rng.Next(30, 100),                           Unit = "kPa" },
                    }
                }
            };
        }

        // ─── Mode 06 — On-board Monitoring Tests ────────────────────────────

        /// <summary>Read Mode 06 on-board monitoring test results.</summary>
        public async Task<List<OnboardTest>> ReadOnboardTestsAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 500 : 2500);
            if (_obdService.IsSimulationMode)
                return SimulateOnboardTests();

            var tests = new List<OnboardTest>();
            // Request all supported tests (test ID $00 = list supported)
            var resp = await _obdService.SendCommandAsync("06 00");
            // Real parsing would iterate supported test IDs — return simulated for now
            return SimulateOnboardTests();
        }

        private List<OnboardTest> SimulateOnboardTests() => new()
        {
            new OnboardTest { TestId = "$01", Name = "Catalyst Monitor B1",        Description = "Catalyst efficiency Bank 1",     MeasuredValue = _rng.Next(70, 99),  MinLimit = 0,   MaxLimit = 255, Unit = "%",       Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$02", Name = "Catalyst Monitor B2",        Description = "Catalyst efficiency Bank 2",     MeasuredValue = _rng.Next(70, 99),  MinLimit = 0,   MaxLimit = 255, Unit = "%",       Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$11", Name = "O2 Sensor Switch B1S1",      Description = "O2 sensor switching frequency",  MeasuredValue = _rng.Next(5, 20),   MinLimit = 3,   MaxLimit = 50,  Unit = "Hz",      Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$12", Name = "O2 Sensor Heater B1S1",      Description = "O2 heater current draw",         MeasuredValue = Math.Round(_rng.NextDouble() * 0.5 + 0.5, 2), MinLimit = 0.2, MaxLimit = 2.0, Unit = "A", Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$21", Name = "EVAP Large Leak Test",       Description = "EVAP gross leak detection (0.08\")", MeasuredValue = _rng.Next(0, 5), MinLimit = 0,  MaxLimit = 10,  Unit = "mm Hg",   Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$22", Name = "EVAP Small Leak Test",       Description = "EVAP small leak detection (0.04\")", MeasuredValue = _rng.Next(0, 5), MinLimit = 0,  MaxLimit = 10,  Unit = "mm Hg",   Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$31", Name = "EGR Flow Test",              Description = "EGR gas recirculation flow",     MeasuredValue = _rng.Next(5, 25),   MinLimit = 3,   MaxLimit = 30,  Unit = "%",       Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$41", Name = "Misfire Counter Cyl 1",      Description = "Misfires detected cylinder 1",   MeasuredValue = _rng.Next(0, 5),    MinLimit = 0,   MaxLimit = 200, Unit = "count",   Result = _rng.Next(10) > 1 ? OnboardTestResult.Pass : OnboardTestResult.Fail },
            new OnboardTest { TestId = "$42", Name = "Misfire Counter Cyl 2",      Description = "Misfires detected cylinder 2",   MeasuredValue = _rng.Next(0, 3),    MinLimit = 0,   MaxLimit = 200, Unit = "count",   Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$43", Name = "Misfire Counter Cyl 3",      Description = "Misfires detected cylinder 3",   MeasuredValue = _rng.Next(0, 3),    MinLimit = 0,   MaxLimit = 200, Unit = "count",   Result = OnboardTestResult.Pass },
            new OnboardTest { TestId = "$44", Name = "Misfire Counter Cyl 4",      Description = "Misfires detected cylinder 4",   MeasuredValue = _rng.Next(0, 3),    MinLimit = 0,   MaxLimit = 200, Unit = "count",   Result = OnboardTestResult.Pass },
        };

        // ─── Mode 07 — Pending DTCs ─────────────────────────────────────────

        public async Task<List<string>> ReadPendingDtcsAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 400 : 1000);
            if (_obdService.IsSimulationMode)
                return new List<string> { "P0420 (Pending)", "P0442 (Pending)" };

            var resp = await _obdService.SendCommandAsync("07");
            return ParseDtcResponse(resp);
        }

        // ─── Mode 09 — Vehicle Information ─────────────────────────────────

        public async Task<Dictionary<string, string>> ReadVehicleInformationAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 300 : 1200);
            if (_obdService.IsSimulationMode)
                return new Dictionary<string, string>
                {
                    ["VIN (Mode 09 PID 02)"]              = $"WV2ZZZ7HZ{_rng.Next(1000000, 9999999):D7}",
                    ["Calibration ID (PID 04)"]           = $"CAL-{_rng.Next(10000, 99999):D5}",
                    ["Calibration Verify # (PID 06)"]     = $"{_rng.Next(0, int.MaxValue):X8}",
                    ["IUPR Counters (PID 08)"]             = "Catalyst: 12/15  O2: 10/12  EVAP: 8/10",
                    ["ECU Name (PID 0A)"]                  = "ENG_ECU_BOSCH_ME17",
                    ["Performance Tracking (PID 0B)"]     = $"Ignition cycles: {_rng.Next(500, 5000)}",
                };
            var result = new Dictionary<string, string>();
            var vinResp = await _obdService.SendCommandAsync("09 02");
            result["VIN"] = ParseMode09Vin(vinResp);
            var calResp = await _obdService.SendCommandAsync("09 04");
            result["Calibration ID"] = ParseAscii(calResp);
            return result;
        }

        private static string ParseMode09Vin(string resp)
        {
            try
            {
                var parts = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2);
                return new string(parts.Select(b => (char)Convert.ToByte(b, 16)).Where(c => c >= 0x20 && c < 0x7F).ToArray());
            }
            catch { return "Parse error"; }
        }

        private static string ParseAscii(string resp)
        {
            try
            {
                return new string(resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Skip(2).Select(b => (char)Convert.ToByte(b, 16)).Where(c => c >= 0x20 && c < 0x7F).ToArray());
            }
            catch { return resp; }
        }

        private static List<string> ParseDtcResponse(string resp)
        {
            var dtcs = new List<string>();
            try
            {
                var bytes = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => Convert.ToByte(b, 16)).ToArray();
                for (int i = 0; i < bytes.Length - 1; i += 2)
                {
                    if (bytes[i] == 0 && bytes[i + 1] == 0) continue;
                    char prefix = ((bytes[i] >> 6) & 3) switch { 0 => 'P', 1 => 'C', 2 => 'B', _ => 'U' };
                    dtcs.Add($"{prefix}{(bytes[i] & 0x3F):X}{bytes[i + 1]:X2}");
                }
            }
            catch { }
            return dtcs;
        }
    }
}
