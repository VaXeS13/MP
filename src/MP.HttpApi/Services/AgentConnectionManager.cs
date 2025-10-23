using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MP.HttpApi.Hubs;
using MP.LocalAgent.Contracts.Models;

namespace MP.Services
{
    /// <summary>
    /// Service for managing agent connections
    /// </summary>
    public class AgentConnectionManager : IAgentConnectionManager
    {
        private readonly ILogger<AgentConnectionManager> _logger;
        private readonly ConcurrentDictionary<string, AgentConnectionInfo> _connections;
        private readonly ConcurrentDictionary<(Guid TenantId, string AgentId), string> _connectionMapping;

        public AgentConnectionManager(ILogger<AgentConnectionManager> logger)
        {
            _logger = logger;
            _connections = new ConcurrentDictionary<string, AgentConnectionInfo>();
            _connectionMapping = new ConcurrentDictionary<(Guid, string), string>();
        }

        public async Task RegisterAgentAsync(Guid tenantId, string agentId, string connectionId, string? userId)
        {
            _logger.LogInformation("Registering agent {AgentId} for tenant {TenantId} with connection {ConnectionId}",
                agentId, tenantId, connectionId);

            try
            {
                // Remove any existing connection for this agent
                await UnregisterAgentAsync(tenantId, agentId, connectionId);

                var connectionInfo = new AgentConnectionInfo
                {
                    TenantId = tenantId,
                    AgentId = agentId,
                    ConnectionId = connectionId,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    DeviceInfo = new AgentDeviceInfo
                    {
                        TenantId = tenantId,
                        AgentId = agentId,
                        ConnectionStatus = MP.LocalAgent.Contracts.Enums.AgentConnectionStatus.Connected,
                        Devices = new List<DeviceInfo>()
                    }
                };

                // Add connection
                _connections.TryAdd(connectionId, connectionInfo);
                _connectionMapping.TryAdd((tenantId, agentId), connectionId);

                _logger.LogInformation("Agent {AgentId} registered successfully for tenant {TenantId}",
                    agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);
                throw;
            }
        }

        public async Task UnregisterAgentAsync(Guid tenantId, string agentId, string connectionId)
        {
            _logger.LogInformation("Unregistering agent {AgentId} for tenant {TenantId} with connection {ConnectionId}",
                agentId, tenantId, connectionId);

            try
            {
                // Remove connection
                _connections.TryRemove(connectionId, out _);
                _connectionMapping.TryRemove((tenantId, agentId), out _);

                _logger.LogInformation("Agent {AgentId} unregistered successfully for tenant {TenantId}",
                    agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);
            }
        }

