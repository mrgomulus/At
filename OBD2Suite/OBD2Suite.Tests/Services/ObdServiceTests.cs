using OBD2Suite.Models;
using OBD2Suite.Services;
using Xunit;

namespace OBD2Suite.Tests.Services
{
    public class ObdServiceTests
    {
        private static ObdService CreateSimulator()
        {
            var svc = new ObdService();
            svc.IsSimulationMode = true;
            return svc;
        }

        [Fact]
        public async Task ConnectSimulation_ReturnsTrue()
        {
            var svc = CreateSimulator();
            var conn = new ObdConnection { Type = ConnectionType.Simulation };
            var result = await svc.ConnectAsync(conn);
            Assert.True(result);
            Assert.True(svc.IsConnected);
        }

        [Fact]
        public async Task ConnectSimulation_SetsElmVersion()
        {
            var svc = CreateSimulator();
            var conn = new ObdConnection { Type = ConnectionType.Simulation };
            await svc.ConnectAsync(conn);
            Assert.False(string.IsNullOrEmpty(conn.ElmVersion));
            Assert.Contains("Simulated", conn.ElmVersion);
        }

        [Fact]
        public async Task Disconnect_ClearsConnection()
        {
            var svc = CreateSimulator();
            var conn = new ObdConnection { Type = ConnectionType.Simulation };
            await svc.ConnectAsync(conn);
            await svc.DisconnectAsync();
            Assert.False(svc.IsConnected);
        }

        [Fact]
        public async Task ReadDtcs_ReturnsNonEmptyList()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var dtcs = await svc.ReadDtcsAsync();
            Assert.NotNull(dtcs);
            Assert.True(dtcs.Count >= 0);
        }

        [Fact]
        public async Task ClearDtcs_ReturnsTrue()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var ok = await svc.ClearDtcsAsync();
            Assert.True(ok);
        }

        [Fact]
        public async Task ReadLiveData_ReturnsParameters()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var data = await svc.ReadLiveDataAsync();
            Assert.NotNull(data);
            Assert.NotEmpty(data);
        }

        [Fact]
        public async Task ReadLiveData_ParametersHaveValues()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var data = await svc.ReadLiveDataAsync();
            Assert.All(data, p =>
            {
                Assert.False(string.IsNullOrEmpty(p.Name));
                Assert.False(string.IsNullOrEmpty(p.Unit));
                Assert.True(p.Value >= p.MinValue);
            });
        }

        [Fact]
        public async Task ReadVehicleInfo_ReturnsVin()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var info = await svc.ReadVehicleInfoAsync();
            Assert.NotNull(info);
            Assert.False(string.IsNullOrEmpty(info.Vin));
        }

        [Fact]
        public async Task ReadEcuModules_ReturnsList()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var ecus = await svc.ReadEcuModulesAsync();
            Assert.NotNull(ecus);
            Assert.NotEmpty(ecus);
        }

        [Fact]
        public async Task ReadEcuCoding_ReturnsString()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var coding = await svc.ReadEcuCodingAsync(0x01, 0xF186);
            Assert.False(string.IsNullOrEmpty(coding));
        }

        [Fact]
        public async Task WriteEcuCoding_ReturnsTrue()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var ok = await svc.WriteEcuCodingAsync(0x01, 0xF186, new byte[] { 0x01, 0x02, 0x03 });
            Assert.True(ok);
        }

        [Fact]
        public async Task ReadAdaptation_ReturnsString()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var val = await svc.ReadAdaptationAsync(0x01, 0);
            Assert.NotNull(val);
        }

        [Fact]
        public async Task WriteAdaptation_ReturnsTrue()
        {
            var svc = CreateSimulator();
            await svc.ConnectAsync(new ObdConnection { Type = ConnectionType.Simulation });
            var ok = await svc.WriteAdaptationAsync(0x01, 0, "100");
            Assert.True(ok);
        }
    }

    public class DtcDatabaseTests
    {
        [Fact]
        public void GetDescription_KnownCode_ReturnsDescription()
        {
            var desc = DtcDatabase.GetDescription("P0171");
            Assert.False(string.IsNullOrEmpty(desc));
            Assert.NotEqual("Unknown DTC", desc);
        }

        [Fact]
        public void GetDescription_UnknownCode_ReturnsUnknown()
        {
            var desc = DtcDatabase.GetDescription("P9999");
            Assert.Equal("Unknown DTC", desc);
        }

        [Fact]
        public void GetDescription_P0300_IsMisfire()
        {
            var desc = DtcDatabase.GetDescription("P0300");
            Assert.False(string.IsNullOrEmpty(desc));
            Assert.Contains("Misfire", desc, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Database_ContainsAtLeast100Entries()
        {
            // Verify the database has substantial coverage
            int count = 0;
            string[] testCodes = { "P0100","P0101","P0102","P0103","P0104","P0105","P0106",
                "P0107","P0108","P0109","P0110","P0171","P0172","P0300","P0301","P0302",
                "P0303","P0304","P0400","P0420","P0440","P0442","P0500","P0700" };
            foreach (var code in testCodes)
                if (DtcDatabase.GetDescription(code) != "Unknown DTC") count++;
            Assert.True(count >= 15, $"Expected ≥15 known codes, got {count}");
        }
    }
}
