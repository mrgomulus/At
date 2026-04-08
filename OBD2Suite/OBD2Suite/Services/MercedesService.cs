using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Mercedes-Benz Group (Mercedes / Smart) specific diagnostics.
    /// Supports CAN, SDI (Serial Diagnostic Interface), KWP2000 protocols.
    /// Mercedes-specific features: SCN coding (Software Calibration Number),
    /// variant coding, Xentry/DAS-equivalent functions, SBC adaptation.
    /// </summary>
    public class MercedesService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> MercedesEcuAddresses = new()
        {
            [0x07] = "ME — Motor Electronics (Petrol Engine)",
            [0x01] = "CDI — Common Rail Diesel Electronics",
            [0x02] = "ETC — Electronic Transmission Control",
            [0x03] = "ESP — Electronic Stability Program",
            [0x08] = "EIS — Electronic Ignition Switch",
            [0x09] = "IC — Instrument Cluster",
            [0x0A] = "AAC — Automatic A/C Control",
            [0x12] = "SRS — Supplemental Restraint System (Airbag)",
            [0x18] = "PTS — Parktronic System",
            [0x19] = "HHC — Headlamp Range Control",
            [0x1A] = "COMAND — Navigation / Head Unit",
            [0x1D] = "AGN — Power Antenna",
            [0x1E] = "TCM — Trailer Coupling Module",
            [0x20] = "SAM-F — Signal Acquisition Module (Front)",
            [0x21] = "SAM-R — Signal Acquisition Module (Rear)",
            [0x25] = "KFB — Keyless-Go Control Module",
            [0x29] = "GLA — Liftgate Control",
            [0x3D] = "DTR — Active Body Control",
            [0x50] = "BAS — Brake Assist System",
            [0x56] = "EPS — Electric Power Steering",
            [0x60] = "Distronic — Adaptive Cruise Control",
        };

        public MercedesService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Mercedes)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x07, 0x02, 0x03, 0x08, 0x09, 0x12, 0x20, 0x21, 0x25, 0x50 };
            foreach (var addr in addresses)
            {
                var name = MercedesEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X2}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "CAN / KWP2000",
                    PartNumber = $"A {_rng.Next(100, 999):D3} {_rng.Next(100, 999):D3} {_rng.Next(10, 99):D2} {_rng.Next(10, 99):D2}",
                    SoftwareVersion = $"v{_rng.Next(1, 30):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 5):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// SCN coding: reads the Software Calibration Number required for Xentry/DAS
        /// online variant coding. In real implementation this requires server auth.
        /// </summary>
        public async Task<string> ReadScnCodingAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 300 : 1000);
            if (IsSimulation)
                return $"SCN-{_rng.Next(1000000, 9999999):D7}-{_rng.Next(10000, 99999):D5}";
            var resp = await SendUdsAsync(ecuAddress, "22 40 00");
            return resp;
        }

        /// <summary>
        /// Mercedes variant coding — reads the variant code and active options.
        /// </summary>
        public async Task<Dictionary<string, string>> ReadVariantCodingAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["Variant Code"] = $"{_rng.Next(0xFFFF):X4}",
                    ["Country Variant"] = new[] { "Europe", "USA", "Japan", "Rest of World" }[_rng.Next(4)],
                    ["Language"] = new[] { "German", "English", "French", "Spanish" }[_rng.Next(4)],
                    ["Units"] = new[] { "Metric", "Imperial" }[_rng.Next(2)],
                    ["Fuel Type"] = new[] { "Petrol", "Diesel", "Hybrid" }[_rng.Next(3)],
                    ["Emissions Standard"] = new[] { "Euro 6d", "EU6b", "EU5" }[_rng.Next(3)],
                };
            var resp = await SendUdsAsync(ecuAddress, "22 F1 88");
            return new Dictionary<string, string> { ["Variant"] = resp };
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(ecuAddress, "10 03"); // Extended session
                await SendUdsAsync(ecuAddress, "2E F1 A0 FF FF FF FF"); // Reset assyst service
            }
            else
                await Task.Delay(500);
            return true;
        }

        public override async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            if (ecuAddress == 0x07) // ME — Engine
                return new List<ActuatorTest>
                {
                    new() { TestId = 1, Name = "Fuel Injectors",          Description = "Test all fuel injectors",           EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 2, Name = "High Pressure Pump",      Description = "Activate high-pressure fuel pump",  EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 3, Name = "Camshaft Adjuster Left",  Description = "Activate camshaft timing (left)",   EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 4, Name = "Camshaft Adjuster Right", Description = "Activate camshaft timing (right)",  EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 5, Name = "Air Pump",                Description = "Activate secondary air injection",  EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 6, Name = "EGR Valve",               Description = "Open/close EGR valve",              EcuAddress = ecuAddress, Category = "Emissions", RequiresEngineRunning = true },
                    new() { TestId = 7, Name = "Throttle Actuator",       Description = "Sweep throttle plate",              EcuAddress = ecuAddress, Category = "Induction" },
                    new() { TestId = 8, Name = "Cooling Fan",             Description = "Activate radiator fan",             EcuAddress = ecuAddress, Category = "Cooling" },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }
    }
}
