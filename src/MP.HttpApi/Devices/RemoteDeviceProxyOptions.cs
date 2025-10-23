using System;

namespace MP.HttpApi.Devices
{
    /// <summary>
    /// Configuration options for IRemoteDeviceProxy
    /// </summary>
    public class RemoteDeviceProxyOptions
    {
        /// <summary>
        /// Timeout for remote device operations (default: 30 seconds)
        /// </summary>
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of retry attempts for failed commands (default: 3)
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts (default: 2 seconds)
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Enable offline queue for critical operations when agent is unavailable (default: true)
        /// </summary>
        public bool EnableOfflineQueue { get; set; } = true;

        /// <summary>
        /// Maximum number of commands to queue when agent is offline (default: 1000)
        /// </summary>
        public int MaxQueuedCommands { get; set; } = 1000;

        /// <summary>
        /// Circuit breaker failure threshold - number of failures to open the circuit (default: 5)
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Circuit breaker reset time in seconds - how long to keep circuit open (default: 60)
        /// </summary>
        public int CircuitBreakerResetTimeSeconds { get; set; } = 60;
    }
}