        public async Task UpdateAgentInfoAsync(Guid tenantId, string agentId, string connectionId, AgentDeviceInfo deviceInfo)
        {
            _logger.LogDebug("Updating agent info for {AgentId} in tenant {TenantId}", agentId, tenantId);

            try
            {
                if (_connections.TryGetValue(connectionId, out var connectionInfo))
                {
                    connectionInfo.DeviceInfo = deviceInfo;
                    connectionInfo.LastHeartbeat = DateTime.UtcNow;

                    _logger.LogDebug("Agent info updated successfully for {AgentId}", agentId);
                }
                else
                {
                    _logger.LogWarning("Connection {ConnectionId} not found for agent {AgentId}", connectionId, agentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update agent info for {AgentId}", agentId);
            }
        }

        public async Task UpdateHeartbeatAsync(Guid tenantId, string agentId, string connectionId)
        {
            try
            {
                if (_connections.TryGetValue(connectionId, out var connectionInfo))
                {
                    connectionInfo.LastHeartbeat = DateTime.UtcNow;
                    _logger.LogTrace("Heartbeat updated for agent {AgentId}", agentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update heartbeat for agent {AgentId}", agentId);
            }
        }

        public async Task UpdateDeviceStatusAsync(Guid tenantId, string agentId, string deviceId, MP.LocalAgent.Contracts.Enums.DeviceStatus status, string? details)
        {
            _logger.LogDebug("Updating device status for agent {AgentId}, device {DeviceId}: {Status}",
                agentId, deviceId, status);

            try
            {
                if (_connectionMapping.TryGetValue((tenantId, agentId), out var connectionId) &&
                    _connections.TryGetValue(connectionId, out var connectionInfo))
                {
                    var device = connectionInfo.DeviceInfo.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
                    if (device != null)
                    {
                        device.Status = status;
                        device.LastStatusUpdate = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new device info if not exists
                        connectionInfo.DeviceInfo.Devices.Add(new DeviceInfo
                        {
                            DeviceId = deviceId,
                            Status = status,
                            LastStatusUpdate = DateTime.UtcNow,
                            IsEnabled = true
                        });
                    }

                    _logger.LogDebug("Device status updated successfully for {DeviceId}: {Status}", deviceId, status);
                }
                else
                {
                    _logger.LogWarning("Connection not found for agent {AgentId} in tenant {TenantId}", agentId, tenantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update device status for agent {AgentId}, device {DeviceId}",
                    agentId, deviceId);
            }
        }

        public async Task<AgentConnectionInfo?> GetActiveAgentAsync(Guid tenantId)
        {
            try
            {
                // Find active agent for tenant (most recent heartbeat)
                var agentConnection = _connections.Values
                    .Where(c => c.TenantId == tenantId && c.IsActive)
                    .OrderByDescending(c => c.LastHeartbeat)
                    .FirstOrDefault();

                return agentConnection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active agent for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<List<AgentConnectionInfo>> GetAgentsAsync(Guid tenantId)
        {
            try
            {
                var agents = _connections.Values
                    .Where(c => c.TenantId == tenantId)
                    .OrderByDescending(c => c.ConnectedAt)
                    .ToList();

                return agents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get agents for tenant {TenantId}", tenantId);
                return new List<AgentConnectionInfo>();
            }
        }

        public async Task<bool> IsAgentConnectedAsync(Guid tenantId, string agentId)
        {
            try
            {
                if (_connectionMapping.TryGetValue((tenantId, agentId), out var connectionId) &&
                    _connections.TryGetValue(connectionId, out var connectionInfo))
                {
                    return connectionInfo.IsActive;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check connection status for agent {AgentId}", agentId);
                return false;
            }
        }

        public async Task<DeviceStatusInfo?> GetDeviceStatusAsync(Guid tenantId, string agentId, string deviceId)
        {
            try
            {
                if (_connectionMapping.TryGetValue((tenantId, agentId), out var connectionId) &&
                    _connections.TryGetValue(connectionId, out var connectionInfo))
                {
                    var device = connectionInfo.DeviceInfo.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
                    if (device != null)
                    {
                        return new DeviceStatusInfo
                        {
                            DeviceId = deviceId,
                            Status = device.Status,
                            Details = null,
                            LastUpdated = device.LastStatusUpdate
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device status for agent {AgentId}, device {DeviceId}",
                    agentId, deviceId);
                return null;
            }
        }

        public async Task<ConnectionStatistics> GetStatisticsAsync()
        {
            try
            {
                var allConnections = _connections.Values.ToList();
                var activeConnections = allConnections.Where(c => c.IsActive).ToList();

                var statistics = new ConnectionStatistics
                {
                    TotalAgents = allConnections.Count,
                    ActiveAgents = activeConnections.Count,
                    TotalDevices = allConnections.Sum(c => c.DeviceInfo.Devices.Count),
                    OnlineDevices = activeConnections.Sum(c => c.DeviceInfo.Devices.Count(d => d.Status == MP.LocalAgent.Contracts.Enums.DeviceStatus.Ready || d.Status == MP.LocalAgent.Contracts.Enums.DeviceStatus.Online)),
                    DevicesByType = new Dictionary<string, int>(),
                    AgentsByTenant = new Dictionary<string, int>()
                };

                // Count devices by type
                foreach (var connection in activeConnections)
                {
                    foreach (var device in connection.DeviceInfo.Devices)
                    {
                        if (!statistics.DevicesByType.ContainsKey(device.DeviceType))
                            statistics.DevicesByType[device.DeviceType] = 0;
                        statistics.DevicesByType[device.DeviceType]++;
                    }
                }

                // Count agents by tenant
                foreach (var connection in activeConnections)
                {
                    var tenantKey = connection.TenantId.ToString();
                    if (!statistics.AgentsByTenant.ContainsKey(tenantKey))
                        statistics.AgentsByTenant[tenantKey] = 0;
                    statistics.AgentsByTenant[tenantKey]++;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get connection statistics");
                return new ConnectionStatistics();
            }
        }
    }
}