using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class Mode06ViewModel : BaseViewModel
    {
        private readonly Obd2ExtendedService _service;
        private bool _isBusy;
        public ObservableCollection<OnboardTest> Tests { get; } = new();
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public ICommand ReadCommand { get; }

        public Mode06ViewModel(IObdService obdService)
        {
            _service = new Obd2ExtendedService(obdService);
            ReadCommand = new RelayCommand(async _ => await ReadAsync(), _ => !IsBusy);
        }

        private async Task ReadAsync()
        {
            IsBusy = true;
            Tests.Clear();
            try
            {
                var tests = await _service.ReadOnboardTestsAsync();
                foreach (var t in tests) Tests.Add(t);
                StatusMessage = $"{tests.Count} on-board test(s) retrieved.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
