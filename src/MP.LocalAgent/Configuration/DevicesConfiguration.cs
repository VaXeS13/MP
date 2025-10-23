using System.Collections.Generic;

namespace MP.LocalAgent.Configuration
{
    /// <summary>
    /// Device configuration for the local agent
    /// </summary>
    public class DevicesConfiguration
    {
        public DeviceConfiguration Terminal { get; set; } = new();
        public DeviceConfiguration FiscalPrinter { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific device
    /// </summary>
    public class DeviceConfiguration
    {
        public string ProviderId { get; set; } = null!;
        public bool Enabled { get; set; }
        public string ConnectionType { get; set; } = null!; // USB, RS232, TCP, Mock
        public string ConnectionDetails { get; set; } = null!; // COM3, 192.168.1.100:9100, etc.
        public Dictionary<string, object> Settings { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool IsPrimary { get; set; } = true;
    }
}