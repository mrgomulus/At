using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Ford Group (Ford / Lincoln) specific diagnostics.
    /// Supports ISO 15765-4 (CAN), UDS, MS-CAN, HS-CAN protocols.
    /// Ford-specific features: FORScan-style enhanced PID reading,
    /// PCM configuration, module programming (PATS), IDS-equivalent functions.
    /// </summary>
    public class FordService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> FordEcuAddresses = new()
        {
            [0x7E0] = "PCM — Powertrain Control Module",
            [0x7E1] = "TCM — Transmission Control Module",
            [0x760] = "ABS — Anti-lock Brake System",
            [0x726] = "RCM — Restraints Control Module (Airbag)",
            [0x7A0] = "APIM — Accessory Protocol Interface Module (Sync)",
            [0x7D0] = "BCM — Body Control Module",
            [0x7C0] = "IPC — Instrument Panel Cluster",
            [0x740] = "SCCM — Steering Column Control Module",
            [0x7B0] = "OCSM — Occupant Classification System",
            [0x7C4] = "PSCM — Power Steering Control Module",
            [0x7E8] = "EPAS — Electric Power Assist Steering",
            [0x754] = "TCU — Telematics Control Unit (FordPass)",
            [0x703] = "FCIM — Front Control Interface Module",
            [0x741] = "DDM — Driver Door Module",
            [0x742] = "PDM — Passenger Door Module",
            [0x757] = "HVAC — Climate Control Module",
            [0x764] = "PAM — Parking Aid Module",
            [0x737] = "ACM — Audio Control Module",
            [0x777] = "TRM — Trailer Module",
        };

        // Ford-specific enhanced PIDs (beyond standard OBD2)
        public static readonly Dictionary<string, (string Pid, string Unit, string Description)> FordEnhancedPids = new()
        {
            ["Fuel Rail Pressure Desired"]  = ("0122", "kPa", "PCM desired fuel rail pressure"),
            ["MAF Sensor Voltage"]          = ("0149", "V",   "Mass Airflow sensor voltage"),
            ["BARO Sensor"]                 = ("F400", "kPa", "Barometric pressure sensor"),
            ["Cam Phaser Angle"]            = ("014C", "°",   "Cam phaser actual angle"),
            ["EPC Solenoid"]                = ("0198", "%",   "Electronic pressure control duty cycle"),
            ["4WD Mode"]                    = ("0280", "",    "4WD / AWD engagement state"),
            ["Fuel Pump Duty Cycle"]        = ("F40D", "%",   "Fuel pump module duty cycle"),
            ["Alternator Duty Cycle"]       = ("F40E", "%",   "Alternator load"),
            ["PATS Status"]                 = ("0431", "",    "Passive Anti-Theft System status"),
        };

        public FordService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Ford)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x7E0, 0x7E1, 0x760, 0x726, 0x7D0, 0x7C0, 0x7A0, 0x757, 0x764 };
            foreach (var addr in addresses)
            {
                var name = FordEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X3}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "ISO 15765-4 / UDS",
                    PartNumber = $"{_rng.Next(1000000, 9999999):D7}",
                    SoftwareVersion = $"{_rng.Next(1, 99):D2}.{_rng.Next(0, 99):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 9):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// Read Ford-enhanced PIDs via PCM. These go beyond standard OBD2 Mode 01.
        /// </summary>
        public override async Task<Dictionary<string, string>> ReadEnhancedPidsAsync()
        {
            await Task.Delay(IsSimulation ? 300 : 1200);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["Fuel Rail Pressure Desired"] = $"{_rng.Next(350, 650):D3} kPa",
                    ["MAF Sensor Voltage"] = $"{(_rng.NextDouble() * 3.5 + 0.5):F3} V",
                    ["BARO Sensor"] = $"{_rng.Next(95, 103):D3} kPa",
                    ["Cam Phaser Angle"] = $"{_rng.Next(-10, 40):D2} °",
                    ["EPC Solenoid"] = $"{_rng.Next(30, 70):D2} %",
                    ["Fuel Pump Duty Cycle"] = $"{_rng.Next(40, 80):D2} %",
                    ["Alternator Duty Cycle"] = $"{_rng.Next(30, 90):D2} %",
                    ["PATS Status"] = new[] { "OK", "No Transponder", "Wrong Code" }[_rng.Next(3)],
                };

            var result = new Dictionary<string, string>();
            foreach (var (name, (pid, unit, _)) in FordEnhancedPids)
            {
                var resp = await _obdService.SendCommandAsync($"22 {pid}");
                result[name] = $"{resp} {unit}".Trim();
            }
            return result;
        }

        /// <summary>
        /// PATS (Passive Anti-Theft System) status and key programming info.
        /// </summary>
        public async Task<Dictionary<string, string>> ReadPatsStatusAsync()
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["PATS Status"] = "System OK — Theft Indicator Normal",
                    ["Keys Programmed"] = _rng.Next(1, 5).ToString(),
                    ["Max Keys Allowed"] = "8",
                    ["Last Key Used"] = $"Key {_rng.Next(1, 4):D1}",
                    ["Immobilizer State"] = new[] { "Enabled", "Disabled" }[_rng.Next(2)],
                };
            var resp = await SendUdsAsync(0x726, "22 04 31");
            return new Dictionary<string, string> { ["PATS Raw"] = resp };
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            // Ford Intelligent Oil-Life Monitor reset via IPC (0x7C0)
            if (!IsSimulation)
            {
                await SendUdsAsync(0x7C0, "10 03");
                await SendUdsAsync(0x7C0, "2E F1 A1 00"); // Reset oil life to 100%
            }
            else
                await Task.Delay(400);
            return true;
        }

        public override async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            if (ecuAddress == 0x7E0) // PCM
                return new List<ActuatorTest>
                {
                    new() { TestId = 1, Name = "Injector Balance Test",   Description = "Test injector balance on each cylinder",  EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 2, Name = "Fuel Pump Relay",         Description = "Activate fuel pump relay",                EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 3, Name = "EVAP Canister Purge",     Description = "Activate EVAP purge valve",               EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 4, Name = "EGR Valve",               Description = "Sweep EGR valve",                        EcuAddress = ecuAddress, Category = "Emissions", RequiresEngineRunning = true },
                    new() { TestId = 5, Name = "Cooling Fan",             Description = "Activate engine cooling fan",             EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 6, Name = "Variable Cam Timing",     Description = "Test VCT solenoid",                      EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 7, Name = "Idle Air Control",        Description = "Cycle idle air control valve",           EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 8, Name = "Throttle Plate Reset",    Description = "Reset throttle plate learned position",   EcuAddress = ecuAddress, Category = "Induction", RequiresEngineOff = true },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }
    }
}
