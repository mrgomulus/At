using System;
using System.Collections.Generic;

namespace OBD2Suite.Services
{
    public static class ElmProtocol
    {
        // AT Commands
        public const string ATZ = "ATZ";
        public const string ATE0 = "ATE0";
        public const string ATE1 = "ATE1";
        public const string ATL0 = "ATL0";
        public const string ATL1 = "ATL1";
        public const string ATH0 = "ATH0";
        public const string ATH1 = "ATH1";
        public const string ATSP0 = "ATSP0";
        public const string ATSP1 = "ATSP1";
        public const string ATSP2 = "ATSP2";
        public const string ATSP3 = "ATSP3";
        public const string ATSP4 = "ATSP4";
        public const string ATSP5 = "ATSP5";
        public const string ATSP6 = "ATSP6";
        public const string ATSP7 = "ATSP7";
        public const string ATSP8 = "ATSP8";
        public const string ATSP9 = "ATSP9";
        public const string ATSPA = "ATSPA";
        public const string ATDP = "ATDP";
        public const string ATDPN = "ATDPN";
        public const string ATST = "ATST";
        public const string ATAT0 = "ATAT0";
        public const string ATAT1 = "ATAT1";
        public const string ATAT2 = "ATAT2";
        public const string ATAL = "ATAL";
        public const string ATMA = "ATMA";
        public const string ATPC = "ATPC";
        public const string ATV = "AT@1";
        public const string ATI = "ATI";
        public const string ATRV = "ATRV";
        public const string ATCAF0 = "ATCAF0";
        public const string ATCAF1 = "ATCAF1";

        // OBD2 Modes
        public const byte MODE_CURRENT_DATA = 0x01;
        public const byte MODE_FREEZE_FRAME = 0x02;
        public const byte MODE_DTC = 0x03;
        public const byte MODE_CLEAR_DTC = 0x04;
        public const byte MODE_TEST_RESULTS = 0x05;
        public const byte MODE_TEST_RESULTS_CAN = 0x06;
        public const byte MODE_PENDING_DTC = 0x07;
        public const byte MODE_CONTROL_OPERATION = 0x08;
        public const byte MODE_VEHICLE_INFO = 0x09;
        public const byte MODE_PERMANENT_DTC = 0x0A;

