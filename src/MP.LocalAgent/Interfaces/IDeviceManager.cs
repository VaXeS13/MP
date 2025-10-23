using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Contracts.Enums;
using MP.LocalAgent.Configuration;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Manages physical devices connected to the local agent
    /// </summary>
    public interface IDeviceManager
    {
        /// <summary>
        /// Initialize the device manager
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Initialize a specific device with configuration
        /// </summary>
        Task<bool> InitializeDeviceAsync(string deviceId, DeviceConfiguration config);

        /// <summary>
        /// Get the status of a specific device
        /// </summary>
        Task<DeviceStatus> GetDeviceStatusAsync(string deviceId);

        /// <summary>
        /// Get all configured devices
        /// </summary>
        Task<List<DeviceInfo>> GetAllDevicesAsync();

        /// <summary>
        /// Set a device as primary for its type
        /// </summary>
        Task<bool> SetDevicePrimaryAsync(string deviceType, string deviceId);

        /// <summary>
        /// Report device status change
        /// </summary>
        Task ReportDeviceStatusAsync(string deviceId, DeviceStatus status, string? details = null);

        /// <summary>
        /// Get primary device for a specific type
        /// </summary>
        Task<DeviceInfo?> GetPrimaryDeviceAsync(string deviceType);

        /// <summary>
        /// Check if any devices of a type are available
        /// </summary>
        Task<bool> IsDeviceTypeAvailableAsync(string deviceType);

        /// <summary>
        /// Get device by ID
        /// </summary>
        Task<DeviceInfo?> GetDeviceAsync(string deviceId);

        /// <summary>
        /// Refresh device status for all devices
        /// </summary>
        Task RefreshAllDeviceStatusesAsync();

        /// <summary>
        /// Event fired when device status changes
        /// </summary>
        event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
    }

    /// <summary>
    /// Device status changed event arguments
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = null!;
        public DeviceStatus PreviousStatus { get; set; }
        public DeviceStatus CurrentStatus { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}