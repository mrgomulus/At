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
        private readonly IObdService _obdService;

        private string _selectedPort = "COM3";
        private int _selectedBaudRate = 38400;
        private ProtocolType _selectedProtocol = ProtocolType.Auto;
        private bool _simulationMode = true;
        private string _statusMessage = "Configure connection settings and click Connect";
        private bool _isBusy;
        private string _elmInfo = "";

        public event Action<bool>? ConnectionChanged;

        public ObservableCollection<string> AvailablePorts { get; } = new();

        public List<int> BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200, 230400 };

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

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshPortsCommand { get; }

        public SettingsViewModel(IObdService obdService)
        {
            _obdService = obdService;
            _obdService.IsSimulationMode = _simulationMode;

            ConnectCommand    = new RelayCommand(async _ => await ConnectAsync(),    _ => !IsBusy && !IsConnected);
            DisconnectCommand = new RelayCommand(async _ => await DisconnectAsync(), _ => !IsBusy && IsConnected);
            RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());

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

        private async Task ConnectAsync()
        {
            IsBusy = true;
            StatusMessage = SimulationMode ? "Starting simulation…" : $"Connecting to {SelectedPort}…";
            try
            {
                var conn = new ObdConnection
                {
                    Type = SimulationMode ? ConnectionType.Simulation : ConnectionType.Serial,
                    PortName = SelectedPort,
                    BaudRate = SelectedBaudRate,
                    Protocol = SelectedProtocol,
                    Timeout = 5000
                };
                _obdService.IsSimulationMode = SimulationMode;
                var ok = await _obdService.ConnectAsync(conn);
                OnPropertyChanged(nameof(IsConnected));
                if (ok)
                {
                    ElmInfo = _obdService.CurrentConnection?.ElmVersion ?? "";
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

        private async Task DisconnectAsync()
        {
            IsBusy = true;
            StatusMessage = "Disconnecting…";
            try
            {
                await _obdService.DisconnectAsync();
                OnPropertyChanged(nameof(IsConnected));
                ElmInfo = "";
                StatusMessage = "Disconnected";
                ConnectionChanged?.Invoke(false);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
