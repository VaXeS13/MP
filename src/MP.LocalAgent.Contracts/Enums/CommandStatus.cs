namespace MP.LocalAgent.Contracts.Enums
{
    /// <summary>
    /// Status of a command in the processing queue
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>
        /// Command has been queued but not yet processed
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Command is currently being processed
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Command completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Command failed with an error
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Command timed out
        /// </summary>
        TimedOut = 4,

        /// <summary>
        /// Command was cancelled
        /// </summary>
        Cancelled = 5
    }

    /// <summary>
    /// Connection status of the local agent
    /// </summary>
    public enum AgentConnectionStatus
    {
        /// <summary>
        /// Agent is disconnected
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Agent is connecting
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// Agent is connected and ready
        /// </summary>
        Connected = 2,

        /// <summary>
        /// Agent connection failed
        /// </summary>
        ConnectionFailed = 3,

        /// <summary>
        /// Agent is reconnecting
        /// </summary>
        Reconnecting = 4
    }

    /// <summary>
    /// Device status types
    /// </summary>
    public enum DeviceStatus
    {
        /// <summary>
        /// Device is offline/disconnected
        /// </summary>
        Offline = 0,

        /// <summary>
        /// Device is online but not ready
        /// </summary>
        Online = 1,

        /// <summary>
        /// Device is ready for operations
        /// </summary>
        Ready = 2,

        /// <summary>
        /// Device is busy processing another operation
        /// </summary>
        Busy = 3,

        /// <summary>
        /// Device has an error
        /// </summary>
        Error = 4,

        /// <summary>
        /// Device is in maintenance mode
        /// </summary>
        Maintenance = 5
    }
}