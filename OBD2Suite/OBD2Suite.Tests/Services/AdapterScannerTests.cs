using OBD2Suite.Models;
using OBD2Suite.Services;
using Xunit;

namespace OBD2Suite.Tests.Services
{
    /// <summary>
    /// Tests for the OBD adapter scanner services.
    /// These tests are designed to run in simulation / mocked mode so that
    /// they work without physical Bluetooth or WiFi hardware.
    /// </summary>
    public class AdapterScannerTests
    {
        // ── SerialAdapterScanner ─────────────────────────────────────────────

        [Fact]
        public void SerialScanner_Interface_IsSerial()
        {
            var scanner = new SerialAdapterScanner();
            Assert.Equal(AdapterInterface.Serial, scanner.Interface);
        }

        [Fact]
        public async Task SerialScanner_Scan_ReturnsReadOnlyList()
        {
            var scanner = new SerialAdapterScanner();
            var result  = await scanner.ScanAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SerialScanner_Scan_AllItemsAreSerial()
        {
            var scanner = new SerialAdapterScanner();
            var result  = await scanner.ScanAsync();
            Assert.All(result, a => Assert.Equal(AdapterInterface.Serial, a.Interface));
        }

        [Fact]
        public async Task SerialScanner_Scan_AllItemsHaveNonEmptyName()
        {
            var scanner = new SerialAdapterScanner();
            var result  = await scanner.ScanAsync();
            Assert.All(result, a => Assert.False(string.IsNullOrWhiteSpace(a.Name)));
        }

        // ── NetworkAdapterScanner ────────────────────────────────────────────

        [Fact]
        public void NetworkScanner_Interface_IsNetwork()
        {
            var scanner = new NetworkAdapterScanner();
            Assert.Equal(AdapterInterface.Network, scanner.Interface);
        }

        [Fact]
        public async Task NetworkScanner_Scan_ReturnsReadOnlyList()
        {
            // Uses a very short timeout so the test completes quickly even with
            // no WiFi OBD adapter present.
            var scanner = new NetworkAdapterScanner(probeTimeoutMs: 50);
            var result  = await scanner.ScanAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NetworkScanner_Scan_CanBeCancelled()
        {
            var scanner = new NetworkAdapterScanner(probeTimeoutMs: 50);
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();   // cancel immediately
            // Should not throw — just return empty list
            var result = await scanner.ScanAsync(cts.Token);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NetworkScanner_Scan_AllItemsAreNetwork()
        {
            var scanner = new NetworkAdapterScanner(probeTimeoutMs: 50);
            var result  = await scanner.ScanAsync();
            Assert.All(result, a => Assert.Equal(AdapterInterface.Network, a.Interface));
        }

        [Fact]
        public async Task NetworkScanner_Scan_AllItemsHaveIpAddress()
        {
            var scanner = new NetworkAdapterScanner(probeTimeoutMs: 50);
            var result  = await scanner.ScanAsync();
            Assert.All(result, a => Assert.False(string.IsNullOrWhiteSpace(a.IpAddress)));
        }

        [Fact]
        public async Task NetworkScanner_Scan_AllItemsHavePositivePort()
        {
            var scanner = new NetworkAdapterScanner(probeTimeoutMs: 50);
            var result  = await scanner.ScanAsync();
            Assert.All(result, a => Assert.True(a.Port > 0));
        }

        // ── AdapterScannerService ────────────────────────────────────────────

        [Fact]
        public async Task ScannerService_ScanAll_ReturnsReadOnlyList()
        {
            // Use only the network scanner with a short timeout.
            var service = new AdapterScannerService(new IAdapterScanner[]
            {
                new NetworkAdapterScanner(probeTimeoutMs: 50)
            });
            var result = await service.ScanAllAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ScannerService_ScanByInterface_ReturnsCorrectInterface()
        {
            var service = new AdapterScannerService(new IAdapterScanner[]
            {
                new NetworkAdapterScanner(probeTimeoutMs: 50)
            });
            var result = await service.ScanAsync(AdapterInterface.Network);
            Assert.All(result, a => Assert.Equal(AdapterInterface.Network, a.Interface));
        }

        [Fact]
        public async Task ScannerService_ScanUnknownInterface_ReturnsEmpty()
        {
            var service = new AdapterScannerService(new IAdapterScanner[]
            {
                new NetworkAdapterScanner(probeTimeoutMs: 50)
            });
            // Request Bluetooth scanner which was not registered.
            var result = await service.ScanAsync(AdapterInterface.Bluetooth);
            Assert.Empty(result);
        }

        // ── ObdAdapterInfo ───────────────────────────────────────────────────

        [Fact]
        public void AdapterInfo_ToConnection_SerialMapsToSerialType()
        {
            var info = new ObdAdapterInfo
            {
                Interface = AdapterInterface.Serial,
                PortName  = "COM4",
                BaudRate  = 115200,
                Name      = "Test Adapter"
            };
            var conn = info.ToConnection();
            Assert.Equal(ConnectionType.Serial, conn.Type);
            Assert.Equal("COM4",    conn.PortName);
            Assert.Equal(115200,    conn.BaudRate);
            Assert.Equal("Test Adapter", conn.DeviceName);
        }

        [Fact]
        public void AdapterInfo_ToConnection_NetworkMapsToWiFiType()
        {
            var info = new ObdAdapterInfo
            {
                Interface = AdapterInterface.Network,
                IpAddress = "192.168.0.10",
                Port      = 35000,
                Name      = "WiFi OBD"
            };
            var conn = info.ToConnection();
            Assert.Equal(ConnectionType.WiFi, conn.Type);
            Assert.Equal("192.168.0.10", conn.IpAddress);
            Assert.Equal(35000,          conn.NetworkPort);
        }

        [Fact]
        public void AdapterInfo_ToConnection_BluetoothMapsToBluetoothType()
        {
            var info = new ObdAdapterInfo
            {
                Interface        = AdapterInterface.Bluetooth,
                BluetoothAddress = "AABBCCDDEEFF",
                PortName         = "COM5",
                Name             = "BT OBD"
            };
            var conn = info.ToConnection();
            Assert.Equal(ConnectionType.Bluetooth, conn.Type);
            Assert.Equal("AABBCCDDEEFF", conn.BluetoothAddress);
            Assert.Equal("COM5",         conn.PortName);
        }

        // ── ObdConnection – new fields ───────────────────────────────────────

        [Fact]
        public void ObdConnection_DefaultIpAddress_IsELM327Default()
        {
            var conn = new ObdConnection { Type = ConnectionType.WiFi };
            Assert.Equal("192.168.0.10", conn.IpAddress);
            Assert.Equal(35000,          conn.NetworkPort);
        }

        [Fact]
        public void ObdConnection_BluetoothAddress_DefaultEmpty()
        {
            var conn = new ObdConnection { Type = ConnectionType.Bluetooth };
            Assert.Equal("", conn.BluetoothAddress);
        }

        // ── Brand detection ──────────────────────────────────────────────────

        [Theory]
        [InlineData("OBDLink MX",  ObdAdapterBrand.OBDLink)]
        [InlineData("ELM327 USB",  ObdAdapterBrand.ELM327)]
        [InlineData("Veepeak BT",  ObdAdapterBrand.Veepeak)]
        [InlineData("Unknown Dev", ObdAdapterBrand.Unknown)]
        public void SerialScanner_DetectBrand_ReturnsExpected(string name, ObdAdapterBrand expected)
        {
            var brand = SerialAdapterScanner.DetectBrand(name);
            Assert.Equal(expected, brand);
        }
    }
}
