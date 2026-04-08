using OBD2Suite.Models;
using System.Text;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Provides diagnostic information and troubleshooting for OBD connection issues.
    /// Helps users understand why a connection might have failed and suggests solutions.
    /// </summary>
    public static class ConnectionDiagnostics
    {
        /// <summary>
        /// Represents a connection issue with description and suggested solutions.
        /// </summary>
        public class DiagnosticResult
        {
            public string Issue { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Suggestions { get; set; } = new();
            public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Warning;
        }

        public enum DiagnosticSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Analyzes a connection failure and provides diagnostic information.
        /// </summary>
        public static DiagnosticResult AnalyzeConnectionFailure(
            ObdConnection connection,
            Exception? exception = null,
            string? errorMessage = null)
        {
            var result = new DiagnosticResult();

            // Analyze based on connection type
            switch (connection.Type)
            {
                case ConnectionType.Serial:
                    AnalyzeSerialFailure(connection, exception, result);
                    break;

                case ConnectionType.Bluetooth:
                    AnalyzeBluetoothFailure(connection, exception, result);
                    break;

                case ConnectionType.WiFi:
                    AnalyzeWiFiFailure(connection, exception, result);
                    break;
            }

            // Add exception-specific analysis if available
            if (exception != null)
            {
                AnalyzeException(exception, result);
            }

            // Add error message analysis if available
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                AnalyzeErrorMessage(errorMessage, result);
            }

            return result;
        }

        private static void AnalyzeSerialFailure(ObdConnection connection, Exception? exception, DiagnosticResult result)
        {
            result.Issue = "Serial Connection Failed";
            result.Description = $"Unable to connect to OBD adapter on port {connection.PortName} at {connection.BaudRate} baud.";
            result.Severity = DiagnosticSeverity.Error;

            result.Suggestions.Add("Verify the adapter is properly connected to the USB port");
            result.Suggestions.Add("Check if the correct COM port is selected");
            result.Suggestions.Add("Try disconnecting and reconnecting the adapter");
            result.Suggestions.Add($"Try different baud rates: {string.Join(", ", SupportedBaudRates.AutoDetectSequence)}");
            result.Suggestions.Add("Ensure no other application is using the COM port");
            result.Suggestions.Add("Check Device Manager for driver issues");
            
            if (connection.BaudRate != SupportedBaudRates.Default)
            {
                result.Suggestions.Add($"Try the default baud rate ({SupportedBaudRates.Default})");
            }
        }

        private static void AnalyzeBluetoothFailure(ObdConnection connection, Exception? exception, DiagnosticResult result)
        {
            result.Issue = "Bluetooth Connection Failed";
            result.Description = "Unable to establish Bluetooth connection to OBD adapter.";
            result.Severity = DiagnosticSeverity.Error;

            result.Suggestions.Add("Verify the adapter is powered on and in pairing mode");
            result.Suggestions.Add("Check that Bluetooth is enabled on your computer");
            result.Suggestions.Add("Ensure the adapter is paired in Windows Bluetooth settings");
            result.Suggestions.Add("Try removing and re-pairing the Bluetooth device");
            result.Suggestions.Add("Move closer to the vehicle to improve signal strength");
            result.Suggestions.Add("Check if adapter PIN/password is correct (often 0000 or 1234)");
            
            if (!string.IsNullOrEmpty(connection.PortName))
            {
                result.Suggestions.Add($"Virtual COM port {connection.PortName} - ensure it's assigned correctly");
            }
        }

        private static void AnalyzeWiFiFailure(ObdConnection connection, Exception? exception, DiagnosticResult result)
        {
            result.Issue = "WiFi/Network Connection Failed";
            result.Description = $"Unable to connect to OBD adapter at {connection.IpAddress}:{connection.NetworkPort}.";
            result.Severity = DiagnosticSeverity.Error;

            result.Suggestions.Add("Verify your computer is connected to the adapter's WiFi network");
            result.Suggestions.Add($"Check that the IP address {connection.IpAddress} is correct");
            result.Suggestions.Add($"Verify the port number {connection.NetworkPort} is correct");
            result.Suggestions.Add("Ensure the adapter is powered on and WiFi is active");
            result.Suggestions.Add("Try pinging the adapter's IP address from command prompt");
            result.Suggestions.Add("Check Windows Firewall is not blocking the connection");
            result.Suggestions.Add("Try accessing the adapter's web interface (if available)");
            
            if (connection.IpAddress == "192.168.0.10")
            {
                result.Suggestions.Add("Common WiFi OBD adapters use this IP - make sure you're on the adapter's network");
            }
        }

        private static void AnalyzeException(Exception exception, DiagnosticResult result)
        {
            var exceptionType = exception.GetType().Name;
            
            if (exception.Message.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) ||
                exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase))
            {
                result.Suggestions.Insert(0, "Port is in use by another application - close any other OBD software");
                result.Severity = DiagnosticSeverity.Critical;
            }
            else if (exception.Message.Contains("not find", StringComparison.OrdinalIgnoreCase))
            {
                result.Suggestions.Insert(0, "Port not found - the adapter may have been disconnected");
                result.Severity = DiagnosticSeverity.Critical;
            }
            else if (exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                     exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            {
                result.Suggestions.Insert(0, "Connection timed out - adapter may be unresponsive or wrong settings");
                result.Suggestions.Add("Increase timeout value in connection settings");
            }
            else if (exceptionType.Contains("Socket", StringComparison.OrdinalIgnoreCase))
            {
                result.Suggestions.Insert(0, "Network socket error - check network connectivity");
            }
        }

        private static void AnalyzeErrorMessage(string errorMessage, DiagnosticResult result)
        {
            var upper = errorMessage.ToUpperInvariant();
            
            if (upper.Contains("NO DATA"))
            {
                result.Suggestions.Add("Vehicle may not be responding - ensure ignition is ON");
                result.Suggestions.Add("Try starting the engine");
                result.Suggestions.Add("Check adapter is properly plugged into OBD-II port");
            }
            else if (upper.Contains("BUS INIT"))
            {
                result.Suggestions.Add("Bus initialization failed - wrong protocol selected");
                result.Suggestions.Add("Try Auto protocol detection instead of manual selection");
            }
            else if (upper.Contains("CAN ERROR") || upper.Contains("FB ERROR"))
            {
                result.Suggestions.Add("CAN bus communication error - check adapter compatibility");
                result.Suggestions.Add("Try a different protocol setting");
            }
            else if (upper.Contains("UNABLE TO CONNECT"))
            {
                result.Suggestions.Add("Adapter cannot communicate with vehicle ECU");
                result.Suggestions.Add("Verify vehicle is OBD-II compliant (1996+ in USA, 2001+ in EU)");
            }
        }

        /// <summary>
        /// Generates a user-friendly diagnostic report as formatted text.
        /// </summary>
        public static string GenerateDiagnosticReport(DiagnosticResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"⚠️ {result.Issue}");
            sb.AppendLine();
            sb.AppendLine(result.Description);
            sb.AppendLine();
            sb.AppendLine("Suggested solutions:");
            
            for (int i = 0; i < result.Suggestions.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {result.Suggestions[i]}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Performs a basic connection health check and returns diagnostic information.
        /// </summary>
        public static async Task<DiagnosticResult> PerformHealthCheckAsync(
            IObdService obdService,
            ObdConnection connection)
        {
            var result = new DiagnosticResult
            {
                Issue = "Connection Health Check",
                Severity = DiagnosticSeverity.Info
            };

            if (!obdService.IsConnected)
            {
                result.Description = "Not connected to any adapter";
                result.Severity = DiagnosticSeverity.Warning;
                result.Suggestions.Add("Connect to an adapter first");
                return result;
            }

            var tests = new List<string>();
            var failures = new List<string>();

            // Test 1: ATI - Get adapter version
            try
            {
                var version = await obdService.SendCommandAsync("ATI", 2000);
                if (!string.IsNullOrWhiteSpace(version) && !ElmProtocol.IsError(version))
                {
                    tests.Add($"✓ Adapter responds to ATI: {version.Trim()}");
                }
                else
                {
                    failures.Add("✗ Adapter version check failed");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"✗ ATI command error: {ex.Message}");
            }

            // Test 2: AT RV - Get voltage
            try
            {
                var voltage = await obdService.SendCommandAsync("AT RV", 2000);
                if (!string.IsNullOrWhiteSpace(voltage) && !ElmProtocol.IsError(voltage))
                {
                    tests.Add($"✓ Vehicle voltage: {voltage.Trim()}");
                }
                else
                {
                    failures.Add("✗ Voltage reading failed");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"✗ Voltage check error: {ex.Message}");
            }

            // Test 3: 0100 - Check supported PIDs
            try
            {
                var pids = await obdService.SendCommandAsync("0100", 3000);
                if (!string.IsNullOrWhiteSpace(pids) && !ElmProtocol.IsError(pids))
                {
                    tests.Add("✓ ECU responds to PID requests");
                }
                else
                {
                    failures.Add("✗ ECU not responding to PIDs");
                    result.Suggestions.Add("Vehicle may not be running or ignition is OFF");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"✗ PID check error: {ex.Message}");
            }

            // Compile results
            result.Description = $"Health check completed: {tests.Count} passed, {failures.Count} failed";
            
            if (failures.Count == 0)
            {
                result.Severity = DiagnosticSeverity.Info;
                result.Suggestions.Add("Connection is healthy and working properly");
            }
            else if (failures.Count < tests.Count)
            {
                result.Severity = DiagnosticSeverity.Warning;
                result.Suggestions.Add("Partial connectivity - some functions may not work");
            }
            else
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Suggestions.Add("Connection appears to be failing - see failures below");
            }

            result.Suggestions.AddRange(tests);
            result.Suggestions.AddRange(failures);

            return result;
        }

        /// <summary>
        /// Checks if a given adapter brand is likely compatible with a vehicle manufacturer.
        /// </summary>
        public static bool IsAdapterCompatibleWithManufacturer(
            ObdAdapterBrand adapter,
            Manufacturer manufacturer)
        {
            // Professional adapters support all manufacturers
            if (AdapterCapabilities.SupportsAdvancedDiagnostics(adapter))
                return true;

            // Some adapters are manufacturer-specific
            return adapter switch
            {
                ObdAdapterBrand.Carista => manufacturer is Manufacturer.Volkswagen or Manufacturer.Audi 
                    or Manufacturer.Toyota or Manufacturer.Lexus or Manufacturer.BMW,
                
                ObdAdapterBrand.Carly => manufacturer is Manufacturer.BMW or Manufacturer.Mercedes 
                    or Manufacturer.Volkswagen or Manufacturer.Audi or Manufacturer.Porsche,
                
                ObdAdapterBrand.OBDeleven => manufacturer is Manufacturer.Volkswagen or Manufacturer.Audi 
                    or Manufacturer.Skoda or Manufacturer.Seat,
                
                // Generic adapters support all with basic OBD2
                _ => true
            };
        }
    }
}
