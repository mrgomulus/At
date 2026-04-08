using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class OemFeaturesViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private IManufacturerService? _oemService;

        private Manufacturer _selectedManufacturer = Manufacturer.Generic;
        private EcuModule? _selectedEcu;
        private MeasuringBlock? _selectedBlock;
        private ActuatorTest? _selectedTest;
        private int _selectedMeasuringGroup = 1;
        private string _statusMessage = "Select a manufacturer and click 'Scan ECUs'";
        private bool _isBusy;
        private string _outputLog = "";
        private string _ecuIdentInfo = "";

        public List<(string Group, Manufacturer[] Members)> ManufacturerGroups { get; }
            = ManufacturerServiceFactory.GetManufacturerGroups().ToList();

        public ObservableCollection<Manufacturer> AllManufacturers { get; } = new();
        public ObservableCollection<EcuModule> EcuModules { get; } = new();
        public ObservableCollection<MeasuringBlock> MeasuringBlocks { get; } = new();
        public ObservableCollection<ActuatorTest> ActuatorTests { get; } = new();
        public ObservableCollection<int> AvailableGroups { get; } = new();
        public ObservableCollection<string> OemDtcList { get; } = new();

        public Manufacturer SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                SetProperty(ref _selectedManufacturer, value);
                _oemService = ManufacturerServiceFactory.Create(value, _obdService);
                StatusMessage = $"Manufacturer: {value} ({ManufacturerHelper.GetGroupName(value)}) — Protocol: {ManufacturerHelper.GetDefaultProtocol(value)}";
            }
        }

        public EcuModule? SelectedEcu
        {
            get => _selectedEcu;
            set { SetProperty(ref _selectedEcu, value); OnEcuChanged(); }
        }

        public MeasuringBlock? SelectedBlock
        {
            get => _selectedBlock;
            set => SetProperty(ref _selectedBlock, value);
        }

        public ActuatorTest? SelectedTest
        {
            get => _selectedTest;
            set => SetProperty(ref _selectedTest, value);
        }

        public int SelectedMeasuringGroup
        {
            get => _selectedMeasuringGroup;
            set => SetProperty(ref _selectedMeasuringGroup, value);
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

        public string OutputLog
        {
            get => _outputLog;
            set => SetProperty(ref _outputLog, value);
        }

        public string EcuIdentInfo
        {
            get => _ecuIdentInfo;
            set => SetProperty(ref _ecuIdentInfo, value);
        }

        public ICommand ScanEcusCommand { get; }
        public ICommand ReadOemDtcsCommand { get; }
        public ICommand ClearOemDtcsCommand { get; }
        public ICommand ReadMeasuringBlockCommand { get; }
        public ICommand LoadActuatorTestsCommand { get; }
        public ICommand RunActuatorTestCommand { get; }
        public ICommand ReadCodingCommand { get; }
        public ICommand ReadIdentificationCommand { get; }
        public ICommand ResetServiceIntervalCommand { get; }
        public ICommand ThrottleBasicSettingCommand { get; }
        public ICommand SteeringCalibrationCommand { get; }

        public OemFeaturesViewModel(IObdService obdService)
        {
            _obdService = obdService;
            _oemService = ManufacturerServiceFactory.Create(_selectedManufacturer, obdService);

            // Populate manufacturer list
            foreach (var (_, members) in ManufacturerGroups)
                foreach (var m in members)
                    AllManufacturers.Add(m);

            ScanEcusCommand           = new RelayCommand(async _ => await ScanEcusAsync(),             _ => !IsBusy);
            ReadOemDtcsCommand        = new RelayCommand(async _ => await ReadOemDtcsAsync(),           _ => !IsBusy && SelectedEcu != null);
            ClearOemDtcsCommand       = new RelayCommand(async _ => await ClearOemDtcsAsync(),          _ => !IsBusy && SelectedEcu != null);
            ReadMeasuringBlockCommand = new RelayCommand(async _ => await ReadMeasuringBlockAsync(),    _ => !IsBusy && SelectedEcu != null);
            LoadActuatorTestsCommand  = new RelayCommand(async _ => await LoadActuatorTestsAsync(),     _ => !IsBusy && SelectedEcu != null);
            RunActuatorTestCommand    = new RelayCommand(async _ => await RunActuatorTestAsync(),       _ => !IsBusy && SelectedTest != null);
            ReadCodingCommand         = new RelayCommand(async _ => await ReadCodingAsync(),            _ => !IsBusy && SelectedEcu != null);
            ReadIdentificationCommand = new RelayCommand(async _ => await ReadIdentificationAsync(),    _ => !IsBusy && SelectedEcu != null);
            ResetServiceIntervalCommand    = new RelayCommand(async _ => await ResetServiceAsync(),     _ => !IsBusy && SelectedEcu != null);
            ThrottleBasicSettingCommand    = new RelayCommand(async _ => await ThrottleBasicAsync(),    _ => !IsBusy && SelectedEcu != null);
            SteeringCalibrationCommand     = new RelayCommand(async _ => await SteeringCalibAsync(),    _ => !IsBusy);
        }

        private void OnEcuChanged()
        {
            MeasuringBlocks.Clear();
            ActuatorTests.Clear();
            OemDtcList.Clear();
            EcuIdentInfo = "";
        }

        private async Task ScanEcusAsync()
        {
            if (_oemService == null) return;
            IsBusy = true; StatusMessage = $"Scanning all ECUs for {_oemService.DisplayName}…";
            try
            {
                var ecus = await _oemService.ScanAllEcusAsync();
                EcuModules.Clear();
                foreach (var e in ecus) EcuModules.Add(e);
                var groups = await _oemService.GetAvailableMeasuringGroupsAsync(0x01);
                AvailableGroups.Clear();
                foreach (var g in groups) AvailableGroups.Add(g);
                StatusMessage = $"Found {ecus.Count} ECU(s)";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Scan complete — {ecus.Count} ECUs found for {_oemService.DisplayName}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ReadOemDtcsAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Reading OEM fault codes from {SelectedEcu.Name}…";
            try
            {
                var dtcs = await _oemService.ReadAllFaultCodesAsync(SelectedEcu.Address);
                OemDtcList.Clear();
                foreach (var d in dtcs)
                    OemDtcList.Add($"{d.Code} [{d.OemCode}] — {d.OemDescription} ({d.StatusText})");
                StatusMessage = $"Found {dtcs.Count} fault code(s) in {SelectedEcu.Name}";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {SelectedEcu.Name}: {dtcs.Count} OEM DTCs");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ClearOemDtcsAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Clearing fault codes in {SelectedEcu.Name}…";
            try
            {
                var ok = await _oemService.ClearFaultCodesAsync(SelectedEcu.Address);
                if (ok) OemDtcList.Clear();
                StatusMessage = ok ? "Fault codes cleared." : "Clear failed.";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Clear DTCs {SelectedEcu.Name}: {(ok ? "OK" : "FAILED")}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ReadMeasuringBlockAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Reading measuring block {SelectedMeasuringGroup}…";
            try
            {
                var blocks = await _oemService.ReadMeasuringBlockAsync(SelectedEcu.Address, SelectedMeasuringGroup);
                MeasuringBlocks.Clear();
                foreach (var b in blocks) MeasuringBlocks.Add(b);
                StatusMessage = $"Block {SelectedMeasuringGroup} read — {blocks.Sum(b => b.Fields.Count)} values";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Measuring block {SelectedMeasuringGroup}: {string.Join(", ", blocks.SelectMany(b => b.Fields).Select(f => $"{f.Label}={f.DisplayText}"))}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task LoadActuatorTestsAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Loading actuator tests for {SelectedEcu.Name}…";
            try
            {
                var tests = await _oemService.GetActuatorTestsAsync(SelectedEcu.Address);
                ActuatorTests.Clear();
                foreach (var t in tests) ActuatorTests.Add(t);
                StatusMessage = $"Loaded {tests.Count} actuator test(s)";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task RunActuatorTestAsync()
        {
            if (_oemService == null || SelectedEcu == null || SelectedTest == null) return;
            var confirm = System.Windows.MessageBox.Show(
                $"Run actuator test: '{SelectedTest.Name}' on {SelectedEcu.Name}?\n\n{SelectedTest.Description}",
                "Confirm Actuator Test", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true; StatusMessage = $"Running '{SelectedTest.Name}'…";
            SelectedTest.Status = ActuatorTestStatus.Running;
            try
            {
                var result = await _oemService.RunActuatorTestAsync(SelectedEcu.Address, SelectedTest.TestId);
                SelectedTest.Status = ActuatorTestStatus.Completed;
                SelectedTest.LastResult = result;
                StatusMessage = $"Test '{SelectedTest.Name}' completed: {result}";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Actuator test '{SelectedTest.Name}': {result}");
            }
            catch (Exception ex)
            {
                SelectedTest.Status = ActuatorTestStatus.Failed;
                StatusMessage = $"Test failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private async Task ReadCodingAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Reading coding from {SelectedEcu.Name}…";
            try
            {
                var coding = await _oemService.ReadCodingAsync(SelectedEcu.Address);
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Coding {SelectedEcu.Name}: {coding}");
                StatusMessage = $"Coding: {coding}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ReadIdentificationAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = $"Reading identification from {SelectedEcu.Name}…";
            try
            {
                var ident = await _oemService.ReadEcuIdentificationAsync(SelectedEcu.Address);
                EcuIdentInfo = string.Join("\n", ident.Select(kv => $"{kv.Key}: {kv.Value}"));
                StatusMessage = "ECU identification read successfully.";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Ident {SelectedEcu.Name}: OK");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ResetServiceAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            var confirm = System.Windows.MessageBox.Show(
                "Reset service interval? This will clear the maintenance reminder.",
                "Confirm Service Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;
            IsBusy = true; StatusMessage = "Resetting service interval…";
            try
            {
                var ok = await _oemService.ResetServiceIntervalAsync(SelectedEcu.Address);
                StatusMessage = ok ? "Service interval reset successfully." : "Reset failed.";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Service interval reset: {(ok ? "OK" : "FAILED")}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ThrottleBasicAsync()
        {
            if (_oemService == null || SelectedEcu == null) return;
            IsBusy = true; StatusMessage = "Performing throttle basic setting…";
            try
            {
                var ok = await _oemService.PerformThrottleBasicSettingAsync(SelectedEcu.Address);
                StatusMessage = ok ? "Throttle basic setting completed." : "Failed.";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Throttle basic setting: {(ok ? "OK" : "FAILED")}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task SteeringCalibAsync()
        {
            if (_oemService == null) return;
            IsBusy = true; StatusMessage = "Performing steering angle calibration…";
            try
            {
                var ok = await _oemService.PerformSteeringAngleCalibrationAsync(0x44);
                StatusMessage = ok ? "Steering angle calibration completed." : "Failed.";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Steering calibration: {(ok ? "OK" : "FAILED")}");
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void AppendLog(string line)
            => OutputLog = OutputLog + line + "\n";
    }
}
