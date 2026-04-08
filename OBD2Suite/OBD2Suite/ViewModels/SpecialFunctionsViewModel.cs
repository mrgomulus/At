using System.Collections.ObjectModel;
using System.Windows.Input;
using OBD2Suite.Commands;
using OBD2Suite.Models;
using OBD2Suite.Services;

namespace OBD2Suite.ViewModels
{
    public class SpecialFunctionsViewModel : BaseViewModel
    {
        private readonly SpecialFunctionsService _svc;
        private bool _isBusy;
        private SpecialFunction? _selectedFunction;
        private string _selectedCategory = "All";
        private string _lastResult = "";
        private BatteryTestInfo? _batteryInfo;
        private bool _showBatteryTest;

        public ObservableCollection<SpecialFunction> AllFunctions  { get; } = new();
        public ObservableCollection<SpecialFunction> FilteredFunctions { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Log { get; } = new();

        public SpecialFunction? SelectedFunction { get => _selectedFunction; set { _selectedFunction = value; OnPropertyChanged(); } }
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); ApplyFilter(); }
        }
        public bool IsBusy          { get => _isBusy;          set { _isBusy = value;          OnPropertyChanged(); } }
        public string LastResult    { get => _lastResult;       set { _lastResult = value;       OnPropertyChanged(); } }
        public BatteryTestInfo? BatteryInfo { get => _batteryInfo; set { _batteryInfo = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBattery)); } }
        public bool HasBattery      => _batteryInfo != null;
        public bool ShowBatteryTest { get => _showBatteryTest;  set { _showBatteryTest = value; OnPropertyChanged(); } }

        public ICommand RunFunctionCommand   { get; }
        public ICommand BatteryTestCommand   { get; }
        public ICommand ClearLogCommand      { get; }

        public SpecialFunctionsViewModel(IObdService obdService)
        {
            _svc = new SpecialFunctionsService(obdService);

            var functions = SpecialFunctionsService.GetAllFunctions();
            foreach (var f in functions) AllFunctions.Add(f);

            var cats = new[] { "All" }.Concat(
                Enum.GetValues<SpecialFunctionCategory>().Select(c => c.ToString())
            );
            foreach (var c in cats) Categories.Add(c);

            ApplyFilter();

            RunFunctionCommand  = new RelayCommand(async p => await RunAsync(p as SpecialFunction), _ => !IsBusy);
            BatteryTestCommand  = new RelayCommand(async _ => await RunBatteryTestAsync(), _ => !IsBusy);
            ClearLogCommand     = new RelayCommand(_ => Log.Clear());
        }

        private void ApplyFilter()
        {
            FilteredFunctions.Clear();
            foreach (var f in AllFunctions)
            {
                if (SelectedCategory == "All" || f.Category.ToString() == SelectedCategory)
                    FilteredFunctions.Add(f);
            }
        }

        private async Task RunAsync(SpecialFunction? func)
        {
            if (func == null) return;
            IsBusy = true;
            func.Status = SpecialFunctionStatus.Running;
            try
            {
                Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ▶ Starting: {func.Name}");
                var result = await _svc.ExecuteFunctionAsync(func);
                LastResult = result;
                func.Status = result.StartsWith("✔") ? SpecialFunctionStatus.Success : SpecialFunctionStatus.Failed;
                func.LastResult = result;
                Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {result}");
            }
            catch (Exception ex)
            {
                func.Status = SpecialFunctionStatus.Failed;
                Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ✖ Exception: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task RunBatteryTestAsync()
        {
            IsBusy = true;
            ShowBatteryTest = false;
            Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ▶ Starting battery conductance test…");
            try
            {
                BatteryInfo = await _svc.RunBatteryTestAsync();
                ShowBatteryTest = true;
                Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {BatteryInfo.ResultText}");
            }
            catch (Exception ex) { Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ✖ {ex.Message}"); }
            finally { IsBusy = false; }
        }
    }
}
