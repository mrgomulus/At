using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// BMW Group (BMW / Mini / Rolls-Royce) specific diagnostics.
    /// Supports D-CAN (ISO 15765), PT-CAN, K-CAN protocols.
    /// BMW-specific features: FA/FDL coding, ZCS coding, SZL adaptation,
    /// FAS (Fahrzeuganpassungsspeicher), KOMBI reset, CBS service reset.
    /// </summary>
    public class BmwService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        // BMW ECU addresses on D-CAN
        public static readonly Dictionary<int, string> BmwEcuAddresses = new()
        {
            [0x12] = "DME — Digital Motor Electronics (Engine)",
            [0x18] = "EGS — Electronic Gearbox Control",
            [0x1C] = "DSC — Dynamic Stability Control",
            [0x13] = "EPS — Electric Power Steering",
            [0x62] = "ACSM — Airbag / Crash Safety Module",
            [0x63] = "KAFAS — Camera-Based Driver Assist",
            [0x60] = "KOMBI — Instrument Cluster",
            [0x77] = "CAS — Car Access System (Immobilizer)",
            [0x40] = "LM — Lamp Module",
            [0x72] = "FRM — Footwell Module",
            [0x5E] = "IHKA — HVAC Control",
            [0x00] = "ZGM — Central Gateway Module",
            [0x50] = "PDC — Park Distance Control",
            [0x76] = "SZL — Steering Column Switch Cluster",
            [0x67] = "JBE — Junction Box Electronics",
            [0x6F] = "TCU — Telematics Control Unit",
            [0x56] = "RAD — Radio / HiFi",
            [0x37] = "CCC — Car Communication Computer (Nav)",
            [0x26] = "SMG — Sequential Manual Gearbox",
            [0x34] = "SHD — Sliding Headliner",
            [0x71] = "EHC — Electronic Height Control",
            [0x45] = "VDM — Vertical Dynamics Management",
        };

        // BMW CBS (Condition Based Service) items
        private static readonly Dictionary<int, string> CbsItems = new()
        {
            [0x01] = "Engine Oil",
            [0x02] = "Spark Plugs",
            [0x03] = "Front Brake Fluid",
            [0x04] = "Rear Brake Fluid",
            [0x05] = "Front Brake Pads",
            [0x06] = "Rear Brake Pads",
            [0x07] = "Air Filter",
            [0x08] = "Microfilter / Cabin Filter",
            [0x0B] = "Vehicle Inspection",
            [0x0C] = "Exhaust Inspection",
        };

        public BmwService(IObdService obdService, Manufacturer manufacturer = Manufacturer.BMW)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x12, 0x18, 0x1C, 0x60, 0x62, 0x72, 0x77, 0x76, 0x40, 0x5E, 0x50 };
            foreach (var addr in addresses)
            {
                var name = BmwEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X2}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "D-CAN (ISO 15765)",
                    PartNumber = $"61{_rng.Next(10000, 99999):D5}",
                    SoftwareVersion = $"{_rng.Next(1, 9):D2}.{_rng.Next(0, 9):D2}.{_rng.Next(0, 9):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 9):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// Read BMW FA (Fahrzeugauftrag — Vehicle Order) coding string.
        /// This encodes all factory options fitted to the vehicle.
        /// </summary>
        public async Task<string> ReadFaAsync(int ecuAddress = 0x60)
        {
            await Task.Delay(IsSimulation ? 300 : 1000);
            if (IsSimulation)
                return $"FA{_rng.Next(100000, 999999):D6}_S{_rng.Next(10, 99):D2}A{_rng.Next(100, 999):D3}";
            var resp = await SendUdsAsync(ecuAddress, "22 F1 90");
            return ParseAsciiBytes(resp);
        }

        /// <summary>
        /// Read ZCS (Zentralcodierspeicher — Central Coding Memory).
        /// </summary>
        public async Task<Dictionary<string, string>> ReadZcsAsync(int ecuAddress = 0x60)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["ZCS_ECU"] = $"{_rng.Next(0xFFFF):X4}",
                    ["ZCS_NAVI"] = $"{_rng.Next(0xFFFF):X4}",
                    ["ZCS_BASE"] = $"{_rng.Next(0xFFFF):X4}",
                    ["ZCS_EHC"] = $"{_rng.Next(0xFF):X2}",
                };
            var result = new Dictionary<string, string>();
            var resp = await SendUdsAsync(ecuAddress, "22 F1 91");
            result["ZCS"] = resp;
            return result;
        }

        /// <summary>
        /// Read CBS (Condition Based Service) remaining distances/times.
        /// </summary>
        public async Task<Dictionary<string, string>> ReadCbsAsync(int ecuAddress = 0x60)
        {
            await Task.Delay(IsSimulation ? 200 : 800);
            var result = new Dictionary<string, string>();
            if (IsSimulation)
            {
                foreach (var (id, name) in CbsItems)
                    result[name] = $"{_rng.Next(0, 25000):D5} km / {_rng.Next(0, 24):D2} months";
                return result;
            }
            var resp = await SendUdsAsync(ecuAddress, "22 40 00"); // BMW-specific CBS read
            result["CBS Raw"] = resp;
            return result;
        }

        /// <summary>
        /// Reset a BMW CBS service item.
        /// </summary>
        public async Task<bool> ResetCbsItemAsync(int ecuAddress, int cbsId)
        {
            await Task.Delay(IsSimulation ? 400 : 1500);
            if (!IsSimulation)
                await SendUdsAsync(ecuAddress, $"2E 40 {cbsId:X2} FF FF FF FF");
            return true;
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            // Reset oil service (CBS item 0x01) on KOMBI (0x60)
            return await ResetCbsItemAsync(0x60, 0x01);
        }

        /// <summary>
        /// BMW SZL (Steering Column Switch Cluster) adaptation — e.g. steering angle sensor reset.
        /// </summary>
        public override async Task<bool> PerformSteeringAngleCalibrationAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(0x76, "10 03"); // Extended session on SZL
                await SendUdsAsync(0x76, "2C 03 01 00"); // Reset steering angle offset
            }
            else
                await Task.Delay(800);
            return true;
        }

        public override async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            if (ecuAddress == 0x12) // DME
                return new List<ActuatorTest>
                {
                    new() { TestId = 1, Name = "Fuel Injectors",         Description = "Cycle all injectors",              EcuAddress = ecuAddress, Category = "Fuel" },
                    new() { TestId = 2, Name = "VANOS Solenoid",          Description = "Activate variable valve timing",   EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 3, Name = "Throttle Actuator",       Description = "Sweep throttle plate",             EcuAddress = ecuAddress, Category = "Induction" },
                    new() { TestId = 4, Name = "Secondary Air Pump",      Description = "Activate cold-start air pump",     EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 5, Name = "Cooling Fan",             Description = "Activate electric cooling fan",    EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 6, Name = "Leak Detection Pump",     Description = "EVAP leak detection pump test",    EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 7, Name = "Fuel Pump",               Description = "Activate high-pressure fuel pump", EcuAddress = ecuAddress, Category = "Fuel" },
                };
            if (ecuAddress == 0x40) // LM — Lamp Module
                return new List<ActuatorTest>
                {
                    new() { TestId = 1, Name = "Low Beam Left",   Description = "Activate low beam headlight (left)",  EcuAddress = ecuAddress, Category = "Lighting" },
                    new() { TestId = 2, Name = "Low Beam Right",  Description = "Activate low beam headlight (right)", EcuAddress = ecuAddress, Category = "Lighting" },
                    new() { TestId = 3, Name = "High Beam",       Description = "Activate high beam headlights",       EcuAddress = ecuAddress, Category = "Lighting" },
                    new() { TestId = 4, Name = "Fog Lights",      Description = "Activate front fog lights",           EcuAddress = ecuAddress, Category = "Lighting" },
                    new() { TestId = 5, Name = "Turn Signal Left",Description = "Flash left turn signal",              EcuAddress = ecuAddress, Category = "Lighting" },
                    new() { TestId = 6, Name = "Turn Signal Right",Description = "Flash right turn signal",            EcuAddress = ecuAddress, Category = "Lighting" },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }

        public override async Task<Dictionary<string, string>> ReadEcuIdentificationAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 200 : 1000);
            if (IsSimulation)
            {
                var name = BmwEcuAddresses.TryGetValue(ecuAddress, out var n) ? n : $"ECU 0x{ecuAddress:X2}";
                return new Dictionary<string, string>
                {
                    ["ECU Address"] = $"0x{ecuAddress:X2}",
                    ["ECU Name"] = name,
                    ["BMW Part Number"] = $"61 35 {_rng.Next(1000, 9999):D4} xxx",
                    ["Hardware Number"] = $"61 35 {_rng.Next(1000, 9999):D4} xxx",
                    ["Software Number"] = $"{_rng.Next(1, 9):D2}.{_rng.Next(0, 99):D2}.{_rng.Next(0, 99):D2}",
                    ["ECU Manufacturer"] = "Bosch / Continental / Siemens",
                    ["Boot Software Version"] = $"B{_rng.Next(1, 9):D2}.{_rng.Next(0, 9):D2}",
                    ["VIN Stored"] = $"WBSBL91{_rng.Next(0, 9)}G{_rng.Next(100000, 999999):D6}",
                    ["Production Date"] = $"{_rng.Next(2018, 2024)}-{_rng.Next(1, 12):D2}-{_rng.Next(1, 28):D2}",
                };
            }
            return await base.ReadEcuIdentificationAsync(ecuAddress);
        }
    }
}
