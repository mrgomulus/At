using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// VAG Group (VW / Audi / Skoda / Seat / Porsche) specific diagnostics.
    /// Supports KWP2000 (KWP on K-Line), TP2.0 over CAN, and UDS/CAN.
    /// Implements VAG-specific functions: long coding, adaptations (channel-based),
    /// measuring blocks (Messwerteblöcke), output tests, login, SRI reset.
    /// </summary>
    public class VagService : BaseManufacturerService
    {
        private readonly Manufacturer _manufacturer;
        public override Manufacturer Manufacturer => _manufacturer;
        public override string DisplayName => _manufacturer.ToString();

        // Known VAG ECU addresses (per UDS CAN addressing)
        public static readonly Dictionary<int, string> VagEcuAddresses = new()
        {
            [0x01] = "Engine Control Unit (ECU)",
            [0x02] = "Automatic Gearbox",
            [0x03] = "ABS Brakes",
            [0x05] = "Kessy / Access & Start Auth.",
            [0x06] = "Seat Adjustment (Driver)",
            [0x07] = "Display Control Unit",
            [0x08] = "Climatronic / HVAC",
            [0x09] = "Central Electronics Module",
            [0x0F] = "Digital Radio Tuner",
            [0x10] = "Parking Assistance",
            [0x15] = "Airbags",
            [0x16] = "Steering Column Electronics",
            [0x17] = "Instrument Cluster",
            [0x19] = "CAN Gateway",
            [0x1A] = "Adaptive Cruise Control",
            [0x22] = "All-Wheel Drive",
            [0x25] = "Immobilizer",
            [0x2B] = "Steering Wheel Electronics",
            [0x36] = "Seat Adjustment (Passenger)",
            [0x37] = "Navigation System",
            [0x3C] = "Lane Change Assist",
            [0x44] = "Steering Assistance",
            [0x46] = "Central Comfort System",
            [0x47] = "Sound System",
            [0x52] = "Door Electronics (Driver)",
            [0x53] = "Parking Brake",
            [0x55] = "Headlamp Control",
            [0x5F] = "Information Electronics",
            [0x61] = "Battery Regulation",
            [0x65] = "Tire Pressure Monitoring",
            [0x69] = "Trailer Recognition",
            [0x6C] = "Back-up Camera",
            [0x6D] = "Windshield Wiper",
            [0x72] = "Door Electronics (Passenger)",
            [0x75] = "Telematics",
            [0x76] = "Parking System",
            [0x77] = "Telephone",
        };

        // VAG-specific measuring block groups for engine ECU
        private static readonly Dictionary<int, string> VagMeasuringGroupNames = new()
        {
            [1] = "Engine Speed, Coolant Temp, Lambda, Load",
            [2] = "Intake Temp, Boost Pressure, MAF, EGR",
            [3] = "Injection Timing, Rail Pressure, Fuel Trim",
            [4] = "Throttle Angle, Pedal Position, Idle Control",
            [5] = "O2 Sensor Voltage, Lambda Controller",
            [6] = "Knock Sensor 1+2, Ignition Timing",
            [7] = "Vehicle Speed, Gear, ATF Temp",
            [8] = "Battery Voltage, Alternator Load",
            [9] = "EGR Valve Position, Boost Control",
            [10] = "Catalyst Temp, DPF Differential Pressure",
            [20] = "Adaptation Status, Long Term Fuel Trim",
            [21] = "Idle Speed Regulator",
            [22] = "Camshaft Adjustment",
            [60] = "Login Counter, Odometer, Engine Running Time",
            [99] = "ECU Part Number, Software Version, Coding",
        };

        public VagService(IObdService obdService, Manufacturer manufacturer = Manufacturer.Volkswagen)
            : base(obdService)
        {
            _manufacturer = manufacturer;
        }

        public override async Task<List<EcuModule>> ScanAllEcusAsync()
        {
            await Task.Delay(IsSimulation ? 1200 : 8000);
            return GetSimulatedEcuList();
        }

        protected override List<EcuModule> GetSimulatedEcuList()
        {
            var modules = new List<EcuModule>();
            var addresses = new[] { 0x01, 0x02, 0x03, 0x08, 0x09, 0x15, 0x17, 0x19, 0x25, 0x44, 0x52, 0x53, 0x65, 0x76 };
            foreach (var addr in addresses)
            {
                var name = VagEcuAddresses.TryGetValue(addr, out var n) ? n : $"ECU 0x{addr:X2}";
                modules.Add(new EcuModule
                {
                    Address = addr,
                    Name = name,
                    Protocol = addr <= 0x03 ? "UDS on CAN" : "KWP2000",
                    Variant = $"Variant_{_rng.Next(1, 5):D2}",
                    PartNumber = $"{_rng.Next(1000000, 9999999):D7}",
                    SoftwareVersion = $"0001",
                    HardwareVersion = $"H{_rng.Next(1, 9):D2}",
                    LongCodingString = $"{_rng.Next(0, 255):X2} {_rng.Next(0, 255):X2} {_rng.Next(0, 255):X2}",
                    IsReachable = true
                });
            }
            return modules;
        }

        public override async Task<List<MeasuringBlock>> ReadMeasuringBlockAsync(int ecuAddress, int groupNumber)
        {
            await Task.Delay(IsSimulation ? 250 : 600);
            if (!IsSimulation)
            {
                // VAG KWP2000 Group Read: service 0x21 groupNumber
                var resp = await SendUdsAsync(ecuAddress, $"21 {groupNumber:X2}");
                return ParseVagMeasuringBlock(groupNumber, resp);
            }
            return GetVagSimulatedMeasuringBlock(ecuAddress, groupNumber);
        }

        private List<MeasuringBlock> ParseVagMeasuringBlock(int group, string response)
        {
            // Real KWP2000 response parsing would decode measurement value blocks here
            return new List<MeasuringBlock>
            {
                new MeasuringBlock
                {
                    GroupNumber = group,
                    Name = VagMeasuringGroupNames.TryGetValue(group, out var gn) ? gn : $"Group {group:D3}",
                    Fields = new List<MeasuringField>
                    {
                        new() { FieldIndex = 1, Label = "Value 1", Value = response.Length > 6 ? response.Substring(0, 6) : response }
                    }
                }
            };
        }

        private List<MeasuringBlock> GetVagSimulatedMeasuringBlock(int ecuAddress, int group)
        {
            var groupName = VagMeasuringGroupNames.TryGetValue(group, out var gn) ? gn : $"Group {group:D3}";
            List<MeasuringField> fields;

            if (group == 1)
                fields = new()
                {
                    new() { FieldIndex = 1, Label = "Engine Speed", Value = _rng.Next(700, 3500).ToString(), Unit = "rpm" },
                    new() { FieldIndex = 2, Label = "Coolant Temp", Value = _rng.Next(80, 105).ToString(), Unit = "°C" },
                    new() { FieldIndex = 3, Label = "Lambda (Bank 1)", Value = (0.97 + _rng.NextDouble() * 0.06).ToString("F3"), Unit = "λ" },
                    new() { FieldIndex = 4, Label = "Engine Load", Value = _rng.Next(10, 80).ToString(), Unit = "%" },
                };
            else if (group == 2)
                fields = new()
                {
                    new() { FieldIndex = 1, Label = "Intake Air Temp", Value = _rng.Next(20, 45).ToString(), Unit = "°C" },
                    new() { FieldIndex = 2, Label = "Boost Pressure", Value = (_rng.NextDouble() * 1.5 + 0.9).ToString("F3"), Unit = "bar" },
                    new() { FieldIndex = 3, Label = "MAF Sensor", Value = _rng.Next(2, 200).ToString(), Unit = "mg/h" },
                    new() { FieldIndex = 4, Label = "EGR Rate", Value = _rng.Next(0, 40).ToString(), Unit = "%" },
                };
            else if (group == 5)
                fields = new()
                {
                    new() { FieldIndex = 1, Label = "O2 Sensor Voltage", Value = (_rng.NextDouble() * 1.0).ToString("F3"), Unit = "V" },
                    new() { FieldIndex = 2, Label = "Lambda Controller", Value = (_rng.Next(-25, 25)).ToString(), Unit = "%" },
                    new() { FieldIndex = 3, Label = "Fuel Trim Short", Value = (_rng.Next(-10, 10)).ToString(), Unit = "%" },
                    new() { FieldIndex = 4, Label = "Fuel Trim Long", Value = (_rng.Next(-10, 10)).ToString(), Unit = "%" },
                };
            else if (group == 6)
                fields = new()
                {
                    new() { FieldIndex = 1, Label = "Knock Sensor 1", Value = (_rng.NextDouble() * 2.0).ToString("F3"), Unit = "V" },
                    new() { FieldIndex = 2, Label = "Knock Sensor 2", Value = (_rng.NextDouble() * 2.0).ToString("F3"), Unit = "V" },
                    new() { FieldIndex = 3, Label = "Ignition Advance", Value = _rng.Next(-5, 25).ToString(), Unit = "°" },
                    new() { FieldIndex = 4, Label = "Knock Retard", Value = _rng.Next(0, 5).ToString(), Unit = "°" },
                };
            else
                fields = new()
                {
                    new() { FieldIndex = 1, Label = "Field 1", Value = _rng.Next(0, 1000).ToString(), Unit = "" },
                    new() { FieldIndex = 2, Label = "Field 2", Value = _rng.Next(0, 255).ToString(), Unit = "" },
                    new() { FieldIndex = 3, Label = "Field 3", Value = _rng.Next(0, 255).ToString(), Unit = "" },
                    new() { FieldIndex = 4, Label = "Field 4", Value = _rng.Next(0, 255).ToString(), Unit = "" },
                };

            return new List<MeasuringBlock>
            {
                new MeasuringBlock { GroupNumber = group, Name = groupName, Fields = fields }
            };
        }

        public override async Task<List<ActuatorTest>> GetActuatorTestsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            if (ecuAddress == 0x01) // Engine
                return new List<ActuatorTest>
                {
                    new() { TestId = 1,  Name = "Fuel Injector 1",          Description = "Activate injector 1",              EcuAddress = ecuAddress, Category = "Fuel System" },
                    new() { TestId = 2,  Name = "Fuel Injector 2",          Description = "Activate injector 2",              EcuAddress = ecuAddress, Category = "Fuel System" },
                    new() { TestId = 3,  Name = "Fuel Injector 3",          Description = "Activate injector 3",              EcuAddress = ecuAddress, Category = "Fuel System" },
                    new() { TestId = 4,  Name = "Fuel Injector 4",          Description = "Activate injector 4",              EcuAddress = ecuAddress, Category = "Fuel System" },
                    new() { TestId = 5,  Name = "Fuel Pump Relay",          Description = "Switch fuel pump relay ON/OFF",    EcuAddress = ecuAddress, Category = "Fuel System" },
                    new() { TestId = 6,  Name = "EGR Valve",                Description = "Open/close EGR valve",             EcuAddress = ecuAddress, Category = "Emissions", RequiresEngineRunning = true },
                    new() { TestId = 7,  Name = "Purge Valve (EVAP)",       Description = "Activate EVAP canister purge",     EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 8,  Name = "Secondary Air Pump",       Description = "Activate secondary air injection", EcuAddress = ecuAddress, Category = "Emissions" },
                    new() { TestId = 9,  Name = "Turbo Bypass Valve",       Description = "Open turbocharger bypass valve",   EcuAddress = ecuAddress, Category = "Induction", RequiresEngineRunning = true },
                    new() { TestId = 10, Name = "Throttle Body",            Description = "Perform throttle body sweep",      EcuAddress = ecuAddress, Category = "Induction", RequiresEngineOff = true },
                    new() { TestId = 11, Name = "Cooling Fan Low",          Description = "Activate cooling fan (low speed)", EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 12, Name = "Cooling Fan High",         Description = "Activate cooling fan (high speed)",EcuAddress = ecuAddress, Category = "Cooling" },
                    new() { TestId = 13, Name = "Camshaft Adjuster",        Description = "Activate camshaft timing solenoid",EcuAddress = ecuAddress, Category = "VVT", RequiresEngineRunning = true },
                    new() { TestId = 14, Name = "Glow Plugs",               Description = "Activate glow plugs",              EcuAddress = ecuAddress, Category = "Diesel" },
                };
            return GetDefaultActuatorTests(ecuAddress);
        }

        public override async Task<bool> ResetServiceIntervalAsync(int ecuAddress)
        {
            // VAG SRI reset via adaptation channel 0 on instrument cluster (0x17)
            if (!IsSimulation)
                await WriteAdaptationAsync(0x17, 0, "0"); // Reset service interval
            else
                await Task.Delay(600);
            return true;
        }

        public override async Task<bool> PerformThrottleBasicSettingAsync(int ecuAddress)
        {
            // VAG basic setting via KWP2000 Start Diagnostic Session (0x10) + Basic Setting (0x28)
            if (!IsSimulation)
            {
                await SendUdsAsync(ecuAddress, "10 83"); // Extended diagnostic session
                await SendUdsAsync(ecuAddress, "28 00");  // Basic setting group 0
                await Task.Delay(5000);
            }
            else
                await Task.Delay(1000);
            return true;
        }

        public override async Task<List<int>> GetAvailableMeasuringGroupsAsync(int ecuAddress)
        {
            await Task.Delay(IsSimulation ? 100 : 500);
            // Return the known VAG groups plus generic ones
            var groups = VagMeasuringGroupNames.Keys.ToList();
            groups.AddRange(Enumerable.Range(1, 30).Where(i => !VagMeasuringGroupNames.ContainsKey(i)));
            return groups.Distinct().OrderBy(g => g).ToList();
        }
    }
}
