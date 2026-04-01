using System;
using System.Collections.Generic;

namespace OBD2Suite.Models
{
    public enum DtcSeverity { Info, Minor, Moderate, Severe, Critical }
    public enum DtcStatus { Pending, Confirmed, Permanent, Historical }
    public enum DtcCategory { Powertrain, Body, Chassis, Network }

    public class FreezeFrameData
    {
        public double EngineRpm { get; set; }
        public double VehicleSpeed { get; set; }
        public double CoolantTemp { get; set; }
        public double ThrottlePosition { get; set; }
        public double FuelTrim { get; set; }
        public double LoadValue { get; set; }
        public double IntakeTemp { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DiagnosticTroubleCode
    {
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public string EcuSource { get; set; } = "Engine";
        public DtcSeverity Severity { get; set; } = DtcSeverity.Moderate;
        public DtcStatus Status { get; set; } = DtcStatus.Confirmed;
        public DtcCategory Category { get; set; } = DtcCategory.Powertrain;
        public bool IsMilOn { get; set; }
        public byte StatusByte { get; set; }
        public FreezeFrameData? FreezeFrame { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.Now;
        public int OccurrenceCount { get; set; } = 1;

        public string SeverityText => Severity.ToString();
        public string StatusText => Status.ToString();
        public string CategoryCode => Code.Length > 0 ? Code[0].ToString() : "P";

        public static DtcCategory GetCategory(string code)
        {
            if (string.IsNullOrEmpty(code)) return DtcCategory.Powertrain;
            return code[0] switch
            {
                'P' => DtcCategory.Powertrain,
                'B' => DtcCategory.Body,
                'C' => DtcCategory.Chassis,
                'U' => DtcCategory.Network,
                _ => DtcCategory.Powertrain
            };
        }

        public static DtcSeverity GetSeverity(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 5) return DtcSeverity.Moderate;
            int num = int.TryParse(code.Substring(1), out int n) ? n : 0;
            if (num < 100) return DtcSeverity.Critical;
            if (num < 500) return DtcSeverity.Severe;
            if (num < 1000) return DtcSeverity.Moderate;
            if (num < 2000) return DtcSeverity.Minor;
            return DtcSeverity.Info;
        }
    }
}
