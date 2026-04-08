using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class VinDecoderViewModel : BaseViewModel
    {
        private string _vinInput = "";
        private VinDecodeResult? _result;
        private bool _hasResult;

        public string VinInput
        {
            get => _vinInput;
            set { _vinInput = value.ToUpperInvariant(); OnPropertyChanged(); }
        }

        public VinDecodeResult? Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); HasResult = value != null; }
        }

        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }

        public ObservableCollection<(string Field, string Value)> DecodeFields { get; } = new();

        public ICommand DecodeCommand { get; }

        public VinDecoderViewModel()
        {
            DecodeCommand = new RelayCommand(_ => Decode(), _ => VinInput.Length >= 5);
        }

        private void Decode()
        {
            var result = VinDecoderService.Decode(VinInput.Trim());
            Result = result;
            DecodeFields.Clear();
            foreach (var (field, value) in result.ToDisplayList())
                DecodeFields.Add((field, value));
            StatusMessage = result.IsValid
                ? $"VIN decoded: {result.Manufacturer} — {result.ModelYear}"
                : $"VIN warning: {result.Error}";
        }
    }
}
