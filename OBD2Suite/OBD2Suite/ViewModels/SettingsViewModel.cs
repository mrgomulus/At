using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IObdService          _obdService;
        private readonly AdapterScannerService _scanner;

        // ── connection-type selection ────────────────────────────────────────
        private ConnectionType _selectedConnectionType = ConnectionType.Serial;
        public List<ConnectionType> ConnectionTypes { get; } =
            Enum.GetValues<ConnectionType>().Where(t => t != ConnectionType.Simulation).ToList();

        public ConnectionType SelectedConnectionType
        {
            get => _selectedConnectionType;
            set
            {
                if (_selectedConnectionType != value)
                {
                    _selectedConnectionType = value;
                    OnPropertyChanged(nameof(SelectedConnectionType));
                    OnPropertyChanged(nameof(IsSerialMode));
                    OnPropertyChanged(nameof(IsWiFiMode));
                    OnPropertyChanged(nameof(IsBluetoothMode));
                    OnPropertyChanged(nameof(ShowAdapterList));
                }
            }
        }

        public bool IsSerialMode
        {
            get => _selectedConnectionType == ConnectionType.Serial;
            set { if (value) SelectedConnectionType = ConnectionType.Serial; }
        }

        public bool IsWiFiMode
        {
            get => _selectedConnectionType == ConnectionType.WiFi;
            set { if (value) SelectedConnectionType = ConnectionType.WiFi; }
        }

        public bool IsBluetoothMode
        {
            get => _selectedConnectionType == ConnectionType.Bluetooth;
            set { if (value) SelectedConnectionType = ConnectionType.Bluetooth; }
        }
        public bool ShowAdapterList => FoundAdapters.Count > 0 || IsScanning;

        // ── adapter scanning ─────────────────────────────────────────────────
        private bool   _isScanning;
        private string _scanStatus = "";

        public ObservableCollection<ObdAdapterInfo> FoundAdapters { get; } = new();

        private ObdAdapterInfo? _selectedAdapter;
        public ObdAdapterInfo? SelectedAdapter
        {
            get => _selectedAdapter;
            set
            {
                if (SetProperty(ref _selectedAdapter, value) && value != null)
                    ApplyAdapter(value);
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set { SetProperty(ref _isScanning, value); OnPropertyChanged(nameof(ShowAdapterList)); }
        }

        public string ScanStatus
        {
            get => _scanStatus;
            set => SetProperty(ref _scanStatus, value);
        }

        // ── serial (USB) settings ────────────────────────────────────────────
        private string _selectedPort = "COM3";
        private int    _selectedBaudRate = 38400;
        private ProtocolType _selectedProtocol = ProtocolType.Auto;

        public ObservableCollection<string> AvailablePorts { get; } = new();
        public List<int>          BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200, 230400 };
        public List<ProtocolType> Protocols { get; } = Enum.GetValues<ProtocolType>().ToList();

        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        public ProtocolType SelectedProtocol
        {
            get => _selectedProtocol;
            set => SetProperty(ref _selectedProtocol, value);
        }

        // ── WiFi settings ────────────────────────────────────────────────────
        private string _wifiIpAddress = "192.168.0.10";
        private int    _wifiPort      = 35000;

        public string WiFiIpAddress
        {
            get => _wifiIpAddress;
            set => SetProperty(ref _wifiIpAddress, value);
        }

        public int WiFiPort
        {
            get => _wifiPort;
            set => SetProperty(ref _wifiPort, value);
        }

        // ── Bluetooth settings ───────────────────────────────────────────────
        private string _bluetoothAddress = "";
        private string _bluetoothComPort = "";

        public string BluetoothAddress
        {
            get => _bluetoothAddress;
            set => SetProperty(ref _bluetoothAddress, value);
        }

        public string BluetoothComPort
        {
            get => _bluetoothComPort;
            set => SetProperty(ref _bluetoothComPort, value);
        }

        // ── common status ────────────────────────────────────────────────────
        private bool   _simulationMode = true;
        private string _statusMessage  = "Configure connection settings and click Connect";
        private bool   _isBusy;
        private string _elmInfo = "";

        public event Action<bool>? ConnectionChanged;

        public bool SimulationMode
        {
            get => _simulationMode;
            set
            {
                SetProperty(ref _simulationMode, value);
                _obdService.IsSimulationMode = value;
            }
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

        public string ElmInfo
        {
            get => _elmInfo;
            set => SetProperty(ref _elmInfo, value);
        }

        public bool IsConnected => _obdService.IsConnected;

        // ── commands ─────────────────────────────────────────────────────────
        public ICommand ConnectCommand       { get; }
        public ICommand DisconnectCommand    { get; }
        public ICommand RefreshPortsCommand  { get; }
        public ICommand ScanAdaptersCommand  { get; }
        public ICommand UseSelectedAdapterCommand { get; }

        public SettingsViewModel(IObdService obdService)
            : this(obdService, new AdapterScannerService()) { }

        public SettingsViewModel(IObdService obdService, AdapterScannerService scanner)
        {
            _obdService = obdService;
            _scanner    = scanner;
            _obdService.IsSimulationMode = _simulationMode;

            ConnectCommand    = new RelayCommand(async _ => await ConnectAsync(),    _ => !IsBusy && !IsConnected);
            DisconnectCommand = new RelayCommand(async _ => await DisconnectAsync(), _ => !IsBusy && IsConnected);
            RefreshPortsCommand      = new RelayCommand(_ => RefreshPorts());
            ScanAdaptersCommand      = new RelayCommand(async _ => await ScanAdaptersAsync(), _ => !IsScanning && !IsBusy);
            UseSelectedAdapterCommand = new RelayCommand(
                _ => { if (SelectedAdapter != null) ApplyAdapter(SelectedAdapter); },
                _ => SelectedAdapter != null);

            RefreshPorts();
        }

        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var p in SerialPort.GetPortNames())
                AvailablePorts.Add(p);
            if (AvailablePorts.Count == 0)
                AvailablePorts.Add("No ports found");
            if (!AvailablePorts.Contains(SelectedPort) && AvailablePorts.Count > 0)
                SelectedPort = AvailablePorts[0];
        }

        // ── Adapter scanning ─────────────────────────────────────────────────

        private async Task ScanAdaptersAsync()
        {
            IsScanning = true;
            ScanStatus = "Scanning for OBD adapters…";
            FoundAdapters.Clear();
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                var iface = _selectedConnectionType switch
                {
                    ConnectionType.Bluetooth => AdapterInterface.Bluetooth,
                    ConnectionType.WiFi      => AdapterInterface.Network,
                    _                        => AdapterInterface.Serial
                };

                IReadOnlyList<ObdAdapterInfo> found;
                if (_selectedConnectionType == ConnectionType.Serial)
                {
                    // When USB/Serial is selected, scan all interfaces so the
                    // user can see — and switch to — any available adapter type
                    // from a single scan action.
                    found = await _scanner.ScanAllAsync(cts.Token);
                }
                else
                {
                    found = await _scanner.ScanAsync(iface, cts.Token);
                }

                foreach (var a in found)
                    FoundAdapters.Add(a);

                ScanStatus = FoundAdapters.Count == 0
                    ? "No adapters found."
                    : $"Found {FoundAdapters.Count} adapter(s).";
                OnPropertyChanged(nameof(ShowAdapterList));
            }
            catch (Exception ex)
            {
                ScanStatus = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Fills the manual-entry fields from a discovered adapter.
        /// </summary>
        private void ApplyAdapter(ObdAdapterInfo adapter)
        {
            switch (adapter.Interface)
            {
                case AdapterInterface.Serial:
                    SelectedConnectionType = ConnectionType.Serial;
                    if (adapter.PortName != null)
                    {
                        if (!AvailablePorts.Contains(adapter.PortName))
                            AvailablePorts.Add(adapter.PortName);
                        SelectedPort = adapter.PortName;
                    }
                    break;

                case AdapterInterface.Network:
                    SelectedConnectionType = ConnectionType.WiFi;
                    WiFiIpAddress = adapter.IpAddress ?? WiFiIpAddress;
                    WiFiPort      = adapter.Port > 0 ? adapter.Port : WiFiPort;
                    break;

                case AdapterInterface.Bluetooth:
                    SelectedConnectionType = ConnectionType.Bluetooth;
                    BluetoothAddress = adapter.BluetoothAddress ?? BluetoothAddress;
                    BluetoothComPort = adapter.PortName ?? "";
                    break;
            }
        }

        // ── Connect / Disconnect ─────────────────────────────────────────────

        private async Task ConnectAsync()
        {
            IsBusy = true;
            try
            {
                ObdConnection conn;
                string label;

                if (SimulationMode)
                {
                    conn  = new ObdConnection { Type = ConnectionType.Simulation };
                    label = "Starting simulation…";
                }
                else
                {
                    (conn, label) = BuildConnection();
                }

                StatusMessage = label;
                _obdService.IsSimulationMode = SimulationMode;
                var ok = await _obdService.ConnectAsync(conn);
                OnPropertyChanged(nameof(IsConnected));
                if (ok)
                {
                    ElmInfo       = _obdService.CurrentConnection?.ElmVersion ?? "";
                    StatusMessage = SimulationMode
                        ? "Simulation mode active — all data is generated"
                        : $"Connected: {ElmInfo}";
                    ConnectionChanged?.Invoke(true);
                }
                else
                {
                    StatusMessage = "Connection failed.";
                    ConnectionChanged?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                ConnectionChanged?.Invoke(false);
            }
            finally { IsBusy = false; }
        }

        private (ObdConnection conn, string label) BuildConnection() =>
            _selectedConnectionType switch
            {
                ConnectionType.WiFi => (
                    new ObdConnection
                    {
                        Type        = ConnectionType.WiFi,
                        IpAddress   = WiFiIpAddress,
                        NetworkPort = WiFiPort,
                        Protocol    = SelectedProtocol,
                        Timeout     = 5000
                    },
                    $"Connecting to {WiFiIpAddress}:{WiFiPort}…"),

                ConnectionType.Bluetooth => (
                    new ObdConnection
                    {
                        Type             = ConnectionType.Bluetooth,
                        BluetoothAddress = BluetoothAddress,
                        PortName         = BluetoothComPort,
                        Protocol         = SelectedProtocol,
                        Timeout          = 8000
                    },
                    $"Connecting via Bluetooth ({(string.IsNullOrWhiteSpace(BluetoothComPort) ? BluetoothAddress : BluetoothComPort)})…"),

                _ => (
                    new ObdConnection
                    {
                        Type     = ConnectionType.Serial,
                        PortName  = SelectedPort,
                        BaudRate  = SelectedBaudRate,
                        Protocol  = SelectedProtocol,
                        Timeout   = 5000
                    },
                    $"Connecting to {SelectedPort}…")
            };

        private async Task DisconnectAsync()
        {
            IsBusy = true;
            StatusMessage = "Disconnecting…";
            try
            {
                await _obdService.DisconnectAsync();
                OnPropertyChanged(nameof(IsConnected));
                ElmInfo       = "";
                StatusMessage = "Disconnected";
                ConnectionChanged?.Invoke(false);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
