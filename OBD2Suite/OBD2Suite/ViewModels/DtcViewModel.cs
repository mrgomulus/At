using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class DtcViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private string _statusMessage = "Ready — press 'Read DTCs' to scan";
        private bool _isBusy;
        private DiagnosticTroubleCode? _selectedDtc;

        public ObservableCollection<DiagnosticTroubleCode> Codes { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public DiagnosticTroubleCode? SelectedDtc
        {
            get => _selectedDtc;
            set => SetProperty(ref _selectedDtc, value);
        }

        public int DtcCount => Codes.Count;
        public int ActiveCount => Codes.Count(c => c.IsActive);

        public ICommand ReadDtcsCommand { get; }
        public ICommand ClearDtcsCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public DtcViewModel(IObdService obdService)
        {
            _obdService = obdService;
            ReadDtcsCommand  = new RelayCommand(async _ => await ReadDtcsAsync(),  _ => !IsBusy);
            ClearDtcsCommand = new RelayCommand(async _ => await ClearDtcsAsync(), _ => !IsBusy && Codes.Count > 0);
            ExportCsvCommand = new RelayCommand(_ => ExportToCsv(), _ => Codes.Count > 0);
        }

        private async Task ReadDtcsAsync()
        {
            IsBusy = true;
            StatusMessage = "Reading fault codes…";
            try
            {
                var codes = await _obdService.ReadDtcsAsync();
                Codes.Clear();
                foreach (var c in codes)
                    Codes.Add(c);
                OnPropertyChanged(nameof(DtcCount));
                OnPropertyChanged(nameof(ActiveCount));
                StatusMessage = codes.Count == 0
                    ? "No fault codes found."
                    : $"Found {codes.Count} fault code(s) — {ActiveCount} active";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private async Task ClearDtcsAsync()
        {
            IsBusy = true;
            StatusMessage = "Clearing fault codes…";
            try
            {
                var ok = await _obdService.ClearDtcsAsync();
                if (ok)
                {
                    Codes.Clear();
                    OnPropertyChanged(nameof(DtcCount));
                    OnPropertyChanged(nameof(ActiveCount));
                    StatusMessage = "All fault codes cleared successfully.";
                }
                else
                    StatusMessage = "Clear command failed.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void ExportToCsv()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"DTC_Export_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".csv",
                Filter = "CSV files|*.csv"
            };
            if (dlg.ShowDialog() != true) return;
            var lines = new List<string> { "Code,Description,ECU,Status,Severity,Timestamp" };
            lines.AddRange(Codes.Select(c =>
                $"{c.Code},{c.Description.Replace(",", ";")}," +
                $"{c.EcuSource},{(c.IsActive ? "Active" : "Stored")}," +
                $"{c.Severity},{c.Timestamp:yyyy-MM-dd HH:mm:ss}"));
            System.IO.File.WriteAllLines(dlg.FileName, lines, System.Text.Encoding.UTF8);
            StatusMessage = $"Exported {Codes.Count} codes to {dlg.FileName}";
        }
    }
}
