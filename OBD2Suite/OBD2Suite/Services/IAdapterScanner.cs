using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Scans for OBD adapters on a specific physical interface
    /// (Serial/USB, Bluetooth, or Network/WiFi).
    /// </summary>
    public interface IAdapterScanner
    {
        /// <summary>The interface this scanner handles.</summary>
        AdapterInterface Interface { get; }

        /// <summary>
        /// Performs a scan and returns every OBD adapter found on this interface.
        /// Implementations should not throw; they return an empty list on failure.
        /// </summary>
        Task<IReadOnlyList<ObdAdapterInfo>> ScanAsync(CancellationToken cancellationToken = default);
    }
}
