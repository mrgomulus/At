using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Stellantis PSA sub-group: Peugeot / Citroën — Diagbox-equivalent diagnostics.
    /// </summary>
    public class PsaService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> PsaEcuAddresses = new()
        {
            [0x10] = "BSI — Built-in Systems Interface (BCM)",
            [0x18] = "ECM — Engine Control Module",
            [0x12] = "EAT — Automatic Gearbox",
            [0x28] = "ABS/ESP Module",
            [0x74] = "Airbag / SRS",
            [0x16] = "IC — Instrument Cluster",
            [0x1A] = "BSM — Steering Column Module",
            [0x30] = "A/C — Air Conditioning",
            [0x5E] = "Radio / Head Unit",
            [0x42] = "Parking Sensors",
            [0x52] = "Camera Module",
            [0x8C] = "TPMS",
        };

        public PsaService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Peugeot)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x10, 0x18, 0x12, 0x28, 0x74, 0x16, 0x1A, 0x30 };
            foreach (var addr in addresses)
            {
                var name = PsaEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X2}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "CAN / KWP2000",
                    PartNumber = $"96{_rng.Next(100000, 999999):D6}",
                    SoftwareVersion = $"v{_rng.Next(1, 30):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 5):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        /// <summary>
        /// Read PSA BSI (Built-in Systems Interface) configuration parameters.
        /// </summary>
        public async Task<Dictionary<string, string>> ReadBsiConfigAsync()
        {
            await Task.Delay(IsSimulation ? 300 : 1000);
            if (IsSimulation)
                return new Dictionary<string, string>
                {
                    ["Country Code"] = new[] { "DE", "FR", "GB", "ES", "IT" }[_rng.Next(5)],
                    ["Language"] = new[] { "German", "French", "English", "Spanish" }[_rng.Next(4)],
                    ["Units"] = new[] { "Metric", "Imperial" }[_rng.Next(2)],
                    ["BSI Version"] = $"v{_rng.Next(1, 20):D2}.{_rng.Next(0, 99):D2}",
                    ["VIN Stored"] = $"VF3{_rng.Next(100000, 999999):D6}{_rng.Next(10000, 99999):D5}",
                    ["Options Fitted"] = "ESP, Start/Stop, Parking Sensors, Lane Assist",
                };
            var resp = await SendUdsAsync(0x10, "22 F1 80");
            return new Dictionary<string, string> { ["BSI Config"] = resp };
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(0x10, "10 03");
                await SendUdsAsync(0x10, "2E F1 A0 00");
            }
            else
                await Task.Delay(400);
            return true;
        }
    }

    /// <summary>
    /// Stellantis FCA sub-group: Fiat / Alfa Romeo / Lancia / Jeep — Examiner/eFPT-equivalent.
    /// </summary>
    public class FiatService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> FiatEcuAddresses = new()
        {
            [0x10] = "ECU — Engine Control Unit",
            [0x18] = "TCU — Transmission Control Unit",
            [0x14] = "ABS/VDC — Brakes",
            [0x44] = "SRS — Airbag",
            [0x16] = "BCM — Body Control Module",
            [0x1C] = "IC — Instrument Cluster",
            [0x40] = "HVAC — Climate Control",
            [0x28] = "EPS — Electric Power Steering",
            [0x58] = "Gateway (Body CAN)",
            [0x78] = "TPMS",
        };

        public FiatService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Fiat)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x10, 0x18, 0x14, 0x44, 0x16, 0x1C, 0x40 };
            foreach (var addr in addresses)
            {
                var name = FiatEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X2}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "CAN / ISO 15765",
                    PartNumber = $"51{_rng.Next(100000, 999999):D6}",
                    SoftwareVersion = $"v{_rng.Next(1, 30):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 5):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(0x16, "10 03");
                await SendUdsAsync(0x16, "2E F1 A0 00");
            }
            else
                await Task.Delay(400);
            return true;
        }
    }

    /// <summary>
    /// Renault Group (Renault / Dacia) — CAN Clip-equivalent diagnostics.
    /// </summary>
    public class RenaultService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        public static readonly Dictionary<int, string> RenaultEcuAddresses = new()
        {
            [0x7A0] = "UCE — Engine Control Unit",
            [0x7A2] = "BVA — Automatic Gearbox",
            [0x7A4] = "ABS/ESP Module",
            [0x7B0] = "UCH — Habitacle (BCM)",
            [0x7B2] = "Airbag / SRS",
            [0x7B4] = "Instrument Cluster",
            [0x7B6] = "A/C System",
            [0x7B8] = "Electric Power Steering",
            [0x7BA] = "Parking Assistance",
            [0x7BC] = "Multimedia / Head Unit",
            [0x7BE] = "TPMS",
        };

        public RenaultService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Renault)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x7A0, 0x7A2, 0x7A4, 0x7B0, 0x7B2, 0x7B4, 0x7B6 };
            foreach (var addr in addresses)
            {
                var name = RenaultEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X3}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = "CAN / ISO 15765",
                    PartNumber = $"23{_rng.Next(100000, 999999):D6}",
                    SoftwareVersion = $"v{_rng.Next(1, 30):D2}",
                    HardwareVersion = $"H{_rng.Next(1, 5):D2}",
                    LongCodingString = string.Join(" ", Enumerable.Range(0, 4).Select(_ => _rng.Next(0, 255).ToString("X2"))),
                    IsReachable = true
                });
            }
            return modules;
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            if (!IsSimulation)
            {
                await SendUdsAsync(0x7B0, "10 03");
                await SendUdsAsync(0x7B0, "2E F1 A0 00");
            }
            else
                await Task.Delay(400);
            return true;
        }
    }
}