        // Common PIDs (Mode 01)
        public const byte PID_SUPPORTED_01_20 = 0x00;
        public const byte PID_MONITOR_STATUS = 0x01;
        public const byte PID_FREEZE_DTC = 0x02;
        public const byte PID_FUEL_SYSTEM_STATUS = 0x03;
        public const byte PID_CALC_ENGINE_LOAD = 0x04;
        public const byte PID_COOLANT_TEMP = 0x05;
        public const byte PID_SHORT_TERM_FUEL_TRIM_1 = 0x06;
        public const byte PID_LONG_TERM_FUEL_TRIM_1 = 0x07;
        public const byte PID_SHORT_TERM_FUEL_TRIM_2 = 0x08;
        public const byte PID_LONG_TERM_FUEL_TRIM_2 = 0x09;
        public const byte PID_FUEL_PRESSURE = 0x0A;
        public const byte PID_INTAKE_MAP = 0x0B;
        public const byte PID_ENGINE_RPM = 0x0C;
        public const byte PID_VEHICLE_SPEED = 0x0D;
        public const byte PID_TIMING_ADVANCE = 0x0E;
        public const byte PID_INTAKE_AIR_TEMP = 0x0F;
        public const byte PID_MAF_FLOW = 0x10;
        public const byte PID_THROTTLE_POS = 0x11;
        public const byte PID_COMMANDED_SECONDARY_AIR = 0x12;
        public const byte PID_O2_SENSORS_PRESENT = 0x13;
        public const byte PID_O2_SENSOR_1_VOLTAGE = 0x14;
        public const byte PID_O2_SENSOR_2_VOLTAGE = 0x15;
        public const byte PID_O2_SENSOR_3_VOLTAGE = 0x16;
        public const byte PID_O2_SENSOR_4_VOLTAGE = 0x17;
        public const byte PID_O2_SENSOR_5_VOLTAGE = 0x18;
        public const byte PID_O2_SENSOR_6_VOLTAGE = 0x19;
        public const byte PID_O2_SENSOR_7_VOLTAGE = 0x1A;
        public const byte PID_O2_SENSOR_8_VOLTAGE = 0x1B;
        public const byte PID_OBD_STANDARD = 0x1C;
        public const byte PID_O2_SENSORS_PRESENT_4BANK = 0x1D;
        public const byte PID_AUX_INPUT_STATUS = 0x1E;
        public const byte PID_RUNTIME_SINCE_START = 0x1F;
        public const byte PID_SUPPORTED_21_40 = 0x20;
        public const byte PID_DISTANCE_WITH_MIL = 0x21;
        public const byte PID_FUEL_RAIL_PRESSURE = 0x22;
        public const byte PID_FUEL_RAIL_GAUGE_PRESSURE = 0x23;
        public const byte PID_O2_SENSOR_1_EQUIV_RATIO = 0x24;
        public const byte PID_O2_SENSOR_2_EQUIV_RATIO = 0x25;
        public const byte PID_EGR_COMMANDED = 0x2C;
        public const byte PID_EGR_ERROR = 0x2D;
        public const byte PID_EVAP_PURGE = 0x2E;
        public const byte PID_FUEL_LEVEL = 0x2F;
        public const byte PID_WARMUPS_SINCE_CLR = 0x30;
        public const byte PID_DISTANCE_SINCE_CLR = 0x31;
        public const byte PID_EVAP_SYS_PRESSURE = 0x32;
        public const byte PID_BARO_PRESSURE = 0x33;
        public const byte PID_CATALYST_TEMP_B1S1 = 0x3C;
        public const byte PID_CATALYST_TEMP_B2S1 = 0x3D;
        public const byte PID_CATALYST_TEMP_B1S2 = 0x3E;
        public const byte PID_CATALYST_TEMP_B2S2 = 0x3F;
        public const byte PID_SUPPORTED_41_60 = 0x40;
        public const byte PID_MONITOR_STATUS_DRIVE = 0x41;
        public const byte PID_CONTROL_MODULE_VOLTAGE = 0x42;
        public const byte PID_ABS_LOAD_VALUE = 0x43;
        public const byte PID_COMMANDED_AIR_FUEL = 0x44;
        public const byte PID_RELATIVE_THROTTLE = 0x45;
        public const byte PID_AMBIENT_AIR_TEMP = 0x46;
        public const byte PID_ABS_THROTTLE_B = 0x47;
        public const byte PID_ABS_THROTTLE_C = 0x48;
        public const byte PID_ACCEL_PEDAL_D = 0x49;
        public const byte PID_ACCEL_PEDAL_E = 0x4A;
        public const byte PID_ACCEL_PEDAL_F = 0x4B;
        public const byte PID_COMMANDED_THROTTLE = 0x4C;
        public const byte PID_ENGINE_RUN_TIME = 0x4D;
        public const byte PID_ENGINE_RUN_TIME_MIL = 0x4E;
        public const byte PID_SUPPORTED_61_80 = 0x60;
        public const byte PID_MAX_MAF = 0x50;
        public const byte PID_FUEL_TYPE = 0x51;
        public const byte PID_ETHANOL_FUEL = 0x52;
        public const byte PID_EVAP_SYS_VAPOR_PRESSURE = 0x53;
        public const byte PID_SHORT_TERM_SEC_O2_B1 = 0x55;
        public const byte PID_LONG_TERM_SEC_O2_B1 = 0x56;
        public const byte PID_SHORT_TERM_SEC_O2_B2 = 0x57;
        public const byte PID_LONG_TERM_SEC_O2_B2 = 0x58;
        public const byte PID_FUEL_RAIL_ABS_PRESSURE = 0x59;
        public const byte PID_RELATIVE_ACCEL_PEDAL = 0x5A;
        public const byte PID_HYBRID_BATTERY_REMAINING = 0x5B;
        public const byte PID_ENGINE_OIL_TEMP = 0x5C;
        public const byte PID_FUEL_INJECTION_TIMING = 0x5D;
        public const byte PID_ENGINE_FUEL_RATE = 0x5E;
        public const byte PID_ENGINE_TORQUE_DEMANDED = 0x61;
        public const byte PID_ENGINE_TORQUE_ACTUAL = 0x62;
        public const byte PID_ENGINE_REF_TORQUE = 0x63;

