using OBD2Suite.Models;
using OBD2Suite.Services;
using Xunit;

namespace OBD2Suite.Tests.Services
{
    public class ManufacturerServiceTests
    {
        private static IObdService CreateSimObdService()
        {
            var svc = new ObdService();
            svc.IsSimulationMode = true;
            return svc;
        }

        [Theory]
        [InlineData(Manufacturer.Volkswagen)]
        [InlineData(Manufacturer.Audi)]
        [InlineData(Manufacturer.Skoda)]
        [InlineData(Manufacturer.Seat)]
        public void VagFactory_ReturnsVagService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<VagService>(svc);
            Assert.Equal(m, svc.Manufacturer);
        }

        [Theory]
        [InlineData(Manufacturer.BMW)]
        [InlineData(Manufacturer.Mini)]
        public void BmwFactory_ReturnsBmwService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<BmwService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Mercedes)]
        [InlineData(Manufacturer.Smart)]
        public void MercedesFactory_ReturnsMercedesService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<MercedesService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Ford)]
        [InlineData(Manufacturer.Lincoln)]
        public void FordFactory_ReturnsFordService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<FordService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Toyota)]
        [InlineData(Manufacturer.Lexus)]
        public void ToyotaFactory_ReturnsToyotaService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<ToyotaService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Opel)]
        [InlineData(Manufacturer.Chevrolet)]
        [InlineData(Manufacturer.Cadillac)]
        public void GmFactory_ReturnsGmService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<GmService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Peugeot)]
        [InlineData(Manufacturer.Citroen)]
        public void PsaFactory_ReturnsPsaService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<PsaService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Fiat)]
        [InlineData(Manufacturer.AlfaRomeo)]
        [InlineData(Manufacturer.Jeep)]
        public void FiatFactory_ReturnsFiatService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<FiatService>(svc);
        }

        [Theory]
        [InlineData(Manufacturer.Renault)]
        [InlineData(Manufacturer.Dacia)]
        public void RenaultFactory_ReturnsRenaultService(Manufacturer m)
        {
            var svc = ManufacturerServiceFactory.Create(m, CreateSimObdService());
            Assert.IsType<RenaultService>(svc);
        }

        [Fact]
        public async Task VagService_ScanAllEcus_ReturnsModules()
        {
            var svc = new VagService(CreateSimObdService());
            var ecus = await svc.ScanAllEcusAsync();
            Assert.NotEmpty(ecus);
            Assert.All(ecus, e => Assert.False(string.IsNullOrEmpty(e.Name)));
        }

        [Fact]
        public async Task VagService_ReadMeasuringBlock_ReturnsBlock()
        {
            var svc = new VagService(CreateSimObdService());
            var blocks = await svc.ReadMeasuringBlockAsync(0x01, 1);
            Assert.NotEmpty(blocks);
            Assert.NotEmpty(blocks[0].Fields);
        }

        [Fact]
        public async Task VagService_GetActuatorTests_ReturnsTests()
        {
            var svc = new VagService(CreateSimObdService());
            var tests = await svc.GetActuatorTestsAsync(0x01);
            Assert.NotEmpty(tests);
        }

        [Fact]
        public async Task BmwService_ScanAllEcus_ReturnsModules()
        {
            var svc = new BmwService(CreateSimObdService());
            var ecus = await svc.ScanAllEcusAsync();
            Assert.NotEmpty(ecus);
        }

        [Fact]
        public async Task BmwService_ReadCbs_ReturnsDictionary()
        {
            var svc = new BmwService(CreateSimObdService());
            var cbs = await svc.ReadCbsAsync();
            Assert.NotEmpty(cbs);
        }

        [Fact]
        public async Task ToyotaService_ReadEnhancedPids_ReturnsDictionary()
        {
            var svc = new ToyotaService(CreateSimObdService());
            var pids = await svc.ReadEnhancedPidsAsync();
            Assert.NotEmpty(pids);
        }

        [Fact]
        public async Task FordService_ReadEnhancedPids_ReturnsDictionary()
        {
            var svc = new FordService(CreateSimObdService());
            var pids = await svc.ReadEnhancedPidsAsync();
            Assert.NotEmpty(pids);
        }

        [Fact]
        public async Task GmService_ReadEnhancedPids_ReturnsDictionary()
        {
            var svc = new GmService(CreateSimObdService());
            var pids = await svc.ReadEnhancedPidsAsync();
            Assert.NotEmpty(pids);
        }

        [Fact]
        public async Task AnyService_ResetServiceInterval_ReturnsTrue()
        {
            var svc = new VagService(CreateSimObdService());
            var ok = await svc.ResetServiceIntervalAsync(0x17);
            Assert.True(ok);
        }

        [Fact]
        public async Task AnyService_ReadCoding_ReturnsString()
        {
            var svc = new VagService(CreateSimObdService());
            var coding = await svc.ReadCodingAsync(0x01);
            Assert.False(string.IsNullOrEmpty(coding));
        }

        [Fact]
        public async Task AnyService_ReadLongCoding_ReturnsBytes()
        {
            var svc = new VagService(CreateSimObdService());
            var bytes = await svc.ReadLongCodingAsync(0x01);
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public async Task AnyService_ReadEcuIdentification_ReturnsDictionary()
        {
            var svc = new BmwService(CreateSimObdService());
            var ident = await svc.ReadEcuIdentificationAsync(0x12);
            Assert.NotEmpty(ident);
            Assert.True(ident.ContainsKey("ECU Name"));
        }

        [Fact]
        public void ManufacturerGroups_ContainsAllExpectedGroups()
        {
            var groups = ManufacturerServiceFactory.GetManufacturerGroups().ToList();
            var groupNames = groups.Select(g => g.Group).ToList();
            Assert.Contains("VAG Group", groupNames);
            Assert.Contains("BMW Group", groupNames);
            Assert.Contains("Mercedes Group", groupNames);
            Assert.Contains("Ford Group", groupNames);
            Assert.Contains("Toyota Group", groupNames);
            Assert.Contains("GM Group", groupNames);
        }

        [Fact]
        public void ManufacturerHelper_GetGroupName_WorksForAllManufacturers()
        {
            foreach (Manufacturer m in Enum.GetValues<Manufacturer>())
            {
                var group = ManufacturerHelper.GetGroupName(m);
                Assert.False(string.IsNullOrEmpty(group));
            }
        }
    }
}
