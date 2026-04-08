using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// GM Group (Opel / Vauxhall / Chevrolet / Cadillac / Buick / GMC) specific diagnostics.
    /// Supports GM-LAN, UART ALDL (older), ISO 15765-4 (CAN), UDS protocols.
    /// GM-specific: Tech2-style functions, SPS module programming info,
    /// GM enhanced PIDs, module configuration reads.
    /// </summary>
    public class GmService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> GmEcuAddresses = new()
        {
            [0x7E0] = "ECM — Engine Control Module",
            [0x7E1] = "TCM — Transmission Control Module",
            [0x7E4] = "BCM — Body Control Module",
            [0x7D0] = "EBCM — Electronic Brake Control Module (ABS/TCS/ESC)",
            [0x720] = "SDM — Sensing & Diagnostic Module (Airbag)",
            [0x737] = "HVAC — Heating, Ventilation & A/C Module",
            [0x710] = "IPC — Instrument Panel Cluster",
            [0x7D4] = "FICM — Fuel Injector Control Module (Diesel)",
            [0x771] = "FCM — Front Camera Module",
            [0x773] = "RCM — Rear Camera Module",
            [0x785] = "PAM — Park Assist Module",
            [0x792] = "TPMS — Tire Pressure Monitor",
            [0x7B0] = "EPS — Electric Power Steering",
            [0x7C0] = "OnStar — Telematics Module",
            [0x762] = "DRL — Daytime Running Lamp Module",
            [0x7A5] = "HUD — Head-Up Display",
            [0x796] = "ACC — Adaptive Cruise Control",
        };

        // GM enhanced PIDs
        public static readonly Dictionary<string, (string Pid, string Unit, string Description)> GmEnhancedPids = new()
        {
            ["Engine Torque"]             = ("F401", "Nm",  "Actual engine torque output"),
            ["Desired Idle Speed"]        = ("F402", "rpm", "ECM desired idle speed"),
            ["Boost Pressure Desired"]    = ("F403", "kPa", "Turbo boost desired pressure"),
            ["Boost Pressure Actual"]     = ("F404", "kPa", "Turbo boost actual pressure"),
            ["MAP Sensor"]                = ("F405", "kPa", "Manifold absolute pressure"),
            ["Fuel Level (raw)"]          = ("F406", "%",   "Fuel level sender voltage"),
            ["Oil Life Remaining"]        = ("F407", "%",   "Oil life monitor percentage"),
            ["Trans Fluid Temp"]          = ("F408", "°C",  "Transmission fluid temperature"),
            ["BARO Pressure"]             = ("F409", "kPa", "Barometric pressure"),
            ["EGR Desired Position"]      = ("F40A", "%",   "EGR valve desired position"),
        };

        public GmService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Opel)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x7E0, 0x7E1, 0x7E4, 0x7D0, 0x720, 0x710, 0x737, 0x792, 0x7B0 };
            foreach (var addr in addresses)
            {
                var name = GmEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X3}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "GM-LAN / ISO 15765-4",
                    PartNumber = $"{_rng.Next(10000000, 99999999):D8}",
                    SoftwareVersion = $"{_rng.Next(1, 9):D2}.{_rng.Next(0, 99):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 9):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// Read GM-enhanced PIDs (module-specific data not in standard OBD2).
        /// </summary>
        public override async Task<Dictionary<string, string>> ReadEnhancedPidsAsync()
        {
            await Task.Delay(IsSimulation ? 300 : 1200);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["Engine Torque"] = $"{_rng.Next(50, 350):D3} Nm",
                    ["Desired Idle Speed"] = $"{_rng.Next(600, 900):D3} rpm",
                    ["Boost Pressure Desired"] = $"{_rng.Next(100, 200):D3} kPa",
                    ["Boost Pressure Actual"] = $"{_rng.Next(95, 195):D3} kPa",
                    ["MAP Sensor"] = $"{_rng.Next(30, 100):D2} kPa",
                    ["Oil Life Remaining"] = $"{_rng.Next(10, 100):D2} %",
                    ["Trans Fluid Temp"] = $"{_rng.Next(70, 110):D2} °C",
                    ["BARO Pressure"] = $"{_rng.Next(95, 103):D3} kPa",
                    ["EGR Desired Position"] = $"{_rng.Next(0, 50):D2} %",
                };
            var result = new Dictionary<string, string>();
            foreach (var (name, (pid, unit, _)) in GmEnhancedPids)
            {
                var resp = await _obdService.SendCommandAsync($"22 {pid}");
                result[name] = $"{resp} {unit}".Trim();
            }
            return result;
        }

        /// <summary>
        /// Read GM Module SPS (Service Programming System) information.
        /// Used before and after module reprogramming.
        /// </summary>
        public async Task<Dictionary<string, string>> ReadSpsInfoAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["Calibration ID"] = $"12{_rng.Next(100000, 999999):D6}",
                    ["Software Version"] = $"{_rng.Next(1, 9):D2}.{_rng.Next(0, 99):D2}.{_rng.Next(0, 99):D2}",
                    ["SPS Status"] = "Not in progress",
                    ["Module Config"] = "Customer vehicle configuration",
                    ["VCI Status"] = "TIS2WEB not required",
                };
            var resp = await SendUdsAsync(ecuAddress, "22 F1 80");
            return new Dictionary<string, string> { ["SPS Info"] = resp };
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            // GM Oil Life Monitor reset via BCM
            if (!IsSimulation)
            {
                await SendUdsAsync(0x7E4, "10 03"); // Extended session on BCM
                await SendUdsAsync(0x7E4, "2E F1 A2 64"); // Set oil life to 100%
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
                    new() { TestId = 1, Name = "Injector #1 Balance",     Description = "Cylinder 1 injector balance test",   EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 2, Name = "Injector #2 Balance",     Description = "Cylinder 2 injector balance test",   EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 3, Name = "Fuel Pump Relay",         Description = "Activate fuel pump relay",           EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 4, Name = "EVAP Solenoid",           Description = "Activate EVAP purge solenoid",       EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 5, Name = "EGR Valve",               Description = "Open EGR valve",                     EcuAddress = ecuAddress, Category = "Emissions", RequiresEngineRunning = true },
                    new() { TestId = 6, Name = "Fan Relay Low",           Description = "Activate cooling fan (low)",         EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 7, Name = "Fan Relay High",          Description = "Activate cooling fan (high)",        EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 8, Name = "Camshaft Actuator",       Description = "Activate VVT solenoid",              EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 9, Name = "Throttle Sweep",          Description = "Sweep electronic throttle",          EcuAddress = ecuAddress, Category = "Induction", RequiresEngineOff = true },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }
    }
}
