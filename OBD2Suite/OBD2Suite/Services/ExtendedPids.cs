namespace OBD2Suite.Services
{
    /// <summary>
    /// Extended and manufacturer-specific PIDs beyond standard OBD-II.
    /// Includes additional PIDs for better vehicle coverage and diagnostics.
    /// </summary>
    public static class ExtendedPids
    {
        // ── Mode 01 Extended PIDs (0x61-0x9F) ────────────────────────────────

        // Driver demand engine torque
        public const byte PID_DRIVER_DEMAND_TORQUE = 0x61;
        // Actual engine torque
        public const byte PID_ACTUAL_ENGINE_TORQUE = 0x62;
        // Engine reference torque
        public const byte PID_ENGINE_REFERENCE_TORQUE = 0x63;
        // Engine percent torque data
        public const byte PID_ENGINE_TORQUE_DATA = 0x64;
        // Auxiliary input / output supported
        public const byte PID_AUX_INPUT_OUTPUT = 0x65;
        // Mass air flow sensor
        public const byte PID_MAF_SENSOR = 0x66;
        // Engine coolant temperature
        public const byte PID_ENGINE_COOLANT_TEMP_EXTENDED = 0x67;
        // Intake air temperature sensor
        public const byte PID_INTAKE_TEMP_SENSOR = 0x68;
        // EGR commanded and actual
        public const byte PID_EGR_COMMANDED_ACTUAL = 0x69;
        // Commanded diesel intake air flow
        public const byte PID_DIESEL_INTAKE_AIR_FLOW = 0x6A;
        // EGR temperature
        public const byte PID_EGR_TEMPERATURE = 0x6B;
        // Commanded throttle actuator
        public const byte PID_THROTTLE_ACTUATOR_COMMANDED = 0x6C;
        // Fuel pressure control system
        public const byte PID_FUEL_PRESSURE_CONTROL = 0x6D;
        // Injection pressure control system
        public const byte PID_INJECTION_PRESSURE_CONTROL = 0x6E;
        // Turbocharger compressor inlet pressure
        public const byte PID_TURBO_COMPRESSOR_INLET_PRESSURE = 0x6F;
        // Boost pressure control
        public const byte PID_BOOST_PRESSURE_CONTROL = 0x70;
        // Variable geometry turbo control
        public const byte PID_VGT_CONTROL = 0x71;
        // Wastegate control
        public const byte PID_WASTEGATE_CONTROL = 0x72;
        // Exhaust pressure
        public const byte PID_EXHAUST_PRESSURE = 0x73;
        // Turbocharger RPM
        public const byte PID_TURBO_RPM = 0x74;
        // Turbocharger temperature
        public const byte PID_TURBO_TEMPERATURE = 0x75;
        // Charge air cooler temperature
        public const byte PID_CHARGE_AIR_COOLER_TEMP = 0x77;
        // Exhaust gas temperature bank 1
        public const byte PID_EXHAUST_TEMP_BANK1 = 0x78;
        // Exhaust gas temperature bank 2
        public const byte PID_EXHAUST_TEMP_BANK2 = 0x79;
        // Diesel particulate filter
        public const byte PID_DPF_STATUS = 0x7C;
        // NOx sensor
        public const byte PID_NOX_SENSOR = 0x83;
        // Manifold surface temperature
        public const byte PID_MANIFOLD_SURFACE_TEMP = 0x84;
        // NOx reagent system
        public const byte PID_NOX_REAGENT_SYSTEM = 0x85;
        // Particulate matter sensor
        public const byte PID_PM_SENSOR = 0x86;
        // Intake manifold absolute pressure
        public const byte PID_INTAKE_MANIFOLD_PRESSURE_EXTENDED = 0x87;

        // ── Manufacturer-Specific PIDs ───────────────────────────────────────

        // Toyota/Lexus specific PIDs (Mode 01, typically 0x21-0x40 range)
        public static class Toyota
        {
            public const byte PID_HYBRID_BATTERY_VOLTAGE = 0x21;
            public const byte PID_HYBRID_BATTERY_CURRENT = 0x22;
            public const byte PID_HYBRID_BATTERY_TEMP = 0x23;
            public const byte PID_HYBRID_BATTERY_SOC = 0x24;
            public const byte PID_MG1_RPM = 0x25;
            public const byte PID_MG2_RPM = 0x26;
            public const byte PID_INVERTER_TEMP = 0x27;
        }

        // VAG Group specific PIDs (VW, Audi, Skoda, Seat)
        public static class VAG
        {
            public const byte PID_BOOST_PRESSURE_ACTUAL = 0x21;
            public const byte PID_BOOST_PRESSURE_SPECIFIED = 0x22;
            public const byte PID_INJECTION_TIMING = 0x23;
            public const byte PID_FUEL_FLOW_RATE = 0x24;
            public const byte PID_TRANSMISSION_OIL_TEMP = 0x25;
            public const byte PID_DSG_CLUTCH_TEMP = 0x26;
        }

        // BMW specific PIDs
        public static class BMW
        {
            public const byte PID_TURBO_WASTE_GATE_POSITION = 0x21;
            public const byte PID_TRANSMISSION_TEMP = 0x22;
            public const byte PID_DIFFERENTIAL_TEMP = 0x23;
            public const byte PID_DME_VOLTAGE = 0x24;
        }

        // Mercedes specific PIDs
        public static class Mercedes
        {
            public const byte PID_GLOW_PLUG_RELAY = 0x21;
            public const byte PID_PARTICLE_FILTER_LOAD = 0x22;
            public const byte PID_ADBLUE_LEVEL = 0x23;
            public const byte PID_AUXILIARY_BATTERY_VOLTAGE = 0x24;
        }

        // Ford specific PIDs
        public static class Ford
        {
            public const byte PID_TRANSMISSION_RANGE = 0x21;
            public const byte PID_4WD_STATUS = 0x22;
            public const byte PID_TPMS_STATUS = 0x23;
        }

        // GM specific PIDs
        public static class GM
        {
            public const byte PID_TRANSMISSION_FLUID_TEMP = 0x21;
            public const byte PID_AFM_STATUS = 0x22; // Active Fuel Management
            public const byte PID_BRAKE_FLUID_PRESSURE = 0x23;
        }

        // ── Mode 06 Test IDs (On-Board Test Results) ────────────────────────

        // Catalyst monitoring
        public const byte TEST_CATALYST_MONITOR = 0x01;
        // Heated catalyst monitoring
        public const byte TEST_HEATED_CATALYST = 0x02;
        // Evaporative system
        public const byte TEST_EVAP_SYSTEM = 0x03;
        // Secondary air system
        public const byte TEST_SECONDARY_AIR = 0x04;
        // A/C system refrigerant
        public const byte TEST_AC_REFRIGERANT = 0x05;
        // Oxygen sensor
        public const byte TEST_O2_SENSOR = 0x06;
        // Oxygen sensor heater
        public const byte TEST_O2_HEATER = 0x07;
        // EGR system
        public const byte TEST_EGR_SYSTEM = 0x08;

        // ── J1939 Protocol (Heavy Duty / Trucks) ─────────────────────────────

        public static class J1939
        {
            // Engine related SPNs (Suspect Parameter Numbers)
            public const int SPN_ENGINE_SPEED = 190;
            public const int SPN_ENGINE_COOLANT_TEMP = 110;
            public const int SPN_ENGINE_OIL_PRESSURE = 100;
            public const int SPN_ENGINE_OIL_TEMP = 175;
            public const int SPN_FUEL_RATE = 183;
            public const int SPN_ENGINE_HOURS = 247;
            public const int SPN_BOOST_PRESSURE = 102;
            public const int SPN_INTAKE_MANIFOLD_TEMP = 105;
            public const int SPN_EXHAUST_GAS_TEMP = 173;
            
            // Vehicle related SPNs
            public const int SPN_VEHICLE_SPEED = 84;
            public const int SPN_CRUISE_CONTROL_STATUS = 527;
            public const int SPN_FUEL_LEVEL = 96;
            public const int SPN_TRIP_DISTANCE = 244;
            public const int SPN_TOTAL_DISTANCE = 245;
            
            // Transmission SPNs
            public const int SPN_TRANSMISSION_GEAR = 523;
            public const int SPN_TRANSMISSION_OIL_TEMP = 177;
            public const int SPN_CLUTCH_PRESSURE = 122;
            
            // Brake system SPNs
            public const int SPN_SERVICE_BRAKE_PRESSURE = 124;
            public const int SPN_PARKING_BRAKE_STATUS = 70;
            public const int SPN_ABS_STATUS = 1436;
            
            // Emissions SPNs
            public const int SPN_DPF_SOOT_LOAD = 3719;
            public const int SPN_DPF_INLET_PRESSURE = 3251;
            public const int SPN_DEF_TANK_LEVEL = 1761;
            public const int SPN_NOX_LEVEL = 3226;
        }

        /// <summary>
        /// Gets a human-readable description for extended PIDs.
        /// </summary>
        public static string GetPidDescription(byte pid)
        {
            return pid switch
            {
                PID_DRIVER_DEMAND_TORQUE => "Driver Demand Engine % Torque",
                PID_ACTUAL_ENGINE_TORQUE => "Actual Engine % Torque",
                PID_ENGINE_REFERENCE_TORQUE => "Engine Reference Torque",
                PID_TURBO_RPM => "Turbocharger Speed",
                PID_TURBO_TEMPERATURE => "Turbocharger Temperature",
                PID_EXHAUST_TEMP_BANK1 => "Exhaust Temperature Bank 1",
                PID_EXHAUST_TEMP_BANK2 => "Exhaust Temperature Bank 2",
                PID_DPF_STATUS => "Diesel Particulate Filter",
                PID_NOX_SENSOR => "NOx Sensor",
                PID_PM_SENSOR => "Particulate Matter Sensor",
                PID_BOOST_PRESSURE_CONTROL => "Boost Pressure Control",
                PID_VGT_CONTROL => "Variable Geometry Turbo Control",
                PID_WASTEGATE_CONTROL => "Wastegate Control",
                PID_EXHAUST_PRESSURE => "Exhaust Pressure",
                PID_CHARGE_AIR_COOLER_TEMP => "Charge Air Cooler Temperature",
                PID_EGR_TEMPERATURE => "EGR Temperature",
                PID_DIESEL_INTAKE_AIR_FLOW => "Diesel Intake Air Flow",
                PID_NOX_REAGENT_SYSTEM => "NOx Reagent System (AdBlue/DEF)",
                _ => $"Extended PID 0x{pid:X2}"
            };
        }

        /// <summary>
        /// Checks if a PID is likely supported by diesel engines.
        /// </summary>
        public static bool IsDieselSpecific(byte pid)
        {
            return pid is 
                PID_DPF_STATUS or
                PID_NOX_SENSOR or
                PID_NOX_REAGENT_SYSTEM or
                PID_DIESEL_INTAKE_AIR_FLOW or
                PID_INJECTION_PRESSURE_CONTROL or
                >= 0x78 and <= 0x79 or  // Exhaust temps
                PID_EGR_TEMPERATURE;
        }

        /// <summary>
        /// Checks if a PID is likely supported by turbocharged engines.
        /// </summary>
        public static bool IsTurboSpecific(byte pid)
        {
            return pid is
                PID_TURBO_RPM or
                PID_TURBO_TEMPERATURE or
                PID_TURBO_COMPRESSOR_INLET_PRESSURE or
                PID_BOOST_PRESSURE_CONTROL or
                PID_VGT_CONTROL or
                PID_WASTEGATE_CONTROL or
                PID_CHARGE_AIR_COOLER_TEMP;
        }

        /// <summary>
        /// Checks if a PID is likely supported by hybrid/electric vehicles.
        /// </summary>
        public static bool IsHybridSpecific(byte pid)
        {
            return pid >= Toyota.PID_HYBRID_BATTERY_VOLTAGE 
                && pid <= Toyota.PID_INVERTER_TEMP;
        }
    }
}
