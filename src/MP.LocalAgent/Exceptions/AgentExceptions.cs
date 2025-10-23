using System;

namespace MP.LocalAgent.Exceptions
{
    /// <summary>
    /// Base exception for agent-related errors
    /// </summary>
    public class AgentException : Exception
    {
        public AgentException(string message) : base(message) { }
        public AgentException(string message, Exception innerException) : base(message, innerException) { }
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Exception thrown when device communication fails
    /// </summary>
    public class DeviceCommunicationException : AgentException
    {
        public DeviceCommunicationException(string message) : base(message) { }
        public DeviceCommunicationException(string message, Exception innerException) : base(message, innerException) { }
        public string? DeviceId { get; set; }
        public string? DeviceType { get; set; }
    }

    /// <summary>
    /// Exception thrown when a command times out
    /// </summary>
    public class CommandTimeoutException : AgentException
    {
        public CommandTimeoutException(string message) : base(message) { }
        public CommandTimeoutException(string message, Exception innerException) : base(message, innerException) { }
        public Guid CommandId { get; set; }
        public TimeSpan Timeout { get; set; }
    }

    /// <summary>
    /// Exception thrown when device initialization fails
    /// </summary>
    public class DeviceInitializationException : AgentException
    {
        public DeviceInitializationException(string message) : base(message) { }
        public DeviceInitializationException(string message, Exception innerException) : base(message, innerException) { }
        public string? DeviceId { get; set; }
        public string? ProviderId { get; set; }
    }

    /// <summary>
    /// Exception thrown when SignalR connection fails
    /// </summary>
    public class SignalRConnectionException : AgentException
    {
        public SignalRConnectionException(string message) : base(message) { }
        public SignalRConnectionException(string message, Exception innerException) : base(message, innerException) { }
        public string? ServerUrl { get; set; }
        public int AttemptCount { get; set; }
    }

    /// <summary>
    /// Exception thrown when device configuration is invalid
    /// </summary>
    public class DeviceConfigurationException : AgentException
    {
        public DeviceConfigurationException(string message) : base(message) { }
        public DeviceConfigurationException(string message, Exception innerException) : base(message, innerException) { }
        public string? DeviceId { get; set; }
        public string? ConfigurationKey { get; set; }
    }

    /// <summary>
    /// Exception thrown when fiscal printer operation fails
    /// </summary>
    public class FiscalPrinterException : DeviceCommunicationException
    {
        public FiscalPrinterException(string message) : base(message) { }
        public FiscalPrinterException(string message, Exception innerException) : base(message, innerException) { }
        public string? FiscalErrorCode { get; set; }
        public bool IsFiscalMemoryError { get; set; }
    }

    /// <summary>
    /// Exception thrown when payment terminal operation fails
    /// </summary>
    public class PaymentTerminalException : DeviceCommunicationException
    {
        public PaymentTerminalException(string message) : base(message) { }
        public PaymentTerminalException(string message, Exception innerException) : base(message, innerException) { }
        public string? TransactionId { get; set; }
        public string? PaymentErrorCode { get; set; }
        public bool IsCardDeclined { get; set; }
    }

    /// <summary>
    /// Exception thrown when command queue operations fail
    /// </summary>
    public class CommandQueueException : AgentException
    {
        public CommandQueueException(string message) : base(message) { }
        public CommandQueueException(string message, Exception innerException) : base(message, innerException) { }
        public Guid CommandId { get; set; }
        public string? QueueOperation { get; set; }
    }

    /// <summary>
    /// Exception thrown when agent is in invalid state
    /// </summary>
    public class AgentStateException : AgentException
    {
        public AgentStateException(string message) : base(message) { }
        public AgentStateException(string message, Exception innerException) : base(message, innerException) { }
        public string ExpectedState { get; set; } = null!;
        public string ActualState { get; set; } = null!;
    }

    /// <summary>
    /// Exception thrown when agent initialization fails
    /// </summary>
    public class AgentInitializationException : AgentException
    {
        public AgentInitializationException(string message) : base(message) { }
        public AgentInitializationException(string message, Exception innerException) : base(message, innerException) { }
        public Guid? TenantId { get; set; }
        public string? AgentId { get; set; }
    }
}