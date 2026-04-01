using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class ReadinessViewModel : BaseViewModel
    {
        private readonly Obd2ExtendedService _service;
        private ReadinessReport? _report;
        private bool _isBusy;

        public ReadinessReport? Report  { get => _report;  set { _report = value;  OnPropertyChanged(); OnPropertyChanged(nameof(HasReport)); } }
        public bool IsBusy              { get => _isBusy;  set { _isBusy = value;  OnPropertyChanged(); } }
        public bool HasReport           => _report != null;

        public ICommand ReadCommand { get; }

        public ReadinessViewModel(IObdService obdService)
        {
            _service = new Obd2ExtendedService(obdService);
            ReadCommand = new RelayCommand(async _ => await ReadAsync(), _ => !IsBusy);
        }

        private async Task ReadAsync()
        {
            IsBusy = true;
            try
            {
                Report = await _service.ReadReadinessAsync();
                StatusMessage = Report.IsReadyForTest ? "Ready for emissions test." : "Not ready — complete drive cycle.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