        // Mode 09 PIDs (Vehicle Info)
        public const byte INFO_SUPPORTED = 0x00;
        public const byte INFO_MESSAGE_COUNT = 0x01;
        public const byte INFO_VIN = 0x02;
        public const byte INFO_CALIBRATION_ID = 0x04;
        public const byte INFO_CVN = 0x06;
        public const byte INFO_PERF_TRACKING = 0x08;
        public const byte INFO_ECU_NAME = 0x0A;
        public const byte INFO_IN_USE_TRACKING = 0x0B;

        public static readonly Dictionary<byte, (string Name, string Unit, double Min, double Max, string Formula, PidCategory Category)> PidDefinitions = new()
        {
            { PID_CALC_ENGINE_LOAD,    ("Calculated Engine Load",   "%",    0,  100, "A*100/255",                                      PidCategory.Engine) },
            { PID_COOLANT_TEMP,        ("Coolant Temperature",      "°C", -40,  215, "A-40",                                           PidCategory.Temperature) },
            { PID_SHORT_TERM_FUEL_TRIM_1, ("Short Term Fuel Trim B1", "%", -100, 99.2, "(A-128)*100/128",                              PidCategory.Fuel) },
            { PID_LONG_TERM_FUEL_TRIM_1,  ("Long Term Fuel Trim B1",  "%", -100, 99.2, "(A-128)*100/128",                              PidCategory.Fuel) },
            { PID_FUEL_PRESSURE,       ("Fuel Pressure",            "kPa",   0,  765, "A*3",                                           PidCategory.Fuel) },
            { PID_INTAKE_MAP,          ("Intake Manifold Pressure", "kPa",   0,  255, "A",                                             PidCategory.Pressure) },
            { PID_ENGINE_RPM,          ("Engine RPM",               "rpm",   0, 8000, "(256*A+B)/4",                                   PidCategory.Engine) },
            { PID_VEHICLE_SPEED,       ("Vehicle Speed",            "km/h",  0,  255, "A",                                             PidCategory.Speed) },
            { PID_TIMING_ADVANCE,      ("Timing Advance",           "°",   -64, 63.5, "A/2-64",                                        PidCategory.Engine) },
            { PID_INTAKE_AIR_TEMP,     ("Intake Air Temperature",   "°C",  -40,  215, "A-40",                                          PidCategory.Temperature) },
            { PID_MAF_FLOW,            ("MAF Air Flow Rate",        "g/s",   0,  655.35, "(256*A+B)/100",                              PidCategory.Engine) },
            { PID_THROTTLE_POS,        ("Throttle Position",        "%",     0,  100, "A*100/255",                                     PidCategory.Engine) },
            { PID_RUNTIME_SINCE_START, ("Run Time Since Engine Start", "s",  0, 65535, "256*A+B",                                      PidCategory.Engine) },
            { PID_DISTANCE_WITH_MIL,   ("Distance with MIL On",    "km",    0, 65535, "256*A+B",                                       PidCategory.Emissions) },
            { PID_FUEL_RAIL_PRESSURE,  ("Fuel Rail Pressure",       "kPa",   0, 5177.265, "(256*A+B)*0.079",                           PidCategory.Fuel) },
            { PID_EGR_COMMANDED,       ("Commanded EGR",            "%",     0,  100, "A*100/255",                                     PidCategory.Emissions) },
            { PID_FUEL_LEVEL,          ("Fuel Level Input",         "%",     0,  100, "A*100/255",                                     PidCategory.Fuel) },
            { PID_WARMUPS_SINCE_CLR,   ("Warm-Ups Since Codes Cleared", "",  0,  255, "A",                                             PidCategory.Engine) },
            { PID_DISTANCE_SINCE_CLR,  ("Distance Since Codes Cleared", "km", 0, 65535, "256*A+B",                                     PidCategory.Engine) },
            { PID_BARO_PRESSURE,       ("Barometric Pressure",      "kPa",   0,  255, "A",                                             PidCategory.Pressure) },
            { PID_CONTROL_MODULE_VOLTAGE, ("Control Module Voltage", "V",    0,  65.535, "(256*A+B)/1000",                             PidCategory.Electrical) },
            { PID_ABS_LOAD_VALUE,      ("Absolute Load Value",      "%",     0, 25700, "(256*A+B)*100/255",                            PidCategory.Engine) },
            { PID_RELATIVE_THROTTLE,   ("Relative Throttle Position", "%",   0,  100, "A*100/255",                                     PidCategory.Engine) },
            { PID_AMBIENT_AIR_TEMP,    ("Ambient Air Temperature",  "°C",  -40,  215, "A-40",                                          PidCategory.Temperature) },
            { PID_ETHANOL_FUEL,        ("Ethanol Fuel Percentage",  "%",     0,  100, "A*100/255",                                     PidCategory.Fuel) },
            { PID_ENGINE_OIL_TEMP,     ("Engine Oil Temperature",   "°C",  -40,  210, "A-40",                                          PidCategory.Temperature) },
            { PID_ENGINE_FUEL_RATE,    ("Engine Fuel Rate",         "L/h",   0, 3212.75, "(256*A+B)*0.05",                             PidCategory.Fuel) },
            { PID_HYBRID_BATTERY_REMAINING, ("Hybrid Battery Remaining", "%", 0, 100, "A*100/255",                                     PidCategory.Electrical) },
        };

