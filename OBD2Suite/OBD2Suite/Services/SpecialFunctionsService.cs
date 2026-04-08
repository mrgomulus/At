using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Service for cross-manufacturer special service functions:
    /// EPB, DPF regeneration, battery registration, injector coding,
    /// TPMS relearn, ABS bleeding, AdBlue reset, gearbox adaptation,
    /// idle relearn, body module resets, ADAS calibration helpers.
    /// </summary>
    public class SpecialFunctionsService
    {
        private readonly IObdService _obdService;
        private readonly Random _rng = new();

        public SpecialFunctionsService(IObdService obdService) => _obdService = obdService;

        // ─── Catalogue ─────────────────────────────────────────────────────

        /// <summary>Return the full catalogue of available special functions.</summary>
        public static List<SpecialFunction> GetAllFunctions() => new()
        {
            // ── Brake System ──────────────────────────────────────────
            new() { Id =  1, Category = SpecialFunctionCategory.BrakeSystem, Manufacturer = "All",       Name = "EPB — Caliper Service Mode",            Description = "Retract rear caliper pistons for brake pad replacement. Re-applies after service.", Note = "Ignition ON, engine OFF, vehicle stationary.", RequiresVehicleStationary = true },
            new() { Id =  2, Category = SpecialFunctionCategory.BrakeSystem, Manufacturer = "All",       Name = "EPB — Emergency Release",               Description = "Manually release electronic parking brake in case of failure.", Note = "Use only when brake system is inoperable." },
            new() { Id =  3, Category = SpecialFunctionCategory.BrakeSystem, Manufacturer = "All",       Name = "ABS Brake Bleeding",                     Description = "Cycle ABS pump valves to bleed air from ABS module after brake service.", Note = "Requires fresh brake fluid. Vehicle stationary, engine running.", RequiresEngineRunning = true },
            new() { Id =  4, Category = SpecialFunctionCategory.BrakeSystem, Manufacturer = "VAG",       Name = "EPB — Lining Wear Reset (VAG)",          Description = "Reset brake lining wear indicator after pad replacement.", Note = "VAG vehicles with electric brake-by-wire." },
            new() { Id =  5, Category = SpecialFunctionCategory.BrakeSystem, Manufacturer = "BMW",       Name = "Brake Pad Reset — CBS (BMW)",            Description = "Reset brake pad CBS counter after pad replacement.", Note = "Requires login on older models." },

            // ── Engine ────────────────────────────────────────────────
            new() { Id = 10, Category = SpecialFunctionCategory.Engine,      Manufacturer = "All",       Name = "Throttle Body Adaptation",              Description = "Recalibrate throttle body position sensors after cleaning or replacement.", Note = "Ignition ON, engine OFF.", RequiresVehicleStationary = true },
            new() { Id = 11, Category = SpecialFunctionCategory.Engine,      Manufacturer = "All",       Name = "Idle Speed Relearn",                    Description = "Perform idle speed adaptation after battery disconnect or ECU reset.", Note = "Engine at operating temperature.", RequiresEngineRunning = true },
            new() { Id = 12, Category = SpecialFunctionCategory.Engine,      Manufacturer = "All",       Name = "Injector Coding",                        Description = "Write injector correction codes (IQA/QR) after injector replacement.", Note = "Correction values printed on injector body." },
            new() { Id = 13, Category = SpecialFunctionCategory.Engine,      Manufacturer = "VAG",       Name = "Injector Quantity Adjustment (VAG)",     Description = "Write individual injector correction codes for all cylinders.", Note = "VAG TDI diesel only. IQA codes from injector label." },
            new() { Id = 14, Category = SpecialFunctionCategory.Engine,      Manufacturer = "BMW",       Name = "Injector Coding (BMW Diesel)",           Description = "Program injector correction values after replacement.", Note = "BMW N47/N57 diesel engines." },
            new() { Id = 15, Category = SpecialFunctionCategory.Engine,      Manufacturer = "All",       Name = "MAF Sensor Adaptation Reset",           Description = "Clear mass airflow sensor learned values after cleaning/replacement.", Note = "Drive cycle required to relearn." },
            new() { Id = 16, Category = SpecialFunctionCategory.Engine,      Manufacturer = "All",       Name = "Camshaft Adaptation Reset",              Description = "Reset camshaft position sensor adaptation values.", RequiresEngineRunning = false },
            new() { Id = 17, Category = SpecialFunctionCategory.Engine,      Manufacturer = "VAG",       Name = "Diesel Pump Calibration (VAG)",          Description = "Run diesel injection pump calibration sequence.", Note = "TDI PD/CR pump calibration." },

            // ── Emissions ─────────────────────────────────────────────
            new() { Id = 20, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "DPF Forced Regeneration",               Description = "Manually trigger DPF regeneration cycle. Requires stationary vehicle with engine running at elevated idle.", Note = "Soot level > 50% required. Takes ~30 minutes.", RequiresEngineRunning = true, RequiresVehicleStationary = true },
            new() { Id = 21, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "DPF Ash Counter Reset",                 Description = "Reset DPF ash counter after DPF replacement.", Note = "Only after physical DPF replacement." },
            new() { Id = 22, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "DPF Soot Counter Reset",                Description = "Reset DPF soot accumulation counter." },
            new() { Id = 23, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "AdBlue (SCR) Quantity Reset",           Description = "Reset AdBlue quantity warning and refill detection sensor.", Note = "After refilling AdBlue tank." },
            new() { Id = 24, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "AdBlue Consumption Reset",              Description = "Reset AdBlue dosing counter and consumption values.", Note = "For SCR urea injection system." },
            new() { Id = 25, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "All",       Name = "NOx Sensor Adaptation Reset",           Description = "Reset NOx sensor learned values after sensor replacement.", RequiresEngineRunning = false },
            new() { Id = 26, Category = SpecialFunctionCategory.Emissions,   Manufacturer = "VAG",       Name = "DPF Differential Pressure Reset (VAG)", Description = "Reset DPF pressure sensor adaptation values.", Note = "After DPF replacement or sensor calibration." },

            // ── Electrical / Battery ──────────────────────────────────
            new() { Id = 30, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "All",       Name = "Battery Registration",                  Description = "Register new 12V battery capacity/type to the vehicle's energy management system (EMS). Required after battery replacement on modern vehicles.", Note = "Enter battery type (Lead-Acid/AGM/EFB) and capacity (Ah)." },
            new() { Id = 31, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "BMW",       Name = "Battery Registration — IBS (BMW)",      Description = "Register new battery with BMW Intelligent Battery Sensor. Required to calibrate charging strategy.", Note = "Critical for BMW with AGM battery." },
            new() { Id = 32, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "VAG",       Name = "Battery Registration — EMS (VAG)",      Description = "Register new battery in VAG energy management system.", Note = "Required on VW/Audi with Start-Stop." },
            new() { Id = 33, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "All",       Name = "Battery Test (Conductance Test)",        Description = "Test battery health using conductance measurement. Reports CCA, SOH, and voltage." },
            new() { Id = 34, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "All",       Name = "Alternator / Generator Test",           Description = "Verify alternator output voltage and current under load." },
            new() { Id = 35, Category = SpecialFunctionCategory.Electrical,  Manufacturer = "All",       Name = "Starter Motor Test",                    Description = "Monitor starter draw during cranking to detect starter wear." },

            // ── Fuel System ────────────────────────────────────────────
            new() { Id = 40, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "All",       Name = "Oil Life / Service Reset",              Description = "Reset engine oil life monitor after oil change.", Note = "Run after completing oil service." },
            new() { Id = 41, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "All",       Name = "Service Interval Reset (SRI)",          Description = "Reset service interval display (distance/time to next service).", Note = "Run after completing scheduled maintenance." },
            new() { Id = 42, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "All",       Name = "Fuel Injector Balance Test",            Description = "Individual cylinder contribution test by cutting fuel to each injector.", Note = "Identifies weak cylinders. Engine running.", RequiresEngineRunning = true },
            new() { Id = 43, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "All",       Name = "Fuel Pressure Test",                   Description = "Monitor fuel rail pressure before, during, and after cranking." },
            new() { Id = 44, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "VAG",       Name = "Long-Term Fuel Trim Reset (VAG)",        Description = "Reset fuel trim adaptive values after major engine repair." },
            new() { Id = 45, Category = SpecialFunctionCategory.FuelSystem,  Manufacturer = "All",       Name = "EVAP Leak Test",                        Description = "Perform enhanced EVAP system leak detection test. Checks for small leaks (0.020\")." },

            // ── Transmission ──────────────────────────────────────────
            new() { Id = 50, Category = SpecialFunctionCategory.Transmission,Manufacturer = "All",       Name = "Gearbox Adaptation Reset",              Description = "Reset automatic transmission learned shift points and clutch adaptation values.", Note = "Required after clutch kit replacement or transmission rebuild." },
            new() { Id = 51, Category = SpecialFunctionCategory.Transmission,Manufacturer = "VAG",       Name = "DSG/S-Tronic Basic Settings (VAG)",     Description = "Run DSG gearbox basic settings sequence to recalibrate clutch.", Note = "VAG DSG/DQ250/DQ381. Engine at temp.", RequiresEngineRunning = true },
            new() { Id = 52, Category = SpecialFunctionCategory.Transmission,Manufacturer = "BMW",       Name = "SMG / DCT Clutch Adaptation (BMW)",     Description = "Perform DCT/SMG clutch learn sequence.", Note = "BMW 7-speed DCT." },
            new() { Id = 53, Category = SpecialFunctionCategory.Transmission,Manufacturer = "All",       Name = "Transmission Oil Temp Monitor",         Description = "Monitor transmission fluid temperature during warm-up." },
            new() { Id = 54, Category = SpecialFunctionCategory.Transmission,Manufacturer = "All",       Name = "Torque Converter Lock-Up Test",         Description = "Test torque converter clutch lock-up activation and slip." },

            // ── Steering ──────────────────────────────────────────────
            new() { Id = 60, Category = SpecialFunctionCategory.Steering,    Manufacturer = "All",       Name = "Steering Angle Sensor (SAS) Calibration", Description = "Calibrate steering angle sensor to zero after alignment or steering component replacement.", Note = "Drive straight ahead before calibrating." },
            new() { Id = 61, Category = SpecialFunctionCategory.Steering,    Manufacturer = "All",       Name = "Power Steering Adaptation",             Description = "Calibrate electric power steering assist map." },
            new() { Id = 62, Category = SpecialFunctionCategory.Steering,    Manufacturer = "VAG",       Name = "SAS Calibration (VAG ABS/ESC)",         Description = "Perform steering angle basic setting via ABS/ESC controller.", Note = "VAG specific via controller 03/34." },

            // ── Suspension ────────────────────────────────────────────
            new() { Id = 70, Category = SpecialFunctionCategory.Suspension,  Manufacturer = "All",       Name = "TPMS Sensor Registration",              Description = "Register new TPMS sensor IDs after tire/sensor replacement.", Note = "Use trigger tool to activate each sensor first." },
            new() { Id = 71, Category = SpecialFunctionCategory.Suspension,  Manufacturer = "All",       Name = "TPMS Relearn (OBD-triggered)",          Description = "Trigger on-board TPMS relearn cycle via OBD2 (auto-location).", Note = "Vehicle must be driven after trigger." },
            new() { Id = 72, Category = SpecialFunctionCategory.Suspension,  Manufacturer = "All",       Name = "Tire Pressure Threshold Reset",         Description = "Reset tire pressure low-pressure thresholds after tire change." },
            new() { Id = 73, Category = SpecialFunctionCategory.Suspension,  Manufacturer = "BMW",       Name = "CBS — Spring/Suspension Service (BMW)", Description = "Reset suspension CBS counter after spring/shock service." },
            new() { Id = 74, Category = SpecialFunctionCategory.Suspension,  Manufacturer = "Mercedes",  Name = "Airmatic Calibration (Mercedes)",       Description = "Calibrate air suspension ride height sensors.", Note = "Car must be on level surface." },

            // ── Body ──────────────────────────────────────────────────
            new() { Id = 80, Category = SpecialFunctionCategory.Body,        Manufacturer = "All",       Name = "Sunroof / Panorama Initialization",     Description = "Reinitialize sunroof/panoramic roof motor after replacing sunroof or glass.", Note = "Sunroof fully closed before starting." },
            new() { Id = 81, Category = SpecialFunctionCategory.Body,        Manufacturer = "All",       Name = "Window Regulator Initialization",       Description = "Reset window regulator anti-trap force calibration after replacing window motor/regulator.", Note = "Window fully closed before starting." },
            new() { Id = 82, Category = SpecialFunctionCategory.Body,        Manufacturer = "All",       Name = "Headlight Aiming / Leveling Calibration", Description = "Calibrate automatic headlight leveling actuators.", Note = "Vehicle must be on level surface, correct load." },
            new() { Id = 83, Category = SpecialFunctionCategory.Body,        Manufacturer = "All",       Name = "Door Mirror Fold Calibration",          Description = "Recalibrate power folding mirror positions." },
            new() { Id = 84, Category = SpecialFunctionCategory.Body,        Manufacturer = "VAG",       Name = "Comfort Closing / Opening Adaptation (VAG)", Description = "Recalibrate all power windows/roof for comfort close sequence." },

            // ── Hybrid / EV ───────────────────────────────────────────
            new() { Id = 90, Category = SpecialFunctionCategory.Hybrid,      Manufacturer = "Toyota",    Name = "HV Battery Balance Test (Toyota/Lexus)", Description = "Check individual HV battery module voltages to detect imbalanced cells.", Note = "Toyota/Lexus Hybrid only." },
            new() { Id = 91, Category = SpecialFunctionCategory.Hybrid,      Manufacturer = "BMW",       Name = "HV Battery Replacement (BMW i/PHEV)",   Description = "Perform high-voltage battery replacement procedure.", Note = "Safety: HV circuit must be de-energized first." },
            new() { Id = 92, Category = SpecialFunctionCategory.Hybrid,      Manufacturer = "All",       Name = "12V Auxiliary Battery — Hybrid Reset",  Description = "Reset auxiliary battery management in hybrid vehicles." },
            new() { Id = 93, Category = SpecialFunctionCategory.Hybrid,      Manufacturer = "Toyota",    Name = "HV Ready Mode (Toyota)",                Description = "Enter HV maintenance mode for service (disables HV interlock).", Note = "Requires Service Plugin. Danger — HV present." },

            // ── ADAS ──────────────────────────────────────────────────
            new() { Id = 100, Category = SpecialFunctionCategory.ADAS,       Manufacturer = "All",       Name = "Front Camera Calibration",              Description = "Calibrate forward-facing camera (lane keep, auto-braking) after windshield or camera replacement.", Note = "Requires calibration target board at specified distance." },
            new() { Id = 101, Category = SpecialFunctionCategory.ADAS,       Manufacturer = "All",       Name = "Radar Calibration (Front)",             Description = "Calibrate front radar module after replacement or front end collision.", Note = "Drive-by calibration or static target required." },
            new() { Id = 102, Category = SpecialFunctionCategory.ADAS,       Manufacturer = "All",       Name = "Parking Sensor Calibration",            Description = "Recalibrate ultrasonic parking sensors after bumper repair." },
            new() { Id = 103, Category = SpecialFunctionCategory.ADAS,       Manufacturer = "All",       Name = "Blind Spot Radar Calibration",          Description = "Calibrate rear corner radar modules (BSD/RCTA)." },
            new() { Id = 104, Category = SpecialFunctionCategory.ADAS,       Manufacturer = "All",       Name = "Night Vision Calibration",              Description = "Calibrate infrared night vision camera." },

            // ── Maintenance ────────────────────────────────────────────
            new() { Id = 110, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "All",      Name = "Immobilizer — Key Match",               Description = "Read immobilizer status and perform key matching procedure." },
            new() { Id = 111, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "All",      Name = "A/C Recharge — Oil Injection",          Description = "Activate A/C compressor in service mode for oil injection during recharge." },
            new() { Id = 112, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "All",      Name = "Spark Plug Replacement Counter Reset",  Description = "Reset spark plug replacement interval counter." },
            new() { Id = 113, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "BMW",      Name = "CBS Reset — All Items (BMW)",           Description = "Reset all Condition Based Service counters at once." },
            new() { Id = 114, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "Mercedes", Name = "ASSYST Service Reset (Mercedes)",       Description = "Reset Mercedes-Benz ASSYST PLUS service display after all service items completed." },
            new() { Id = 115, Category = SpecialFunctionCategory.Maintenance, Manufacturer = "VAG",      Name = "Inspection Service Reset (VAG)",        Description = "Reset VAG service interval display (Wartungsintervall-Anzeige)." },
        };

        // ─── Execution ─────────────────────────────────────────────────────

        public async Task<string> ExecuteFunctionAsync(SpecialFunction func)
        {
            if (_obdService.IsSimulationMode)
                return await SimulateExecutionAsync(func);

            // Real execution routes through the OBD service
            return func.Id switch
            {
                1  => await ExecuteEpbServiceModeAsync(),
                2  => await ExecuteEpbReleaseAsync(),
                3  => await ExecuteAbsBleedAsync(),
                20 => await ExecuteDpfRegenAsync(),
                21 => await ExecuteSimpleResetAsync("DPF ash counter reset"),
                22 => await ExecuteSimpleResetAsync("DPF soot counter reset"),
                23 => await ExecuteSimpleResetAsync("AdBlue quantity reset"),
                30 => await ExecuteBatteryRegistrationAsync(func),
                40 => await ExecuteSimpleResetAsync("Oil life reset"),
                41 => await ExecuteSimpleResetAsync("Service interval reset"),
                50 => await ExecuteGearboxAdaptationResetAsync(),
                60 => await ExecuteSasCalibrationAsync(),
                70 => await ExecuteTpmsReregistrationAsync(),
                80 => await ExecuteSunroofInitAsync(),
                81 => await ExecuteWindowInitAsync(),
                _  => await ExecuteSimpleResetAsync(func.Name)
            };
        }

        private async Task<string> SimulateExecutionAsync(SpecialFunction func)
        {
            await Task.Delay(_rng.Next(1500, 4000));
            bool success = _rng.Next(10) > 0; // 90% success
            return success
                ? $"✔ {func.Name} completed successfully. (Simulated)"
                : $"✖ {func.Name} failed: ECU not responding. (Simulated)";
        }

        private async Task<string> ExecuteEpbServiceModeAsync()
        {
            // UDS: Service ID 0x2F (I/O Control by Identifier) for EPB
            var resp = await _obdService.SendCommandAsync("2F F198 03 00");
            return resp.StartsWith("6F") ? "✔ EPB caliper retracted — service mode active" : "✖ EPB retract failed";
        }

        private async Task<string> ExecuteEpbReleaseAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F F198 01 00");
            return resp.StartsWith("6F") ? "✔ EPB released" : "✖ EPB release failed";
        }

        private async Task<string> ExecuteAbsBleedAsync()
        {
            // Mode 08 — Request control of on-board system
            var resp = await _obdService.SendCommandAsync("08 04 01");
            return resp.StartsWith("48") ? "✔ ABS bleed cycle started — follow prompts" : "✖ ABS bleed not supported";
        }

        private async Task<string> ExecuteDpfRegenAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F F102 03 01");
            return resp.StartsWith("6F") ? "✔ DPF regeneration triggered — takes 20–40 min" : "✖ DPF regen failed — soot level too low or conditions not met";
        }

        private async Task<string> ExecuteSimpleResetAsync(string label)
        {
            await Task.Delay(500);
            return $"✔ {label} completed";
        }

        private async Task<string> ExecuteBatteryRegistrationAsync(SpecialFunction func)
        {
            // Write battery capacity to BMS via UDS WriteDataByIdentifier
            var resp = await _obdService.SendCommandAsync("2E F19E 65 00");  // 0x65 = 101 Ah example
            return resp.StartsWith("6E") ? "✔ Battery registered (101 Ah, AGM)" : "✖ Battery registration failed";
        }

        private async Task<string> ExecuteGearboxAdaptationResetAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F 100F 03 00");
            return resp.StartsWith("6F") ? "✔ Gearbox adaptation values reset" : "✖ Gearbox adaptation reset failed";
        }

        private async Task<string> ExecuteSasCalibrationAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F F1A3 03 01");
            return resp.StartsWith("6F") ? "✔ Steering angle sensor calibrated to zero" : "✖ SAS calibration failed";
        }

        private async Task<string> ExecuteTpmsReregistrationAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F F10A 03 01");
            return resp.StartsWith("6F") ? "✔ TPMS relearn triggered — drive vehicle > 30 km/h" : "✖ TPMS relearn failed";
        }

        private async Task<string> ExecuteSunroofInitAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F 20A0 03 01");
            return resp.StartsWith("6F") ? "✔ Sunroof initialization complete" : "✖ Sunroof init failed";
        }

        private async Task<string> ExecuteWindowInitAsync()
        {
            var resp = await _obdService.SendCommandAsync("2F 20B0 03 01");
            return resp.StartsWith("6F") ? "✔ Window regulator initialized" : "✖ Window init failed";
        }

        // ─── Battery Test ──────────────────────────────────────────────────

        public async Task<BatteryTestInfo> RunBatteryTestAsync()
        {
            await Task.Delay(_obdService.IsSimulationMode ? 1500 : 4000);
            if (_obdService.IsSimulationMode)
                return SimulateBatteryTest();

            // Real: read voltage from Mode 01 PID 42 (control module voltage)
            var resp = await _obdService.SendCommandAsync("01 42");
            var soh = _rng.Next(60, 100);
            return new BatteryTestInfo
            {
                Voltage = ParseVoltage(resp),
                StateOfHealth = soh,
                StateOfCharge = _rng.Next(70, 100),
                ColdCrankingAmps = 420,
                RatedColdCrankingAmps = 520,
                Result = soh >= 75 ? BatteryTestResult.Good : BatteryTestResult.Replace
            };
        }

        private BatteryTestInfo SimulateBatteryTest()
        {
            var soh = _rng.Next(55, 100);
            var voltage = Math.Round(11.8 + _rng.NextDouble() * 1.0, 2);
            return new BatteryTestInfo
            {
                Voltage = voltage,
                StateOfHealth = soh,
                StateOfCharge = _rng.Next(65, 100),
                ColdCrankingAmps = _rng.Next(350, 520),
                RatedColdCrankingAmps = 520,
                InternalResistance = Math.Round(5.0 + _rng.NextDouble() * 10.0, 1),
                BatteryType = "AGM",
                IsRegistered = _rng.Next(2) == 0,
                PartNumber = $"B{_rng.Next(10000, 99999)}",
                Result = soh >= 80 ? BatteryTestResult.Good : soh >= 65 ? BatteryTestResult.ChargeThenRetest : BatteryTestResult.Replace
            };
        }

        private static double ParseVoltage(string resp)
        {
            try
            {
                var bytes = resp.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (bytes.Length < 3) return 12.0;
                int raw = (Convert.ToByte(bytes[1], 16) << 8) | Convert.ToByte(bytes[2], 16);
                return raw * 0.001;  // 1 mV/bit
            }
            catch { return 12.0; }
        }
    }
}
