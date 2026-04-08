using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class VehicleInfoViewModel : BaseViewModel
    {
        private readonly IObdService _obdService;
        private VehicleInfo? _vehicleInfo;
        private string _statusMessage = "Ready — press 'Scan Vehicle' to read info";
        private bool _isBusy;
        private EcuModule? _selectedEcu;

        public VehicleInfo? VehicleInfo
        {
            get => _vehicleInfo;
            set => SetProperty(ref _vehicleInfo, value);
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

        public EcuModule? SelectedEcu
        {
            get => _selectedEcu;
            set => SetProperty(ref _selectedEcu, value);
        }

        public ObservableCollection<EcuModule> EcuModules { get; } = new();

        public ICommand ScanVehicleCommand { get; }
        public ICommand ScanEcusCommand { get; }

        public VehicleInfoViewModel(IObdService obdService)
        {
            _obdService = obdService;
            ScanVehicleCommand = new RelayCommand(async _ => await ScanVehicleAsync(), _ => !IsBusy);
            ScanEcusCommand    = new RelayCommand(async _ => await ScanEcusAsync(),    _ => !IsBusy);
        }

        private async Task ScanVehicleAsync()
        {
            IsBusy = true;
            StatusMessage = "Reading vehicle information…";
            try
            {
                VehicleInfo = await _obdService.ReadVehicleInfoAsync();
                StatusMessage = $"VIN: {VehicleInfo.Vin}  |  {VehicleInfo.Make} {VehicleInfo.Model} {VehicleInfo.Year}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task ScanEcusAsync()
        {
            IsBusy = true;
            StatusMessage = "Scanning all ECUs…";
            try
            {
                var ecus = await _obdService.ReadEcuModulesAsync();
                EcuModules.Clear();
                foreach (var e in ecus) EcuModules.Add(e);
                StatusMessage = $"Found {ecus.Count} ECU module(s)";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
