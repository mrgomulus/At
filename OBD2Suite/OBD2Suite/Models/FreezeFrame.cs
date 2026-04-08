namespace OBD2Suite.Models
{
    /// <summary>
    /// Snapshot of sensor data captured by the ECU when a DTC was set (OBD2 Mode 02).
    /// </summary>
    public class FreezeFrame
    {
        public string DtcCode { get; set; } = "";
        public int FrameNumber { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.Now;
        public List<FreezeFrameParameter> Parameters { get; set; } = new();
        public string Title => $"Freeze Frame #{FrameNumber} — {DtcCode}";
    }

    public class FreezeFrameParameter
    {
        public string Name { get; set; } = "";
        public string PidHex { get; set; } = "";
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public string DisplayText => string.IsNullOrEmpty(Unit) ? $"{Value:F2}" : $"{Value:F2} {Unit}";
    }
}
