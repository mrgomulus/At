using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class DataLoggerViewModel : BaseViewModel
    {
        private readonly DataLoggerService _logSvc;
        private readonly IObdService _obdService;
        private DataLogSession? _currentSession;
        private bool _isRecording;
        private int _intervalMs = 1000;
        private int _sampleCount;
        private double _elapsedSeconds;
        private string _filePath = "";

        public ObservableCollection<string> LiveEntries { get; } = new();
        public ObservableCollection<DataLogSession> SavedSessions { get; } = new();

        public bool IsRecording { get => _isRecording; set { _isRecording = value; OnPropertyChanged(); } }
        public int IntervalMs
        {
            get => _intervalMs;
            set { _intervalMs = Math.Max(200, value); OnPropertyChanged(); }
        }
        public int SampleCount      { get => _sampleCount;     set { _sampleCount = value;     OnPropertyChanged(); } }
        public double ElapsedSeconds{ get => _elapsedSeconds;   set { _elapsedSeconds = value;  OnPropertyChanged(); } }
        public string FilePath      { get => _filePath;         set { _filePath = value;         OnPropertyChanged(); } }

        public ICommand StartCommand  { get; }
        public ICommand StopCommand   { get; }
        public ICommand ExportCommand { get; }
        public ICommand ClearCommand  { get; }

        public DataLoggerViewModel(IObdService obdService)
        {
            _obdService = obdService;
            _logSvc = new DataLoggerService(obdService);
            _logSvc.EntryRecorded += OnEntryRecorded;

            StartCommand  = new RelayCommand(_ => StartLogging(), _ => !IsRecording);
            StopCommand   = new RelayCommand(_ => StopLogging(),  _ => IsRecording);
            ExportCommand = new RelayCommand(_ => ExportLast(),   _ => SavedSessions.Count > 0);
            ClearCommand  = new RelayCommand(_ => { LiveEntries.Clear(); SampleCount = 0; });
        }

        private void StartLogging()
        {
            LiveEntries.Clear();
            SampleCount = 0;
            ElapsedSeconds = 0;
            _currentSession = _logSvc.StartNewSession(IntervalMs, new[]
            {
                "Engine RPM", "Vehicle Speed", "Coolant Temp", "Engine Load",
                "Throttle Position", "MAF Sensor", "Short Fuel Trim B1", "Long Fuel Trim B1",
                "Intake Air Temp", "Fuel Rail Pressure", "Battery Voltage", "Barometric Pressure"
            });
            IsRecording = true;
            StatusMessage = $"Recording — interval {IntervalMs} ms";
        }

        private void StopLogging()
        {
            var session = _logSvc.StopSession();
            IsRecording = false;
            if (session != null)
            {
                SavedSessions.Insert(0, session);
                StatusMessage = $"Stopped. {session.Entries.Count} samples in {session.DurationText}";
            }
        }

        private void OnEntryRecorded(object? sender, DataLogEntry e)
        {
            // Update on UI thread (this runs on timer thread)
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                SampleCount++;
                ElapsedSeconds = e.ElapsedSeconds;
                // Build summary line for the live log
                var mainVals = e.Values.Take(4).Select(v => $"{v.Name}: {v.Value:F1} {v.Unit}");
                LiveEntries.Insert(0, $"[{e.Timestamp:HH:mm:ss.ff}] {string.Join("  |  ", mainVals)}");
                if (LiveEntries.Count > 500) LiveEntries.RemoveAt(LiveEntries.Count - 1);
            });
        }

        private void ExportLast()
        {
            if (SavedSessions.Count == 0) return;
            var session = SavedSessions[0];
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = session.SessionName,
                    DefaultExt = ".csv",
                    Filter = "CSV files|*.csv|All files|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    File.WriteAllText(dlg.FileName, session.ToCsv());
                    FilePath = dlg.FileName;
                    StatusMessage = $"Exported to {dlg.FileName}";
                }
            }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }
}