        public static string BuildCommand(byte mode, byte pid) => $"{mode:X2}{pid:X2}";

        public static string CleanResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return "";
            return response.Replace("\r", " ").Replace("\n", " ").Replace(">", "").Trim().ToUpper();
        }

        public static byte[]? ParseDataBytes(string response, byte mode, byte pid)
        {
            try
            {
                var clean = CleanResponse(response);
                var parts = clean.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var responseMode = (byte)(mode + 0x40);
                int dataStart = -1;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out byte b)
                        && b == responseMode
                        && byte.TryParse(parts[i + 1], System.Globalization.NumberStyles.HexNumber, null, out byte p)
                        && p == pid)
                    {
                        dataStart = i + 2;
                        break;
                    }
                }
                if (dataStart < 0) return null;
                var result = new List<byte>();
                for (int i = dataStart; i < parts.Length; i++)
                {
                    if (byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out byte b))
                        result.Add(b);
                }
                return result.ToArray();
            }
            catch { return null; }
        }

        public static bool IsError(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return true;
            var upper = response.ToUpperInvariant();
            return upper.Contains("NO DATA") || upper.Contains("ERROR") || upper.Contains("UNABLE")
                || upper.Contains("BUS BUSY") || upper.Contains("FB ERROR") || upper.Contains("DATA ERROR");
        }

        public static List<string> ParseDtcResponse(string response)
        {
            var dtcs = new List<string>();
            try
            {
                var clean = CleanResponse(response);
                var lines = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                while (i < lines.Length)
                {
                    if (lines[i].Length == 2 &&
                        (lines[i] == "43" || lines[i] == "47" || lines[i] == "4A"))
                    {
                        i++;
                        continue;
                    }
                    if (i + 1 < lines.Length &&
                        byte.TryParse(lines[i], System.Globalization.NumberStyles.HexNumber, null, out byte high) &&
                        byte.TryParse(lines[i + 1], System.Globalization.NumberStyles.HexNumber, null, out byte low))
                    {
                        if (high != 0 || low != 0)
                        {
                            string dtc = DecodeDtcBytes(high, low);
                            if (!string.IsNullOrEmpty(dtc)) dtcs.Add(dtc);
                        }
                        i += 2;
                    }
                    else i++;
                }
            }
            catch { }
            return dtcs;
        }

        public static string DecodeDtcBytes(byte high, byte low)
        {
            string[] prefixes = { "P", "C", "B", "U" };
            int prefix = (high >> 6) & 0x03;
            int digit1 = (high >> 4) & 0x03;
            int digit2 = high & 0x0F;
            int digit3 = (low >> 4) & 0x0F;
            int digit4 = low & 0x0F;
            return $"{prefixes[prefix]}{digit1}{digit2:X}{digit3:X}{digit4:X}";
        }
    }

    public enum PidCategory
    {
        Engine, Transmission, Fuel, Emissions, Temperature, Pressure, Speed, Electrical, Sensor, Other
    }
}
