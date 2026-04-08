using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Toyota Group (Toyota / Lexus / Daihatsu) specific diagnostics.
    /// Supports Toyota-enhanced OBD2, Toyota CAN, TPMS protocols.
    /// Toyota-specific: INF (Information) codes, enhanced data PIDs,
    /// TechStream-equivalent functions, SFI/VVT-i/D-4S monitoring.
    /// </summary>
    public class ToyotaService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> ToyotaEcuAddresses = new()
        {
            [0x7E0] = "ECM — Engine Control Module",
            [0x7E1] = "TCM — Transmission Control Module",
            [0x7E4] = "HV — Hybrid Vehicle ECU",
            [0x7C0] = "IC — Instrument Cluster / Combination Meter",
            [0x750] = "SRS — Airbag ECU",
            [0x7B0] = "ABS/TRAC/VSC — Brake Control ECU",
            [0x7A0] = "BEAN — Body Electrical & Accessory Network",
            [0x7D0] = "BCM — Body Control Module",
            [0x780] = "PCS — Pre-Collision System",
            [0x7D4] = "MFD — Multi-Function Display",
            [0x7C4] = "EPS — Electric Power Steering",
            [0x7E8] = "SAS — Steering Angle Sensor",
            [0x770] = "AC — Air Conditioning Amplifier",
            [0x795] = "TPMS — Tire Pressure Warning ECU",
            [0x762] = "ITS — Intelligent Traction System",
            [0x793] = "RCCM — Rear Camera Control Module",
        };

        // Toyota-specific INF code descriptions
        public static readonly Dictionary<string, string> ToyotaInfCodes = new()
        {
            ["INF12"] = "IAC (Idle Air Control) Malfunction",
            ["INF14"] = "Crankshaft Position Sensor (NE) Malfunction",
            ["INF22"] = "ECT (Engine Coolant Temperature) Sensor Malfunction",
            ["INF24"] = "IAT (Intake Air Temperature) Sensor Malfunction",
            ["INF31"] = "MAP/BARO Sensor Malfunction",
            ["INF41"] = "Throttle Position Sensor Malfunction",
            ["INF43"] = "STA Signal Malfunction",
            ["INF52"] = "Knock Sensor Malfunction",
            ["INF71"] = "EGR System Malfunction",
            ["INF72"] = "EVAP System Malfunction",
        };

        // Toyota-enhanced PIDs (Mode 22 or proprietary)
        public static readonly Dictionary<string, (string Pid, string Unit, string Description)> ToyotaEnhancedPids = new()
        {
            ["VVT-i Advance Angle"] = ("011D", "°CA", "Variable valve timing advance angle"),
            ["Hybrid Battery SOC"]  = ("F111", "%",   "Hybrid battery state of charge"),
            ["HV Battery Temp"]     = ("F112", "°C",  "Hybrid battery temperature"),
            ["MG1 Speed"]           = ("F113", "rpm", "Motor-Generator 1 speed"),
            ["MG2 Speed"]           = ("F114", "rpm", "Motor-Generator 2 speed"),
            ["HV Battery Current"]  = ("F115", "A",   "Hybrid battery current"),
            ["Inverter Temp"]       = ("F116", "°C",  "Inverter temperature"),
            ["EGR Opening"]         = ("011F", "%",   "EGR valve opening angle"),
            ["Sub-Throttle"]        = ("0120", "°",   "Sub-throttle valve angle"),
            ["Laser Cruise Target"] = ("F200", "m",   "Pre-collision target distance"),
        };

        public ToyotaService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Toyota)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x7E0, 0x7E1, 0x7C0, 0x750, 0x7B0, 0x770, 0x795, 0x780 };
            foreach (var addr in addresses)
            {
                var name = ToyotaEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X3}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "Toyota Enhanced CAN",
                    PartNumber = $"89{_rng.Next(100, 999):D3}-{_rng.Next(10000, 99999):D5}",
                    SoftwareVersion = $"v{_rng.Next(1, 30):D2}.{_rng.Next(0, 9):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 5):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// Read Toyota/Lexus INF (Information) codes — OEM-specific codes beyond standard OBD2.
        /// </summary>
        public async Task<List<OemDtcRecord>> ReadInfCodesAsync()
        {
            await Task.Delay(IsSimulation ? 300 : 1000);
            if (IsSimulation)
                return new List<OemDtcRecord>
                {
                    new OemDtcRecord
                    {
                        Code = "P0171",
                        OemCode = "INF22",
                        Description = "System Too Lean (Bank 1)",
                        OemDescription = ToyotaInfCodes.GetValueOrDefault("INF22", "Unknown"),
                        EcuAddress = 0x7E0,
                        EcuName = "ECM",
                        IsActive = false,
                        IsStored = true,
                        Manufacturer = DisplayName
                    }
                };
            var resp = await SendUdsAsync(0x7E0, "21 00"); // Toyota enhanced read codes
            return new List<OemDtcRecord>();
        }

        /// <summary>
        /// Read Toyota-enhanced PIDs not available in standard OBD2.
        /// </summary>
        public override async Task<Dictionary<string, string>> ReadEnhancedPidsAsync()
        {
            await Task.Delay(IsSimulation ? 300 : 1200);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["VVT-i Advance Angle"] = $"{_rng.Next(0, 45):D2} °CA",
                    ["Hybrid Battery SOC"]  = $"{_rng.Next(30, 85):D2} %",
                    ["HV Battery Temp"]     = $"{_rng.Next(20, 45):D2} °C",
                    ["MG1 Speed"]           = $"{_rng.Next(0, 8000):D4} rpm",
                    ["MG2 Speed"]           = $"{_rng.Next(0, 6000):D4} rpm",
                    ["Inverter Temp"]       = $"{_rng.Next(40, 80):D2} °C",
                    ["EGR Opening"]         = $"{_rng.Next(0, 100):D2} %",
                    ["Sub-Throttle"]        = $"{_rng.Next(0, 90):D2} °",
                };
            var result = new Dictionary<string, string>();
            foreach (var (name, (pid, unit, _)) in ToyotaEnhancedPids)
            {
                var resp = await _obdService.SendCommandAsync($"22 {pid}");
                result[name] = $"{resp} {unit}".Trim();
            }
            return result;
        }

        /// <summary>
        /// Toyota VSC (Vehicle Stability Control) / TRAC zero-point calibration.
        /// </summary>
        public async Task<bool> CalibrateVscSensorAsync()
        {
            await Task.Delay(IsSimulation ? 500 : 3000);
            if (!IsSimulation)
            {
                await SendUdsAsync(0x7B0, "10 03");
                await SendUdsAsync(0x7B0, "2C 03 01 00"); // Zero-point reset
            }
            return true;
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            // Toyota Maintenance Required (MAINT REQD) light reset via combination meter
            if (!IsSimulation)
            {
                await SendUdsAsync(0x7C0, "10 03");
                await SendUdsAsync(0x7C0, "2E F1 A0 00 00 00 00"); // Reset mileage counter
            }
            else
                await Task.Delay(400);
            return true;
        }

        public override async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            if (ecuAddress == 0x7E0) // ECM
                return new List<ActuatorTest>
                {
                    new() { TestId = 1, Name = "Fuel Injectors",          Description = "Activate all injectors",              EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 2, Name = "Fuel Pump",               Description = "Activate fuel pump",                  EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 3, Name = "VVT-i Solenoid",          Description = "Activate VVT-i oil control valve",    EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 4, Name = "ACIS Valve",              Description = "Activate Acoustic Control Induction", EcuAddress = ecuAddress, Category = "Induction", RequiresEngineRunning = true },
                    new() { TestId = 5, Name = "Cooling Fan",             Description = "Activate cooling fan",                EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 6, Name = "EVAP VSV",                Description = "Activate EVAP vacuum switching valve", EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 7, Name = "EGR Valve",               Description = "Open/close EGR valve",               EcuAddress = ecuAddress, Category = "Emissions", RequiresEngineRunning = true },
                    new() { TestId = 8, Name = "D-4S Fuel Pump",          Description = "Activate D-4S high-pressure pump",    EcuAddress = ecuAddress, Category = "Fuel" },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }
    }
}
