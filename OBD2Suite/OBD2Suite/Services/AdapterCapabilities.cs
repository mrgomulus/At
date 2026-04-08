using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Defines capabilities and characteristics of different OBD adapter brands.
    /// Used to optimize connection settings and features based on detected adapter.
    /// </summary>
    public static class AdapterCapabilities
    {
        /// <summary>
        /// Gets the recommended baud rates for a specific adapter brand.
        /// Returns rates in order of likelihood for auto-detection.
        /// </summary>
        public static int[] GetRecommendedBaudRates(ObdAdapterBrand brand)
        {
            return brand switch
            {
                // High-speed adapters
                ObdAdapterBrand.OBDLink => new[] { 115200, 500000, 230400, 38400 },
                ObdAdapterBrand.BlueDriver => new[] { 115200, 230400, 38400 },
                ObdAdapterBrand.Bosch => new[] { 500000, 230400, 115200, 38400 },
                ObdAdapterBrand.Launch => new[] { 115200, 230400, 38400 },
                ObdAdapterBrand.Autel => new[] { 115200, 230400, 38400 },
                ObdAdapterBrand.WOW => new[] { 38400, 115200, 230400 },
                
                // Standard speed adapters
                ObdAdapterBrand.Vgate => new[] { 38400, 115200 },
                ObdAdapterBrand.iCar => new[] { 38400, 115200 },
                ObdAdapterBrand.BAFX => new[] { 38400, 9600 },
                ObdAdapterBrand.Veepeak => new[] { 38400, 115200 },
                ObdAdapterBrand.KONNWEI => new[] { 38400, 115200 },
                ObdAdapterBrand.Foxwell => new[] { 115200, 38400 },
                ObdAdapterBrand.Ancel => new[] { 38400, 115200 },
                
                // Budget/generic adapters (try lower speeds first)
                ObdAdapterBrand.ELM327 => new[] { 38400, 9600, 115200 },
                ObdAdapterBrand.Unknown => SupportedBaudRates.AutoDetectSequence,
                
                // Default for others
                _ => new[] { 38400, 115200, 230400 }
            };
        }

        /// <summary>
        /// Indicates if the adapter supports advanced/extended diagnostic features.
        /// </summary>
        public static bool SupportsAdvancedDiagnostics(ObdAdapterBrand brand)
        {
            return brand switch
            {
                ObdAdapterBrand.Bosch => true,
                ObdAdapterBrand.Launch => true,
                ObdAdapterBrand.Autel => true,
                ObdAdapterBrand.WOW => true,
                ObdAdapterBrand.OBDLink => true,
                ObdAdapterBrand.BlueDriver => true,
                ObdAdapterBrand.Carista => true,
                ObdAdapterBrand.Carly => true,
                ObdAdapterBrand.OBDeleven => true,
                _ => false
            };
        }

        /// <summary>
        /// Indicates if the adapter is known to have good CAN bus support.
        /// </summary>
        public static bool HasReliableCanSupport(ObdAdapterBrand brand)
        {
            return brand switch
            {
                ObdAdapterBrand.OBDLink => true,
                ObdAdapterBrand.BlueDriver => true,
                ObdAdapterBrand.Bosch => true,
                ObdAdapterBrand.Launch => true,
                ObdAdapterBrand.Autel => true,
                ObdAdapterBrand.WOW => true,
                ObdAdapterBrand.Foxwell => true,
                ObdAdapterBrand.TOPDON => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the expected firmware/version identifier pattern for the adapter.
        /// Used to validate that we're talking to the right device.
        /// </summary>
        public static string? GetExpectedVersionPattern(ObdAdapterBrand brand)
        {
            return brand switch
            {
                ObdAdapterBrand.ELM327 => "ELM327",
                ObdAdapterBrand.OBDLink => "OBDLink",
                ObdAdapterBrand.WOW => "WOW",
                ObdAdapterBrand.Bosch => "BOSCH",
                _ => null
            };
        }

        /// <summary>
        /// Gets the recommended timeout in milliseconds for the adapter.
        /// Some adapters are slower to respond than others.
        /// </summary>
        public static int GetRecommendedTimeout(ObdAdapterBrand brand)
        {
            return brand switch
            {
                // Fast professional adapters
                ObdAdapterBrand.Bosch => 1000,
                ObdAdapterBrand.Launch => 1500,
                ObdAdapterBrand.Autel => 1500,
                ObdAdapterBrand.OBDLink => 1500,
                
                // Standard adapters
                ObdAdapterBrand.WOW => 3000,
                ObdAdapterBrand.BlueDriver => 2000,
                ObdAdapterBrand.Foxwell => 2000,
                
                // Budget adapters (often slower)
                ObdAdapterBrand.ELM327 => 5000,
                ObdAdapterBrand.BAFX => 4000,
                ObdAdapterBrand.Vgate => 3000,
                ObdAdapterBrand.iCar => 3000,
                
                // Unknown/default
                _ => 5000
            };
        }

        /// <summary>
        /// Indicates if the adapter typically requires special initialization.
        /// </summary>
        public static bool RequiresSpecialInit(ObdAdapterBrand brand)
        {
            return brand switch
            {
                ObdAdapterBrand.WOW => true,
                ObdAdapterBrand.Bosch => true,
                ObdAdapterBrand.Launch => true,
                ObdAdapterBrand.Autel => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a user-friendly description of the adapter capabilities.
        /// </summary>
        public static string GetCapabilityDescription(ObdAdapterBrand brand)
        {
            return brand switch
            {
                ObdAdapterBrand.WOW => "Professional diagnostic adapter with extended OEM functions",
                ObdAdapterBrand.Bosch => "Professional KTS diagnostic system with full vehicle coverage",
                ObdAdapterBrand.Launch => "Professional diagnostic tool with bi-directional controls",
                ObdAdapterBrand.Autel => "Professional scanner with advanced coding capabilities",
                ObdAdapterBrand.OBDLink => "High-speed professional adapter with extensive protocol support",
                ObdAdapterBrand.BlueDriver => "Professional-grade adapter with enhanced diagnostics",
                ObdAdapterBrand.Carista => "OEM-level diagnostics for VAG, Toyota, and other brands",
                ObdAdapterBrand.Carly => "Advanced coding and diagnostics for BMW, VAG, Mercedes",
                ObdAdapterBrand.OBDeleven => "VAG Group specialist with extensive coding features",
                ObdAdapterBrand.Foxwell => "Professional diagnostic scanner with special functions",
                ObdAdapterBrand.TOPDON => "Advanced diagnostic adapter with bi-directional testing",
                ObdAdapterBrand.UniCarScan => "Universal diagnostic adapter with OEM protocols",
                ObdAdapterBrand.KONNWEI => "Budget-friendly OBD2 scanner with basic diagnostics",
                ObdAdapterBrand.Ancel => "Entry-level diagnostic scanner with code reading",
                ObdAdapterBrand.ThinkDiag => "Smartphone-compatible diagnostic adapter",
                ObdAdapterBrand.Actron => "Reliable North American diagnostic tool",
                ObdAdapterBrand.Veepeak => "Compact Bluetooth adapter for basic diagnostics",
                ObdAdapterBrand.BAFX => "Popular budget Bluetooth OBD2 adapter",
                ObdAdapterBrand.Vgate => "WiFi/Bluetooth adapter with iCar app support",
                ObdAdapterBrand.iCar => "WiFi OBD2 adapter for smartphone diagnostics",
                ObdAdapterBrand.ELM327 => "Generic ELM327 chipset - basic OBD2 functionality",
                ObdAdapterBrand.Unknown => "Unknown adapter - basic OBD2 protocols supported",
                _ => "OBD2 diagnostic adapter"
            };
        }
    }
}
