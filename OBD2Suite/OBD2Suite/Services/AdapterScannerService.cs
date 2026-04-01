using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Orchestrates all <see cref="IAdapterScanner"/> implementations and
    /// aggregates their results into a single adapter list.
    /// </summary>
    public class AdapterScannerService
    {
        private readonly IReadOnlyList<IAdapterScanner> _scanners;

        /// <summary>
        /// Creates a service with the default scanners
        /// (Serial, Bluetooth, Network).
        /// </summary>
        public AdapterScannerService()
            : this(CreateDefaultScanners()) { }

        /// <summary>
        /// Creates a service with explicitly provided scanners.
        /// Useful for testing and dependency injection.
        /// </summary>
        public AdapterScannerService(IEnumerable<IAdapterScanner> scanners)
        {
            _scanners = scanners.ToList();
        }

        /// <summary>
        /// Runs all scanners in parallel and returns the aggregated results,
        /// preserving interface order (Serial, Bluetooth, Network).
        /// </summary>
        public async Task<IReadOnlyList<ObdAdapterInfo>> ScanAllAsync(
            CancellationToken cancellationToken = default)
        {
            var tasks   = _scanners.Select(s => s.ScanAsync(cancellationToken)).ToList();
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        /// <summary>
        /// Runs only the scanners that match the requested interface type.
        /// </summary>
        public async Task<IReadOnlyList<ObdAdapterInfo>> ScanAsync(
            AdapterInterface iface,
            CancellationToken cancellationToken = default)
        {
            var scanner = _scanners.FirstOrDefault(s => s.Interface == iface);
            if (scanner is null) return Array.Empty<ObdAdapterInfo>();
            return await scanner.ScanAsync(cancellationToken);
        }

        private static IEnumerable<IAdapterScanner> CreateDefaultScanners()
        {
            if (OperatingSystem.IsWindows())
            {
                yield return new SerialAdapterScanner();
                yield return new BluetoothAdapterScanner();
            }
            yield return new NetworkAdapterScanner();
        }
    }
}
