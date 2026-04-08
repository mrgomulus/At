namespace OBD2Suite.Models
{
    public enum MonitorStatus { NotAvailable, Incomplete, Complete }

    /// <summary>
    /// OBD2 I/M Readiness Monitor — one of the 11 standard emission monitors.
    /// Read from Mode 01 PID 01 and PID 41.
    /// </summary>
    public class ReadinessMonitor
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public MonitorStatus Status { get; set; } = MonitorStatus.NotAvailable;
        public bool IsContinuous { get; set; }   // Continuous monitors always run
        public bool IsSupported { get; set; } = true;

        public string StatusText => Status switch
        {
            MonitorStatus.Complete    => "✔ Complete",
            MonitorStatus.Incomplete  => "✖ Incomplete",
            _                         => "— N/A"
        };

        public string StatusColor => Status switch
        {
            MonitorStatus.Complete    => "#4CAF50",
            MonitorStatus.Incomplete  => "#FF5252",
            _                         => "#888888"
        };
    }

    /// <summary>
    /// Full I/M Readiness report — all 11 monitors + MIL status.
    /// </summary>
    public class ReadinessReport
    {
        public bool MilOn { get; set; }
        public int DtcCount { get; set; }
        public List<ReadinessMonitor> Monitors { get; set; } = new();
        public DateTime ReadAt { get; set; } = DateTime.Now;
        public bool IsReadyForTest => !MilOn && Monitors.Where(m => m.IsSupported && !m.IsContinuous)
                                                        .All(m => m.Status == MonitorStatus.Complete);

        public string OverallStatus => IsReadyForTest ? "READY — Pass emissions test" : "NOT READY — Complete drive cycle";
    }
}
