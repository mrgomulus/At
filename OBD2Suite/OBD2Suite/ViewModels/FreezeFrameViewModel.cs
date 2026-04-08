using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class FreezeFrameViewModel : BaseViewModel
    {
        private readonly Obd2ExtendedService _service;
        private bool _isBusy;
        public ObservableCollection<FreezeFrame> FreezeFrames { get; } = new();
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public ICommand ReadCommand { get; }

        public FreezeFrameViewModel(IObdService obdService)
        {
            _service = new Obd2ExtendedService(obdService);
            ReadCommand = new RelayCommand(async _ => await ReadAsync(), _ => !IsBusy);
        }

        private async Task ReadAsync()
        {
            IsBusy = true;
            FreezeFrames.Clear();
            try
            {
                var frames = await _service.ReadFreezeFramesAsync();
                foreach (var f in frames) FreezeFrames.Add(f);
                StatusMessage = frames.Count == 0 ? "No freeze frame data found." : $"{frames.Count} freeze frame(s) loaded.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
