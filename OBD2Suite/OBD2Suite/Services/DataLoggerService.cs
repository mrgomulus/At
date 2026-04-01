using System.IO;
using System.Timers;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>Live data logging service — records PIDs at a fixed interval and exports to CSV.</summary>
    public class DataLoggerService
    {
        private readonly IObdService _obdService;
        private System.Timers.Timer? _timer;
        private DataLogSession? _session;
        private DateTime _sessionStart;
        public event EventHandler<DataLogEntry>? EntryRecorded;
        public bool IsRecording => _session?.IsRecording == true;

        public DataLoggerService(IObdService obdService) => _obdService = obdService;

        public DataLogSession StartNewSession(int intervalMs, IEnumerable<string> parameterNames)
        {
            StopSession();
            _session = new DataLogSession
            {
                SessionName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}",
                StartedAt = DateTime.Now,
                IntervalMs = intervalMs,
                ParameterNames = parameterNames.ToList(),
                IsRecording = true
            };
            _sessionStart = DateTime.Now;

            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += async (_, _) => await RecordEntryAsync();
            _timer.Start();
            return _session;
        }

        private async Task RecordEntryAsync()
        {
            if (_session == null || !_session.IsRecording) return;
            try
            {
                var liveData = await _obdService.ReadLiveDataAsync();
                var entry = new DataLogEntry
                {
                    Timestamp = DateTime.Now,
                    ElapsedSeconds = (DateTime.Now - _sessionStart).TotalSeconds,
                    Values = liveData.Select(p => (p.Name, p.Value, p.Unit)).ToList()
                };
                _session.Entries.Add(entry);
                EntryRecorded?.Invoke(this, entry);
            }
            catch { /* continue logging */ }
        }

        public DataLogSession? StopSession()
        {
            if (_session == null) return null;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _session.IsRecording = false;
            _session.EndedAt = DateTime.Now;
            var finished = _session;
            _session = null;
            return finished;
        }

        /// <summary>Save a session's CSV to a file path.</summary>
        public static async Task SaveToCsvAsync(DataLogSession session, string filePath)
        {
            await File.WriteAllTextAsync(filePath, session.ToCsv());
        }

        /// <summary>Quick export — write current session CSV to temp folder and return path.</summary>
        public static string ExportToTempCsv(DataLogSession session)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{session.SessionName}.csv");
            File.WriteAllText(path, session.ToCsv());
            return path;
        }
    }
}
