using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private DispatcherTimer? _pollTimer;

        private bool _isPolling;
        private string _statusMessage = "Not polling";
        private int _refreshInterval = 1000;

        public ObservableCollection<LiveDataParameter> LiveParameters { get; } = new();

        public bool IsPolling
        {
            get => _isPolling;
            set
            {
                SetProperty(ref _isPolling, value);
                OnPropertyChanged(nameof(PollButtonLabel));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                if (SetProperty(ref _refreshInterval, value) && _pollTimer != null)
                    _pollTimer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        public string PollButtonLabel => IsPolling ? "Stop Polling" : "Start Polling";

        public ICommand TogglePollCommand { get; }
        public ICommand RefreshOnceCommand { get; }

        public DashboardViewModel(IObdService obdService)
        {
            _obdService = obdService;
            TogglePollCommand = new RelayCommand(_ => TogglePolling());
            RefreshOnceCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsPolling);
        }

        private void TogglePolling()
        {
            if (IsPolling)
                StopPolling();
            else
                StartPolling();
        }

        public void StartPolling()
        {
            if (IsPolling) return;
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RefreshInterval) };
            _pollTimer.Tick += async (_, _) => await RefreshAsync();
            _pollTimer.Start();
            IsPolling = true;
            StatusMessage = "Polling…";
        }

        public void StopPolling()
        {
            _pollTimer?.Stop();
            _pollTimer = null;
            IsPolling = false;
            StatusMessage = "Polling stopped";
        }

        public async Task RefreshAsync()
        {
            try
            {
                var parameters = await _obdService.ReadLiveDataAsync();
                LiveParameters.Clear();
                foreach (var p in parameters)
                    LiveParameters.Add(p);
                StatusMessage = $"Last update: {DateTime.Now:HH:mm:ss}  ({parameters.Count} parameters)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }
}
