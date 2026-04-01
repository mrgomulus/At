using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel(new ObdService());
            DataContext = vm;
            Resources.Add("BoolToVisConverter", new BoolToVisibilityConverter());
            Resources.Add("NullToVisConverter", new NullToVisibilityConverter());
            Resources.Add("BoolToPollingTextConverter", new BoolToPollingTextConverter());
            Resources.Add("BoolToSimTextConverter", new BoolToSimTextConverter());
            Resources.Add("BoolToMilConverter", new BoolToMilConverter());
            Resources.Add("DtcCountToBrushConverter", new DtcCountToBrushConverter());
            Closing += async (s, e) =>
            {
                if (vm.IsConnected)
                    await vm.ObdService.DisconnectAsync();
            };
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToPollingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? "Live polling active" : "Polling stopped";
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToSimTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? "SIMULATION MODE" : "Hardware Mode";
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToMilConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? "🔴" : "⚫";
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DtcCountToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
            {
                if (count == 0) return Application.Current.Resources["SuccessGradientBrush"];
                if (count <= 2) return Application.Current.Resources["AccentGradientBrush"];
                return Application.Current.Resources["ErrorGradientBrush"];
            }
            return Application.Current.Resources["SurfaceVariantBrush"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IObdService _obdService;
        private CancellationTokenSource? _pollCts;
        private Timer? _pollTimer;

        public IObdService ObdService => _obdService;

        public MainViewModel(IObdService obdService)
        {
            _obdService = obdService;
            _isSimulationMode = true;
            _obdService.IsSimulationMode = true;

            ConnectCommand = new RelayCommand(async _ => await ExecuteConnectAsync());
            NavigateToDashboardCommand = new RelayCommand(_ => ActiveView = "Dashboard");
            NavigateToDtcCommand = new RelayCommand(_ => ActiveView = "Dtc");
            NavigateToLiveDataCommand = new RelayCommand(_ => ActiveView = "LiveData");
            NavigateToVehicleInfoCommand = new RelayCommand(_ => ActiveView = "VehicleInfo");
            NavigateToEcuModulesCommand = new RelayCommand(_ => ActiveView = "EcuModules");
            NavigateToSettingsCommand = new RelayCommand(_ => ActiveView = "Settings");
            ReadDtcsCommand = new RelayCommand(async _ => await ExecuteReadDtcsAsync(), _ => IsConnected);
            ClearDtcsCommand = new RelayCommand(async _ => await ExecuteClearDtcsAsync(), _ => IsConnected && HasDtcs);
            ReadLiveDataCommand = new RelayCommand(async _ => await ExecuteReadLiveDataAsync(), _ => IsConnected);
            TogglePollingCommand = new RelayCommand(_ => TogglePolling(), _ => IsConnected);
            ReadVehicleInfoCommand = new RelayCommand(async _ => await ExecuteReadVehicleInfoAsync(), _ => IsConnected);
            ReadEcuModulesCommand = new RelayCommand(async _ => await ExecuteReadEcuModulesAsync(), _ => IsConnected);

            ActiveView = "Dashboard";
            StatusMessage = "Ready. Select Simulation Mode in Settings or connect a hardware adapter.";
        }

        // ===================== Navigation =====================
        private string _activeView = "Dashboard";
        public string ActiveView
        {
            get => _activeView;
            set
            {
                if (_activeView == value) return;
                _activeView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsDtcActive));
                OnPropertyChanged(nameof(IsLiveDataActive));
                OnPropertyChanged(nameof(IsVehicleInfoActive));
                OnPropertyChanged(nameof(IsEcuModulesActive));
                OnPropertyChanged(nameof(IsSettingsActive));
            }
        }

        public bool IsDashboardActive => ActiveView == "Dashboard";
        public bool IsDtcActive => ActiveView == "Dtc";
        public bool IsLiveDataActive => ActiveView == "LiveData";
        public bool IsVehicleInfoActive => ActiveView == "VehicleInfo";
        public bool IsEcuModulesActive => ActiveView == "EcuModules";
        public bool IsSettingsActive => ActiveView == "Settings";

        // ===================== Connection =====================
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionInfo)); }
        }

        private string _connectionInfo = "";
        public string ConnectionInfo
        {
            get => _obdService.CurrentConnection?.ElmVersion ?? "";
        }

        private string _elmVersion = "Not Connected";
        public string ElmVersion
        {
            get => _obdService.CurrentConnection?.ElmVersion ?? "Not Connected";
        }

        // ===================== Settings =====================
        private bool _isSimulationMode;
        public bool IsSimulationMode
        {
            get => _isSimulationMode;
            set
            {
                _isSimulationMode = value;
                _obdService.IsSimulationMode = value;
                OnPropertyChanged();
            }
        }

        private string _selectedPort = "COM3";
        public string SelectedPort
        {
            get => _selectedPort;
            set { _selectedPort = value; OnPropertyChanged(); }
        }

        private int _selectedBaudRate = 38400;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set { _selectedBaudRate = value; OnPropertyChanged(); }
        }

        private string _selectedProtocol = "Auto";
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set { _selectedProtocol = value; OnPropertyChanged(); }
        }

        private int _connectionTimeout = 5000;
        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set { _connectionTimeout = value; OnPropertyChanged(); }
        }

        private int _pollInterval = 500;
        public int PollInterval
        {
            get => _pollInterval;
            set { _pollInterval = value; OnPropertyChanged(); }
        }

        private bool _autoStartPolling = false;
        public bool AutoStartPolling
        {
            get => _autoStartPolling;
            set { _autoStartPolling = value; OnPropertyChanged(); }
        }

        public List<string> AvailablePorts => new() { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
        public List<int> BaudRates => new() { 9600, 38400, 57600, 115200, 230400 };
        public List<string> Protocols => new() { "Auto", "ISO 9141-2", "KWP2000", "SAE J1850 VPW", "SAE J1850 PWM", "CAN 11-bit", "CAN 29-bit", "ISO 15765-4 (CAN)" };

        // ===================== Busy State =====================
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _busyMessage = "Please wait...";
        public string BusyMessage
        {
            get => _busyMessage;
            set { _busyMessage = value; OnPropertyChanged(); }
        }

        // ===================== Status =====================
        private string _statusMessage = "Ready.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private DateTime _lastUpdateTime = DateTime.Now;
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; OnPropertyChanged(); }
        }

        private int _pollCount;
        public int PollCount
        {
            get => _pollCount;
            set { _pollCount = value; OnPropertyChanged(); }
        }

        // ===================== Dashboard Values =====================
        private double _engineRpm;
        public double EngineRpm { get => _engineRpm; set { _engineRpm = value; OnPropertyChanged(); } }

        private double _vehicleSpeed;
        public double VehicleSpeed { get => _vehicleSpeed; set { _vehicleSpeed = value; OnPropertyChanged(); } }

        private double _coolantTemp;
        public double CoolantTemp { get => _coolantTemp; set { _coolantTemp = value; OnPropertyChanged(); } }

        private double _throttlePosition;
        public double ThrottlePosition { get => _throttlePosition; set { _throttlePosition = value; OnPropertyChanged(); } }

        private double _fuelLevel;
        public double FuelLevel { get => _fuelLevel; set { _fuelLevel = value; OnPropertyChanged(); } }

        private double _batteryVoltage;
        public double BatteryVoltage { get => _batteryVoltage; set { _batteryVoltage = value; OnPropertyChanged(); } }

        private double _engineLoad;
        public double EngineLoad { get => _engineLoad; set { _engineLoad = value; OnPropertyChanged(); } }

        // ===================== DTCs =====================
        private ObservableCollection<DiagnosticTroubleCode> _dtcs = new();
        public ObservableCollection<DiagnosticTroubleCode> Dtcs
        {
            get => _dtcs;
            set { _dtcs = value; OnPropertyChanged(); UpdateFilteredDtcs(); }
        }

        private ObservableCollection<DiagnosticTroubleCode> _filteredDtcs = new();
        public ObservableCollection<DiagnosticTroubleCode> FilteredDtcs
        {
            get => _filteredDtcs;
            set { _filteredDtcs = value; OnPropertyChanged(); }
        }

        private DiagnosticTroubleCode? _selectedDtc;
        public DiagnosticTroubleCode? SelectedDtc
        {
            get => _selectedDtc;
            set { _selectedDtc = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedDtcHasFreezeFrame)); }
        }

        public bool SelectedDtcHasFreezeFrame => _selectedDtc?.FreezeFrame != null;

        public int DtcCount => _dtcs.Count;
        public bool HasDtcs => _dtcs.Count > 0;

        public List<string> DtcFilterOptions => new() { "All", "Confirmed", "Pending", "Permanent", "Powertrain", "Body", "Chassis", "Network" };

        private string _selectedDtcFilter = "All";
        public string SelectedDtcFilter
        {
            get => _selectedDtcFilter;
            set { _selectedDtcFilter = value; OnPropertyChanged(); UpdateFilteredDtcs(); }
        }

        private void UpdateFilteredDtcs()
        {
            var filtered = _selectedDtcFilter switch
            {
                "Confirmed" => _dtcs.Where(d => d.Status == DtcStatus.Confirmed),
                "Pending" => _dtcs.Where(d => d.Status == DtcStatus.Pending),
                "Permanent" => _dtcs.Where(d => d.Status == DtcStatus.Permanent),
                "Powertrain" => _dtcs.Where(d => d.Category == DtcCategory.Powertrain),
                "Body" => _dtcs.Where(d => d.Category == DtcCategory.Body),
                "Chassis" => _dtcs.Where(d => d.Category == DtcCategory.Chassis),
                "Network" => _dtcs.Where(d => d.Category == DtcCategory.Network),
                _ => _dtcs.AsEnumerable()
            };
            FilteredDtcs = new ObservableCollection<DiagnosticTroubleCode>(filtered);
            OnPropertyChanged(nameof(DtcCount));
            OnPropertyChanged(nameof(HasDtcs));
        }

        // ===================== Live Data =====================
        private ObservableCollection<LiveDataParameter> _liveData = new();
        public ObservableCollection<LiveDataParameter> LiveData
        {
            get => _liveData;
            set { _liveData = value; OnPropertyChanged(); UpdateFilteredLiveData(); }
        }

        private ObservableCollection<LiveDataParameter> _filteredLiveData = new();
        public ObservableCollection<LiveDataParameter> FilteredLiveData
        {
            get => _filteredLiveData;
            set { _filteredLiveData = value; OnPropertyChanged(); }
        }

        public List<string> PidCategoryFilters => new() { "All", "Engine", "Fuel", "Temperature", "Pressure", "Speed", "Electrical", "Sensor", "Emissions" };

        private string _selectedPidCategory = "All";
        public string SelectedPidCategory
        {
            get => _selectedPidCategory;
            set { _selectedPidCategory = value; OnPropertyChanged(); UpdateFilteredLiveData(); }
        }

        private void UpdateFilteredLiveData()
        {
            if (_selectedPidCategory == "All")
            {
                FilteredLiveData = new ObservableCollection<LiveDataParameter>(_liveData);
                return;
            }
            if (Enum.TryParse<PidCategory>(_selectedPidCategory, out var cat))
                FilteredLiveData = new ObservableCollection<LiveDataParameter>(_liveData.Where(p => p.Category == cat));
            else
                FilteredLiveData = new ObservableCollection<LiveDataParameter>(_liveData);
        }

        private bool _isPolling;
        public bool IsPolling
        {
            get => _isPolling;
            set { _isPolling = value; OnPropertyChanged(); }
        }

        // ===================== Vehicle Info =====================
        private VehicleInfo _vehicleInfo = new();
        public VehicleInfo VehicleInfo
        {
            get => _vehicleInfo;
            set { _vehicleInfo = value; OnPropertyChanged(); }
        }

        // ===================== ECU Modules =====================
        private ObservableCollection<EcuModule> _ecuModules = new();
        public ObservableCollection<EcuModule> EcuModules
        {
            get => _ecuModules;
            set { _ecuModules = value; OnPropertyChanged(); }
        }

        private EcuModule? _selectedEcuModule;
        public EcuModule? SelectedEcuModule
        {
            get => _selectedEcuModule;
            set { _selectedEcuModule = value; OnPropertyChanged(); }
        }

        // ===================== Commands =====================
        public ICommand ConnectCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToDtcCommand { get; }
        public ICommand NavigateToLiveDataCommand { get; }
        public ICommand NavigateToVehicleInfoCommand { get; }
        public ICommand NavigateToEcuModulesCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ReadDtcsCommand { get; }
        public ICommand ClearDtcsCommand { get; }
        public ICommand ReadLiveDataCommand { get; }
        public ICommand TogglePollingCommand { get; }
        public ICommand ReadVehicleInfoCommand { get; }
        public ICommand ReadEcuModulesCommand { get; }

        // ===================== Command Implementations =====================
        private async Task ExecuteConnectAsync()
        {
            if (IsConnected)
            {
                StopPolling();
                await _obdService.DisconnectAsync();
                IsConnected = false;
                StatusMessage = "Disconnected.";
                return;
            }

            IsBusy = true;
            BusyMessage = "Connecting to OBD adapter...";
            try
            {
                var conn = new ObdConnection
                {
                    Type = _isSimulationMode ? ConnectionType.Simulation : ConnectionType.Serial,
                    PortName = _selectedPort,
                    BaudRate = _selectedBaudRate,
                    Timeout = _connectionTimeout
                };
                _obdService.IsSimulationMode = _isSimulationMode;
                bool ok = await _obdService.ConnectAsync(conn);
                IsConnected = ok;
                if (ok)
                {
                    StatusMessage = $"Connected. {_obdService.CurrentConnection?.ElmVersion}";
                    OnPropertyChanged(nameof(ElmVersion));
                    OnPropertyChanged(nameof(ConnectionInfo));
                    await ExecuteReadVehicleInfoAsync();
                    if (_autoStartPolling) TogglePolling();
                }
                else
                {
                    StatusMessage = "Connection failed.";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Connection failed:\n{ex.Message}", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteReadDtcsAsync()
        {
            IsBusy = true;
            BusyMessage = "Reading fault codes...";
            try
            {
                var dtcs = await _obdService.ReadDtcsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Dtcs = new ObservableCollection<DiagnosticTroubleCode>(dtcs);
                    UpdateFilteredDtcs();
                    LastUpdateTime = DateTime.Now;
                    StatusMessage = dtcs.Count == 0
                        ? "No fault codes found."
                        : $"Found {dtcs.Count} fault code(s).";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading DTCs: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteClearDtcsAsync()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all fault codes?\nThis cannot be undone.",
                "Clear Fault Codes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            BusyMessage = "Clearing fault codes...";
            try
            {
                bool ok = await _obdService.ClearDtcsAsync();
                if (ok)
                {
                    Dtcs.Clear();
                    FilteredDtcs.Clear();
                    OnPropertyChanged(nameof(DtcCount));
                    OnPropertyChanged(nameof(HasDtcs));
                    StatusMessage = "Fault codes cleared successfully.";
                }
                else
                {
                    StatusMessage = "Failed to clear fault codes.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing DTCs: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteReadLiveDataAsync()
        {
            try
            {
                var data = await _obdService.ReadLiveDataAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LiveData = new ObservableCollection<LiveDataParameter>(data);
                    UpdateFilteredLiveData();
                    UpdateDashboardFromLiveData(data);
                    LastUpdateTime = DateTime.Now;
                    PollCount++;
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading live data: {ex.Message}";
            }
        }

        private void UpdateDashboardFromLiveData(List<LiveDataParameter> data)
        {
            foreach (var p in data)
            {
                switch (p.PidHex)
                {
                    case "0x0C": EngineRpm = p.Value; break;
                    case "0x0D": VehicleSpeed = p.Value; break;
                    case "0x05": CoolantTemp = p.Value; break;
                    case "0x11": ThrottlePosition = p.Value; break;
                    case "0x2F": FuelLevel = p.Value; break;
                    case "0x42": BatteryVoltage = p.Value; break;
                    case "0x04": EngineLoad = p.Value; break;
                }
            }
        }

        private void TogglePolling()
        {
            if (IsPolling) StopPolling();
            else StartPolling();
        }

        private void StartPolling()
        {
            if (IsPolling) return;
            IsPolling = true;
            _pollCts = new CancellationTokenSource();
            var token = _pollCts.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await ExecuteReadLiveDataAsync();
                        await Task.Delay(_pollInterval, token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch { await Task.Delay(1000, token); }
                }
            }, token);
            StatusMessage = "Live data polling started.";
        }

        private void StopPolling()
        {
            if (!IsPolling) return;
            _pollCts?.Cancel();
            _pollCts = null;
            IsPolling = false;
            StatusMessage = "Live data polling stopped.";
        }

        private async Task ExecuteReadVehicleInfoAsync()
        {
            IsBusy = true;
            BusyMessage = "Reading vehicle information...";
            try
            {
                var info = await _obdService.ReadVehicleInfoAsync();
                VehicleInfo = info;
                if (info.EcuModules?.Count > 0)
                    EcuModules = new ObservableCollection<EcuModule>(info.EcuModules);
                StatusMessage = string.IsNullOrEmpty(info.Vin)
                    ? "Vehicle info read (no VIN)."
                    : $"Vehicle: {info.Year} {info.Make} {info.Model} — VIN: {info.Vin}";
                OnPropertyChanged(nameof(ElmVersion));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading vehicle info: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteReadEcuModulesAsync()
        {
            IsBusy = true;
            BusyMessage = "Scanning ECU modules...";
            try
            {
                var modules = await _obdService.ReadEcuModulesAsync();
                EcuModules = new ObservableCollection<EcuModule>(modules);
                StatusMessage = $"Found {modules.Count} ECU module(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading ECU modules: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===================== INotifyPropertyChanged =====================
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
