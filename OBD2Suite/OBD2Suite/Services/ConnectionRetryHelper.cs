using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Provides connection retry logic with exponential backoff for OBD adapters.
    /// Helps handle intermittent connection failures and improves reliability.
    /// </summary>
    public class ConnectionRetryHelper
    {
        /// <summary>
        /// Retry policy configuration.
        /// </summary>
        public class RetryPolicy
        {
            /// <summary>Maximum number of retry attempts (default: 3).</summary>
            public int MaxRetries { get; set; } = 3;

            /// <summary>Initial delay in milliseconds before first retry (default: 500ms).</summary>
            public int InitialDelayMs { get; set; } = 500;

            /// <summary>Multiplier for exponential backoff (default: 2.0).</summary>
            public double BackoffMultiplier { get; set; } = 2.0;

            /// <summary>Maximum delay between retries in milliseconds (default: 5000ms).</summary>
            public int MaxDelayMs { get; set; } = 5000;

            /// <summary>Whether to try auto-baud detection on failure (default: false).</summary>
            public bool EnableAutoBaud { get; set; } = false;
        }

        private readonly IObdService _obdService;
        private readonly AutoBaudDetector? _autoBaudDetector;

        public ConnectionRetryHelper(IObdService obdService, AutoBaudDetector? autoBaudDetector = null)
        {
            _obdService = obdService;
            _autoBaudDetector = autoBaudDetector;
        }

        /// <summary>
        /// Attempts to connect to an OBD adapter with retry logic.
        /// Returns true if connection succeeded, false otherwise.
        /// </summary>
        public async Task<bool> ConnectWithRetryAsync(
            ObdConnection connection,
            RetryPolicy? policy = null,
            Action<int, Exception>? onRetry = null,
            CancellationToken cancellationToken = default)
        {
            policy ??= new RetryPolicy();
            int attempt = 0;
            int delayMs = policy.InitialDelayMs;

            while (attempt <= policy.MaxRetries)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                try
                {
                    attempt++;

                    // First attempt or retry without auto-baud
                    if (attempt == 1 || !policy.EnableAutoBaud || _autoBaudDetector == null)
                    {
                        var result = await _obdService.ConnectAsync(connection);
                        if (result)
                            return true;
                    }
                    else if (policy.EnableAutoBaud && _autoBaudDetector != null 
                             && (connection.Type == ConnectionType.Serial || connection.Type == ConnectionType.Bluetooth))
                    {
                        // Try auto-baud detection on retry for serial/bluetooth
                        var detected = await _autoBaudDetector.DetectBaudRateAsync(
                            connection, 
                            ObdAdapterBrand.Unknown, 
                            cancellationToken);

                        if (detected.HasValue)
                        {
                            connection.BaudRate = detected.Value.BaudRate;
                            connection.ElmVersion = detected.Value.ElmVersion;
                            
                            // Now connect with the detected baud rate
                            var result = await _obdService.ConnectAsync(connection);
                            if (result)
                                return true;
                        }
                    }

                    // Connection failed, prepare for retry
                    if (attempt > policy.MaxRetries)
                        return false;
                }
                catch (Exception ex)
                {
                    if (attempt > policy.MaxRetries)
                        throw;

                    // Notify about retry
                    onRetry?.Invoke(attempt, ex);
                }

                // Wait before retry with exponential backoff
                if (attempt <= policy.MaxRetries)
                {
                    await Task.Delay(Math.Min(delayMs, policy.MaxDelayMs), cancellationToken);
                    delayMs = (int)(delayMs * policy.BackoffMultiplier);
                }
            }

            return false;
        }

        /// <summary>
        /// Executes an OBD command with retry logic.
        /// Useful for unreliable connections or adapters.
        /// </summary>
        public async Task<string?> SendCommandWithRetryAsync(
            string command,
            int maxRetries = 2,
            int timeoutMs = 2000,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                try
                {
                    attempt++;
                    var response = await _obdService.SendCommandAsync(command, timeoutMs);
                    
                    // Check if response is valid
                    if (!string.IsNullOrWhiteSpace(response) && !ElmProtocol.IsError(response))
                        return response;

                    // Error response, retry
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt > maxRetries)
                        throw;
                }

                // Small delay before retry
                if (attempt <= maxRetries)
                    await Task.Delay(100, cancellationToken);
            }

            // All retries failed
            if (lastException != null)
                throw lastException;

            return null;
        }

        /// <summary>
        /// Gets a default retry policy based on adapter brand characteristics.
        /// </summary>
        public static RetryPolicy GetDefaultPolicy(ObdAdapterBrand brand)
        {
            return brand switch
            {
                // Professional adapters - fewer retries needed
                ObdAdapterBrand.Bosch => new RetryPolicy
                {
                    MaxRetries = 2,
                    InitialDelayMs = 300,
                    EnableAutoBaud = false
                },
                ObdAdapterBrand.Launch => new RetryPolicy
                {
                    MaxRetries = 2,
                    InitialDelayMs = 400,
                    EnableAutoBaud = false
                },
                ObdAdapterBrand.Autel => new RetryPolicy
                {
                    MaxRetries = 2,
                    InitialDelayMs = 400,
                    EnableAutoBaud = false
                },
                ObdAdapterBrand.OBDLink => new RetryPolicy
                {
                    MaxRetries = 2,
                    InitialDelayMs = 300,
                    EnableAutoBaud = false
                },

                // Mid-range adapters
                ObdAdapterBrand.WOW => new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelayMs = 500,
                    EnableAutoBaud = true
                },
                ObdAdapterBrand.BlueDriver => new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelayMs = 400,
                    EnableAutoBaud = false
                },

                // Budget adapters - more retries and auto-baud
                ObdAdapterBrand.ELM327 => new RetryPolicy
                {
                    MaxRetries = 4,
                    InitialDelayMs = 800,
                    EnableAutoBaud = true
                },
                ObdAdapterBrand.BAFX => new RetryPolicy
                {
                    MaxRetries = 4,
                    InitialDelayMs = 700,
                    EnableAutoBaud = true
                },
                ObdAdapterBrand.Vgate => new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelayMs = 600,
                    EnableAutoBaud = true
                },
                ObdAdapterBrand.iCar => new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelayMs = 600,
                    EnableAutoBaud = true
                },

                // Unknown - conservative policy
                _ => new RetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelayMs = 500,
                    EnableAutoBaud = true
                }
            };
        }
    }
}
