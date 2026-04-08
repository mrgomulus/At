using OBD2Suite.Models;
using OBD2Suite.Services;
using Xunit;

namespace OBD2Suite.Tests.Services
{
    public class Obd2ExtendedServiceTests
    {
        private static IObdService CreateSimObdService()
        {
            var svc = new ObdService();
            svc.IsSimulationMode = true;
            _ = svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation }).Result;
            return svc;
        }

        [Fact]
        public async Task ReadReadiness_ReturnsReport()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var report = await svc.ReadReadinessAsync();
            Assert.NotNull(report);
            Assert.NotEmpty(report.Monitors);
        }

        [Fact]
        public async Task ReadReadiness_Has11Monitors()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var report = await svc.ReadReadinessAsync();
            Assert.Equal(11, report.Monitors.Count);
        }

        [Fact]
        public async Task ReadReadiness_MonitorNamesAreSet()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var report = await svc.ReadReadinessAsync();
            Assert.All(report.Monitors, m => Assert.False(string.IsNullOrEmpty(m.Name)));
        }

        [Fact]
        public async Task ReadReadiness_ContainsCatalystMonitor()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var report = await svc.ReadReadinessAsync();
            Assert.Contains(report.Monitors, m => m.Name == "Catalyst");
        }

        [Fact]
        public async Task ReadReadiness_OverallStatusNotEmpty()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var report = await svc.ReadReadinessAsync();
            Assert.False(string.IsNullOrEmpty(report.OverallStatus));
        }

        [Fact]
        public async Task ReadFreezeFrames_ReturnsList()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var frames = await svc.ReadFreezeFramesAsync();
            Assert.NotNull(frames);
        }

        [Fact]
        public async Task ReadFreezeFrames_HasParameters()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var frames = await svc.ReadFreezeFramesAsync();
            Assert.All(frames, f =>
            {
                Assert.NotEmpty(f.Parameters);
                Assert.All(f.Parameters, p => Assert.False(string.IsNullOrEmpty(p.Name)));
            });
        }

        [Fact]
        public async Task ReadOnboardTests_ReturnsList()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var tests = await svc.ReadOnboardTestsAsync();
            Assert.NotNull(tests);
            Assert.NotEmpty(tests);
        }

        [Fact]
        public async Task ReadOnboardTests_AllHaveNames()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var tests = await svc.ReadOnboardTestsAsync();
            Assert.All(tests, t => Assert.False(string.IsNullOrEmpty(t.Name)));
        }

        [Fact]
        public async Task ReadPendingDtcs_ReturnsList()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var dtcs = await svc.ReadPendingDtcsAsync();
            Assert.NotNull(dtcs);
        }

        [Fact]
        public async Task ReadVehicleInformation_ReturnsDictionary()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var info = await svc.ReadVehicleInformationAsync();
            Assert.NotNull(info);
            Assert.NotEmpty(info);
        }

        [Fact]
        public async Task ReadVehicleInformation_ContainsVin()
        {
            var svc = new Obd2ExtendedService(CreateSimObdService());
            var info = await svc.ReadVehicleInformationAsync();
            Assert.True(info.Keys.Any(k => k.Contains("VIN")));
        }
    }

    public class SpecialFunctionsServiceTests
    {
        private static IObdService CreateSimObdService()
        {
            var svc = new ObdService();
            svc.IsSimulationMode = true;
            _ = svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation }).Result;
            return svc;
        }

        [Fact]
        public void GetAllFunctions_ReturnsNonEmpty()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.NotEmpty(functions);
        }

        [Fact]
        public void GetAllFunctions_AllHaveNames()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.All(functions, f => Assert.False(string.IsNullOrEmpty(f.Name)));
        }

        [Fact]
        public void GetAllFunctions_AllHaveDescriptions()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.All(functions, f => Assert.False(string.IsNullOrEmpty(f.Description)));
        }

        [Fact]
        public void GetAllFunctions_ContainsEpb()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.Contains(functions, f => f.Name.Contains("EPB"));
        }

        [Fact]
        public void GetAllFunctions_ContainsDpf()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.Contains(functions, f => f.Name.Contains("DPF"));
        }

        [Fact]
        public void GetAllFunctions_ContainsBatteryRegistration()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.Contains(functions, f => f.Name.Contains("Battery Registration"));
        }

        [Fact]
        public void GetAllFunctions_ContainsTpms()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            Assert.Contains(functions, f => f.Name.Contains("TPMS"));
        }

        [Fact]
        public void GetAllFunctions_CategoriesAllValid()
        {
            var functions = SpecialFunctionsService.GetAllFunctions();
            var validCategories = Enum.GetValues<SpecialFunctionCategory>();
            Assert.All(functions, f => Assert.Contains(f.Category, validCategories));
        }

        [Fact]
        public async Task ExecuteFunction_Simulation_ReturnsResult()
        {
            var svc = new SpecialFunctionsService(CreateSimObdService());
            var func = SpecialFunctionsService.GetAllFunctions()[0];
            var result = await svc.ExecuteFunctionAsync(func);
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public async Task RunBatteryTest_Simulation_ReturnsResult()
        {
            var svc = new SpecialFunctionsService(CreateSimObdService());
            var result = await svc.RunBatteryTestAsync();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.ResultText));
            Assert.True(result.Voltage > 0);
            Assert.True(result.StateOfHealth >= 0 && result.StateOfHealth <= 100);
        }
    }

    public class VinDecoderServiceTests
    {
        [Fact]
        public void Decode_KnownVw_IsValid()
        {
            // VW Golf VIN pattern
            var r = VinDecoderService.Decode("WVWZZZ1KZAM149698");
            Assert.True(r.IsValid || r.Error.Contains("check digit"));
            Assert.Equal("WVW", r.Wmi);
        }

        [Fact]
        public void Decode_ShortVin_IsInvalid()
        {
            var r = VinDecoderService.Decode("SHORT");
            Assert.False(r.IsValid);
            Assert.False(string.IsNullOrEmpty(r.Error));
        }

        [Fact]
        public void Decode_ExtractsWmi()
        {
            var r = VinDecoderService.Decode("WBA3A9C50DF472124");
            Assert.Equal("WBA", r.Wmi);
        }

        [Fact]
        public void Decode_ModelYear_NotEmpty()
        {
            var r = VinDecoderService.Decode("WBA3A9C50DF472124");
            Assert.False(string.IsNullOrEmpty(r.ModelYear));
        }

        [Fact]
        public void Decode_SerialNumber_Length6()
        {
            var r = VinDecoderService.Decode("WBA3A9C50DF472124");
            Assert.Equal(6, r.SerialNumber.Length);
        }

        [Fact]
        public void Decode_ToDisplayList_Contains10Entries()
        {
            var r = VinDecoderService.Decode("WBA3A9C50DF472124");
            var list = r.ToDisplayList();
            Assert.True(list.Count >= 8);
        }

        [Fact]
        public void Decode_BmwWmi_IdentifiesBmw()
        {
            var r = VinDecoderService.Decode("WBA3A9C50DF472124");
            Assert.Contains("BMW", r.Manufacturer, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Decode_ToyotaWmi_IdentifiesToyota()
        {
            var r = VinDecoderService.Decode("JT2BF22K1Y0301234");
            Assert.Contains("Toyota", r.Manufacturer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class DataLogSessionTests
    {
        [Fact]
        public void ToCsv_GeneratesHeaderRow()
        {
            var session = new DataLogSession
            {
                ParameterNames = new List<string> { "RPM", "Speed", "Temp" },
                Entries = new List<DataLogEntry>
                {
                    new DataLogEntry
                    {
                        Timestamp = new DateTime(2025, 1, 1, 12, 0, 0),
                        ElapsedSeconds = 1.0,
                        Values = new List<(string, double, string)>
                        {
                            ("RPM", 1500, "rpm"),
                            ("Speed", 60, "km/h"),
                            ("Temp", 90, "°C")
                        }
                    }
                }
            };
            var csv = session.ToCsv();
            Assert.Contains("Timestamp", csv);
            Assert.Contains("RPM", csv);
            Assert.Contains("Speed", csv);
            Assert.Contains("1500", csv);
        }

        [Fact]
        public void ToCsv_EmptySession_OnlyHeader()
        {
            var session = new DataLogSession { ParameterNames = new List<string> { "RPM" } };
            var csv = session.ToCsv();
            Assert.Contains("Timestamp", csv);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(1, lines.Length); // Only header
        }
    }

    public class ReadinessMonitorTests
    {
        [Fact]
        public void StatusText_Complete_ContainsCheckmark()
        {
            var m = new ReadinessMonitor { Status = MonitorStatus.Complete };
            Assert.Contains("✔", m.StatusText);
        }

        [Fact]
        public void StatusText_Incomplete_ContainsX()
        {
            var m = new ReadinessMonitor { Status = MonitorStatus.Incomplete };
            Assert.Contains("✖", m.StatusText);
        }

        [Fact]
        public void StatusColor_Complete_IsGreen()
        {
            var m = new ReadinessMonitor { Status = MonitorStatus.Complete };
            Assert.Equal("#4CAF50", m.StatusColor);
        }

        [Fact]
        public void StatusColor_Incomplete_IsRed()
        {
            var m = new ReadinessMonitor { Status = MonitorStatus.Incomplete };
            Assert.Equal("#FF5252", m.StatusColor);
        }

        [Fact]
        public void ReadinessReport_IsReadyForTest_AllComplete()
        {
            var report = new ReadinessReport
            {
                MilOn = false,
                Monitors = new List<ReadinessMonitor>
                {
                    new() { Name = "Catalyst", IsSupported = true, IsContinuous = false, Status = MonitorStatus.Complete },
                    new() { Name = "EVAP",     IsSupported = true, IsContinuous = false, Status = MonitorStatus.Complete },
                    new() { Name = "Misfire",  IsSupported = true, IsContinuous = true,  Status = MonitorStatus.Complete },
                }
            };
            Assert.True(report.IsReadyForTest);
        }

        [Fact]
        public void ReadinessReport_NotReady_WhenMilOn()
        {
            var report = new ReadinessReport
            {
                MilOn = true,
                Monitors = new List<ReadinessMonitor>
                {
                    new() { Name = "Catalyst", IsSupported = true, IsContinuous = false, Status = MonitorStatus.Complete },
                }
            };
            Assert.False(report.IsReadyForTest);
        }
    }
}
