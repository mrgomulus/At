using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Provides automatic baud rate detection for serial OBD adapters.
    /// Tries recommended rates based on adapter brand, with fallback to common rates.
    /// </summary>
    public class AutoBaudDetector
    {
        private readonly IObdService _obdService;

        public AutoBaudDetector(IObdService obdService)
        {
            _obdService = obdService;
        }

        /// <summary>
        /// Attempts to detect the correct baud rate for a serial connection.
        /// Returns the detected baud rate and ELM version, or null if detection failed.
        /// </summary>
        public async Task<(int BaudRate, string ElmVersion)?> DetectBaudRateAsync(
            ObdConnection connection,
            ObdAdapterBrand brand = ObdAdapterBrand.Unknown,
            CancellationToken cancellationToken = default)
        {
            if (connection.Type != ConnectionType.Serial && connection.Type != ConnectionType.Bluetooth)
            {
                throw new InvalidOperationException("Auto-baud detection is only supported for Serial and Bluetooth connections");
            }

            // Get recommended baud rates for this adapter brand
            var ratesToTry = AdapterCapabilities.GetRecommendedBaudRates(brand);

            foreach (var baudRate in ratesToTry)
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                try
                {
                    // Create a test connection with this baud rate
                    var testConnection = new ObdConnection
                    {
                        Type = connection.Type,
                        PortName = connection.PortName,
                        BaudRate = baudRate,
                        BluetoothAddress = connection.BluetoothAddress,
                        Timeout = 2000 // Use shorter timeout for auto-detection
                    };

                    // Try to connect
                    var connected = await _obdService.ConnectAsync(testConnection);
                    if (!connected)
                        continue;

                    try
                    {
                        // Try to communicate with the adapter
                        var response = await _obdService.SendCommandAsync("ATI", 1000);
                        
                        if (!string.IsNullOrWhiteSpace(response) && !ElmProtocol.IsError(response))
                        {
                            // Successfully communicated! This is the right baud rate
                            var version = response.Trim();
                            await _obdService.DisconnectAsync();
                            return (baudRate, version);
                        }
                    }
                    catch
                    {
                        // Communication failed, try next rate
                    }
                    finally
                    {
                        await _obdService.DisconnectAsync();
                    }
                }
                catch
                {
                    // Connection failed, try next rate
                    continue;
                }
            }

            // No baud rate worked
            return null;
        }

        /// <summary>
        /// Simplified auto-detection that just returns the detected baud rate.
        /// </summary>
        public async Task<int?> DetectBaudRateSimpleAsync(
            string portName,
            ObdAdapterBrand brand = ObdAdapterBrand.Unknown,
            CancellationToken cancellationToken = default)
        {
            var connection = new ObdConnection
            {
                Type = ConnectionType.Serial,
                PortName = portName,
                BaudRate = SupportedBaudRates.Default
            };

            var result = await DetectBaudRateAsync(connection, brand, cancellationToken);
            return result?.BaudRate;
        }
    }
}
