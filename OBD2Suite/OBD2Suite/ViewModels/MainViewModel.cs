using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private BaseViewModel _currentView = null!;
        private bool _isConnected;
        private string _connectionInfo = "No device";
        private bool _isBusy;
        private string _statusMessage = "Ready";

        public DashboardViewModel        DashboardVM          { get; }
        public DtcViewModel              DtcVM                { get; }
        public VehicleInfoViewModel      VehicleInfoVM        { get; }
        public CodingViewModel           CodingVM             { get; }
        public OemFeaturesViewModel      OemFeaturesVM        { get; }
        public SettingsViewModel         SettingsVM           { get; }
        public ReadinessViewModel        ReadinessVM          { get; }
        public FreezeFrameViewModel      FreezeFrameVM        { get; }
        public Mode06ViewModel           Mode06VM             { get; }
        public SpecialFunctionsViewModel SpecialFunctionsVM   { get; }
        public DataLoggerViewModel       DataLoggerVM         { get; }
        public VinDecoderViewModel       VinDecoderVM         { get; }

        public BaseViewModel CurrentView    { get => _currentView;    set => SetProperty(ref _currentView, value); }
        public bool IsConnected             { get => _isConnected;    set => SetProperty(ref _isConnected, value); }
        public string ConnectionInfo        { get => _connectionInfo; set => SetProperty(ref _connectionInfo, value); }
        public bool IsBusy                  { get => _isBusy;         set => SetProperty(ref _isBusy, value); }
        public string StatusMessage         { get => _statusMessage;  set => SetProperty(ref _statusMessage, value); }

        public ICommand NavigateDashboardCommand        { get; }
        public ICommand NavigateDtcCommand              { get; }
        public ICommand NavigateVehicleInfoCommand      { get; }
        public ICommand NavigateCodingCommand           { get; }
        public ICommand NavigateOemFeaturesCommand      { get; }
        public ICommand NavigateSettingsCommand         { get; }
        public ICommand NavigateReadinessCommand        { get; }
        public ICommand NavigateFreezeFrameCommand      { get; }
        public ICommand NavigateMode06Command           { get; }
        public ICommand NavigateSpecialFunctionsCommand { get; }
        public ICommand NavigateDataLoggerCommand       { get; }
        public ICommand NavigateVinDecoderCommand       { get; }
        public ICommand ConnectCommand                  { get; }

        public MainViewModel(IObdService obdService)
        {
            _obdService = obdService;

            SettingsVM           = new SettingsViewModel(_obdService);
            DashboardVM          = new DashboardViewModel(_obdService);
            DtcVM                = new DtcViewModel(_obdService);
            VehicleInfoVM        = new VehicleInfoViewModel(_obdService);
            CodingVM             = new CodingViewModel(_obdService);
            OemFeaturesVM        = new OemFeaturesViewModel(_obdService);
            ReadinessVM          = new ReadinessViewModel(_obdService);
            FreezeFrameVM        = new FreezeFrameViewModel(_obdService);
            Mode06VM             = new Mode06ViewModel(_obdService);
            SpecialFunctionsVM   = new SpecialFunctionsViewModel(_obdService);
            DataLoggerVM         = new DataLoggerViewModel(_obdService);
            VinDecoderVM         = new VinDecoderViewModel();

            SettingsVM.ConnectionChanged += OnConnectionChanged;

            NavigateDashboardCommand        = new RelayCommand(_ => CurrentView = DashboardVM);
            NavigateDtcCommand              = new RelayCommand(_ => CurrentView = DtcVM);
            NavigateVehicleInfoCommand      = new RelayCommand(_ => CurrentView = VehicleInfoVM);
            NavigateCodingCommand           = new RelayCommand(_ => CurrentView = CodingVM);
            NavigateOemFeaturesCommand      = new RelayCommand(_ => CurrentView = OemFeaturesVM);
            NavigateSettingsCommand         = new RelayCommand(_ => CurrentView = SettingsVM);
            NavigateReadinessCommand        = new RelayCommand(_ => CurrentView = ReadinessVM);
            NavigateFreezeFrameCommand      = new RelayCommand(_ => CurrentView = FreezeFrameVM);
            NavigateMode06Command           = new RelayCommand(_ => CurrentView = Mode06VM);
            NavigateSpecialFunctionsCommand = new RelayCommand(_ => CurrentView = SpecialFunctionsVM);
            NavigateDataLoggerCommand       = new RelayCommand(_ => CurrentView = DataLoggerVM);
            NavigateVinDecoderCommand       = new RelayCommand(_ => CurrentView = VinDecoderVM);
            ConnectCommand                  = new RelayCommand(async _ => await ToggleConnectionAsync());

            CurrentView = SettingsVM;
        }

        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
            if (connected && _obdService.CurrentConnection != null)
            {
                var c = _obdService.CurrentConnection;
                ConnectionInfo = _obdService.IsSimulationMode ? "Simulation Mode" : $"{c.PortName} @ {c.BaudRate}";
                StatusMessage  = $"Connected — {c.ElmVersion}";
            }
            else
            {
                ConnectionInfo = "No device";
                StatusMessage  = "Disconnected";
            }
        }

        private async Task ToggleConnectionAsync()
        {
            if (_obdService.IsConnected)
            {
                await _obdService.DisconnectAsync();
                IsConnected    = false;
                ConnectionInfo = "No device";
                StatusMessage  = "Disconnected";
            }
            else
            {
                CurrentView = SettingsVM;
            }
        }
    }
}
