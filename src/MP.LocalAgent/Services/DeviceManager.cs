using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MP.LocalAgent.Configuration;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Contracts.Enums;
using MP.LocalAgent.Exceptions;
using MP.LocalAgent.Interfaces;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// Manages physical devices connected to the local agent
    /// </summary>
    public class DeviceManager : IDeviceManager
    {
        private readonly ILogger<DeviceManager> _logger;
        private readonly DevicesConfiguration _devicesConfig;
        private readonly ConcurrentDictionary<string, DeviceInfo> _devices;
        private readonly Dictionary<string, string> _primaryDevices;
        private readonly object _primaryDevicesLock = new();

        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public DeviceManager(
            ILogger<DeviceManager> logger,
            IOptions<DevicesConfiguration> devicesConfig)
        {
            _logger = logger;
            _devicesConfig = devicesConfig.Value;
            _devices = new ConcurrentDictionary<string, DeviceInfo>();
            _primaryDevices = new Dictionary<string, string>();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing device manager");

            try
            {
                // Initialize Terminal device
                if (_devicesConfig?.Terminal != null && _devicesConfig.Terminal.Enabled)
                {
                    await InitializeDeviceAsync("terminal-001", _devicesConfig.Terminal);
                    _primaryDevices["Terminal"] = "terminal-001";
                }

                // Initialize FiscalPrinter device
                if (_devicesConfig?.FiscalPrinter != null && _devicesConfig.FiscalPrinter.Enabled)
                {
                    await InitializeDeviceAsync("fiscal-printer-001", _devicesConfig.FiscalPrinter);
                    _primaryDevices["FiscalPrinter"] = "fiscal-printer-001";
                }

                _logger.LogInformation("Device manager initialized with {DeviceCount} devices", _devices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize device manager");
                throw new DeviceInitializationException("Device manager initialization failed", ex);
            }
        }

        public async Task<bool> InitializeDeviceAsync(string deviceId, DeviceConfiguration config)
        {
            _logger.LogInformation("Initializing device {DeviceId} with provider {ProviderId}",
                deviceId, config.ProviderId);

            try
            {
                var deviceInfo = new DeviceInfo
                {
                    DeviceId = deviceId,
                    DeviceType = GetDeviceTypeFromDeviceId(deviceId),
                    ProviderId = config.ProviderId,
                    Model = $"{GetDeviceTypeFromDeviceId(deviceId)} - {config.ProviderId}",
                    SerialNumber = $"{config.ProviderId.ToUpper()}-{deviceId}",
                    ConnectionType = config.ConnectionType,
                    ConnectionDetails = config.ConnectionDetails,
                    Status = DeviceStatus.Ready,
                    LastStatusUpdate = DateTime.UtcNow,
                    IsEnabled = config.Enabled,
                    IsPrimary = false,
                    ProviderData = config.Settings ?? new Dictionary<string, object>()
                };

                _devices.TryAdd(deviceId, deviceInfo);
                _logger.LogInformation("Device {DeviceId} initialized successfully", deviceId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize device {DeviceId}", deviceId);
                return false;
            }
        }

        public async Task<DeviceStatus> GetDeviceStatusAsync(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                return device.Status;
            }

            _logger.LogWarning("Device {DeviceId} not found", deviceId);
            return DeviceStatus.Offline;
        }

        public async Task<List<DeviceInfo>> GetAllDevicesAsync()
        {
            return _devices.Values.ToList();
        }

        public async Task<bool> SetDevicePrimaryAsync(string deviceType, string deviceId)
        {
            _logger.LogInformation("Setting device {DeviceId} as primary for type {DeviceType}",
                deviceId, deviceType);

            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                {
                    _logger.LogWarning("Device {DeviceId} not found", deviceId);
                    return false;
                }

                if (device.DeviceType != deviceType)
                {
                    _logger.LogWarning("Device {DeviceId} is not of type {DeviceType}", deviceId, deviceType);
                    return false;
                }

                lock (_primaryDevicesLock)
                {
                    // Clear previous primary for this type
                    if (_primaryDevices.TryGetValue(deviceType, out var oldPrimaryId))
                    {
                        if (_devices.TryGetValue(oldPrimaryId, out var oldDevice))
                        {
                            oldDevice.IsPrimary = false;
                        }
                    }

                    // Set new primary
                    device.IsPrimary = true;
                    _primaryDevices[deviceType] = deviceId;
                }

                _logger.LogInformation("Device {DeviceId} is now primary for type {DeviceType}",
                    deviceId, deviceType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary device {DeviceId} for type {DeviceType}",
                    deviceId, deviceType);
                return false;
            }
        }

        public async Task ReportDeviceStatusAsync(string deviceId, DeviceStatus status, string? details = null)
        {
            _logger.LogInformation("Reporting device {DeviceId} status change to {Status}", deviceId, status);

            try
            {
                if (_devices.TryGetValue(deviceId, out var device))
                {
                    var previousStatus = device.Status;
                    device.Status = status;
                    device.LastStatusUpdate = DateTime.UtcNow;

                    // Fire event
                    DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                    {
                        DeviceId = deviceId,
                        PreviousStatus = previousStatus,
                        CurrentStatus = status,
                        Details = details,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Device {DeviceId} status changed from {PreviousStatus} to {CurrentStatus}",
                        deviceId, previousStatus, status);
                }
                else
                {
                    _logger.LogWarning("Device {DeviceId} not found for status report", deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting device status for {DeviceId}", deviceId);
            }
        }

        public async Task<DeviceInfo?> GetPrimaryDeviceAsync(string deviceType)
        {
            lock (_primaryDevicesLock)
            {
                if (_primaryDevices.TryGetValue(deviceType, out var primaryDeviceId))
                {
                    _devices.TryGetValue(primaryDeviceId, out var device);
                    return device;
                }
            }

            _logger.LogWarning("No primary device found for type {DeviceType}", deviceType);
            return null;
        }

        public async Task<bool> IsDeviceTypeAvailableAsync(string deviceType)
        {
            var availableDevice = _devices.Values
                .FirstOrDefault(d => d.DeviceType == deviceType &&
                                    d.IsEnabled &&
                                    (d.Status == DeviceStatus.Ready || d.Status == DeviceStatus.Online));

            return availableDevice != null;
        }

        public async Task<DeviceInfo?> GetDeviceAsync(string deviceId)
        {
            _devices.TryGetValue(deviceId, out var device);
            return device;
        }

        public async Task RefreshAllDeviceStatusesAsync()
        {
            _logger.LogInformation("Refreshing status for all devices");

            try
            {
                foreach (var device in _devices.Values)
                {
                    // In a real implementation, this would check actual device status
                    // For now, just log
                    _logger.LogDebug("Device {DeviceId} status: {Status}", device.DeviceId, device.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing device statuses");
            }
        }

        private string GetDeviceTypeFromDeviceId(string deviceId)
        {
            // Detect device type from device ID
            if (deviceId.Contains("fiscal", StringComparison.OrdinalIgnoreCase))
                return "FiscalPrinter";

            if (deviceId.Contains("terminal", StringComparison.OrdinalIgnoreCase))
                return "Terminal";

            return "Terminal"; // Default
        }
    }
}
