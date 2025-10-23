using System;

namespace MP.LocalAgent.Configuration
{
    /// <summary>
    /// Configuration for the local agent
    /// </summary>
    public class LocalAgentConfiguration
    {
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public string ServerUrl { get; set; } = null!;
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public bool EnableDetailedLogging { get; set; } = true;
        public string? LogLevel { get; set; } = "Information";
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool AutoReconnect { get; set; } = true;
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(10);
        public int MaxReconnectAttempts { get; set; } = 10;
    }
}