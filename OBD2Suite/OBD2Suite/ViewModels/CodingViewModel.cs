using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class CodingViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private EcuModule? _selectedEcu;
        private string _currentCoding = "";
        private string _newCoding = "";
        private string _adaptationChannel = "0";
        private string _adaptationCurrentValue = "";
        private string _adaptationNewValue = "";
        private string _statusMessage = "Select an ECU module to begin coding";
        private bool _isBusy;
        private string _logText = "";

        public ObservableCollection<EcuModule> EcuModules { get; } = new();
        public ObservableCollection<AdaptationChannel> AdaptationChannels { get; } = new();

        public EcuModule? SelectedEcu
        {
            get => _selectedEcu;
            set { SetProperty(ref _selectedEcu, value); OnEcuSelected(); }
        }

        public string CurrentCoding
        {
            get => _currentCoding;
            set => SetProperty(ref _currentCoding, value);
        }

        public string NewCoding
        {
            get => _newCoding;
            set => SetProperty(ref _newCoding, value);
        }

        public string AdaptationChannel
        {
            get => _adaptationChannel;
            set => SetProperty(ref _adaptationChannel, value);
        }

        public string AdaptationCurrentValue
        {
            get => _adaptationCurrentValue;
            set => SetProperty(ref _adaptationCurrentValue, value);
        }

        public string AdaptationNewValue
        {
            get => _adaptationNewValue;
            set => SetProperty(ref _adaptationNewValue, value);
        }

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

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public ICommand LoadEcusCommand { get; }
        public ICommand ReadCodingCommand { get; }
        public ICommand WriteCodingCommand { get; }
        public ICommand ReadAdaptationCommand { get; }
        public ICommand WriteAdaptationCommand { get; }

        public CodingViewModel(IObdService obdService)
        {
            _obdService = obdService;
            LoadEcusCommand        = new RelayCommand(async _ => await LoadEcusAsync(),          _ => !IsBusy);
            ReadCodingCommand      = new RelayCommand(async _ => await ReadCodingAsync(),         _ => !IsBusy && SelectedEcu != null);
            WriteCodingCommand     = new RelayCommand(async _ => await WriteCodingAsync(),        _ => !IsBusy && SelectedEcu != null && !string.IsNullOrWhiteSpace(NewCoding));
            ReadAdaptationCommand  = new RelayCommand(async _ => await ReadAdaptationAsync(),     _ => !IsBusy && SelectedEcu != null);
            WriteAdaptationCommand = new RelayCommand(async _ => await WriteAdaptationAsync(),    _ => !IsBusy && SelectedEcu != null && !string.IsNullOrWhiteSpace(AdaptationNewValue));
        }

        private async Task LoadEcusAsync()
        {
            IsBusy = true;
            StatusMessage = "Scanning ECUs…";
            try
            {
                var ecus = await _obdService.ReadEcuModulesAsync();
                EcuModules.Clear();
                foreach (var e in ecus) EcuModules.Add(e);
                StatusMessage = $"Found {ecus.Count} ECU(s). Select one to read its coding.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void OnEcuSelected()
        {
            CurrentCoding = "";
            NewCoding = "";
            AdaptationCurrentValue = "";
            AdaptationChannels.Clear();
            if (SelectedEcu != null)
            {
                StatusMessage = $"ECU selected: {SelectedEcu.Name} (0x{SelectedEcu.Address:X2}) — click 'Read Coding'";
                foreach (var ch in SelectedEcu.AdaptationChannels)
                    AdaptationChannels.Add(ch);
            }
        }

        private async Task ReadCodingAsync()
        {
            if (SelectedEcu == null) return;
            IsBusy = true;
            StatusMessage = $"Reading coding from {SelectedEcu.Name}…";
            try
            {
                var coding = await _obdService.ReadEcuCodingAsync(SelectedEcu.Address, 0xF186);
                CurrentCoding = coding;
                NewCoding = coding;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] READ coding {SelectedEcu.Name}: {coding}");
                StatusMessage = "Coding read successfully.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task WriteCodingAsync()
        {
            if (SelectedEcu == null) return;
            var confirm = System.Windows.MessageBox.Show(
                $"Write coding '{NewCoding}' to {SelectedEcu.Name}?\n\nThis will modify ECU configuration. Proceed?",
                "Confirm Coding Write", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = $"Writing coding to {SelectedEcu.Name}…";
            try
            {
                var bytes = NewCoding.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(b => Convert.ToByte(b.Trim(), 16))
                                     .ToArray();
                var ok = await _obdService.WriteEcuCodingAsync(SelectedEcu.Address, 0xF186, bytes);
                CurrentCoding = NewCoding;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] WRITE coding {SelectedEcu.Name}: {NewCoding} — {(ok ? "OK" : "FAILED")}");
                StatusMessage = ok ? "Coding written successfully." : "Write failed.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ReadAdaptationAsync()
        {
            if (SelectedEcu == null) return;
            if (!int.TryParse(AdaptationChannel, out int ch)) { StatusMessage = "Invalid channel number."; return; }
            IsBusy = true;
            StatusMessage = $"Reading adaptation channel {ch}…";
            try
            {
                var val = await _obdService.ReadAdaptationAsync(SelectedEcu.Address, ch);
                AdaptationCurrentValue = val;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] READ adaptation ch{ch}: {val}");
                StatusMessage = $"Channel {ch} value: {val}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task WriteAdaptationAsync()
        {
            if (SelectedEcu == null) return;
            if (!int.TryParse(AdaptationChannel, out int ch)) { StatusMessage = "Invalid channel number."; return; }
            var confirm = System.Windows.MessageBox.Show(
                $"Write adaptation value '{AdaptationNewValue}' to channel {ch} on {SelectedEcu.Name}?",
                "Confirm Adaptation Write", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = $"Writing adaptation channel {ch}…";
            try
            {
                var ok = await _obdService.WriteAdaptationAsync(SelectedEcu.Address, ch, AdaptationNewValue);
                if (ok) AdaptationCurrentValue = AdaptationNewValue;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] WRITE adaptation ch{ch}: {AdaptationNewValue} — {(ok ? "OK" : "FAILED")}");
                StatusMessage = ok ? "Adaptation written successfully." : "Write failed.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void AppendLog(string line)
            => LogText = LogText + line + "\n";
    }
}
