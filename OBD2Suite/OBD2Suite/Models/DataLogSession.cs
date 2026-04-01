namespace OBD2Suite.Models
{
    /// <summary>
    /// A single recorded row during live data logging.
    /// </summary>
    public class DataLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double ElapsedSeconds { get; set; }
        public List<(string Name, double Value, string Unit)> Values { get; set; } = new();

        public string ToCsvRow(List<string> headers)
        {
            var cells = new List<string>
            {
                Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ElapsedSeconds.ToString("F2")
            };
            foreach (var header in headers)
            {
                var val = Values.FirstOrDefault(v => v.Name == header);
                cells.Add(val.Value.ToString("F3"));
            }
            return string.Join(",", cells);
        }
    }

    /// <summary>
    /// A complete live-data logging session.
    /// </summary>
    public class DataLogSession
    {
        public string SessionName { get; set; } = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}";
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public int IntervalMs { get; set; } = 1000;
        public List<string> ParameterNames { get; set; } = new();
        public List<DataLogEntry> Entries { get; set; } = new();
        public bool IsRecording { get; set; }

        public double DurationSeconds => (EndedAt ?? DateTime.Now).Subtract(StartedAt).TotalSeconds;
        public string DurationText => $"{DurationSeconds:F0} s ({Entries.Count} samples)";

        public string ToCsv()
        {
            var sb = new System.Text.StringBuilder();
            // Header
            var headers = new List<string> { "Timestamp", "Elapsed_s" };
            headers.AddRange(ParameterNames);
            sb.AppendLine(string.Join(",", headers));
            // Data rows
            foreach (var entry in Entries)
                sb.AppendLine(entry.ToCsvRow(ParameterNames));
            return sb.ToString();
        }
    }
}
