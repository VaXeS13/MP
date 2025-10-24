using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.MultiTenancy;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Contracts.Models;
using MP.Services;
using MP.Domain.OrganizationalUnits;

namespace MP.HttpApi.Hubs
{
    /// <summary>
    /// SignalR Hub for communication with local agents
    /// </summary>
    [Authorize]
    public class LocalAgentHub : Hub
    {
        private readonly ILogger<LocalAgentHub> _logger;
        private readonly IAgentConnectionManager _connectionManager;
        private readonly IAgentCommandProcessor _commandProcessor;
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public LocalAgentHub(
            ILogger<LocalAgentHub> logger,
            IAgentConnectionManager connectionManager,
            IAgentCommandProcessor commandProcessor,
            ICurrentTenant currentTenant,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _commandProcessor = commandProcessor;
            _currentTenant = currentTenant;
            _currentOrganizationalUnit = currentOrganizationalUnit;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation("Agent {AgentId} connecting for tenant {TenantId} with connection {ConnectionId}",
                agentId, tenantId, connectionId);

            try
            {
                await _connectionManager.RegisterAgentAsync(tenantId, agentId, connectionId, Context.UserIdentifier);

                await Clients.Caller.SendAsync("Connected", new
                {
                    Message = "Successfully connected to MP Local Agent Hub",
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Agent {AgentId} successfully connected for tenant {TenantId}",
                    agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to register agent",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });

                throw;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation("Agent {AgentId} disconnecting for tenant {TenantId} with connection {ConnectionId}. Exception: {Exception}",
                agentId, tenantId, connectionId, exception?.Message);

            try
            {
                await _connectionManager.UnregisterAgentAsync(tenantId, agentId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent {AgentId} disconnect for tenant {TenantId}",
                    agentId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Register agent with the system
        /// </summary>
        public async Task RegisterAgent(AgentRegistrationRequest request)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();
            var organizationalUnitId = GetOrganizationalUnitId();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation("Registering agent {AgentId} for tenant {TenantId} and organizational unit {UnitId}",
                agentId, tenantId, organizationalUnitId);

            try
            {
                // Validate organizational unit context
                if (organizationalUnitId == Guid.Empty)
                {
                    throw new InvalidOperationException("Organizational unit context is required for agent registration");
                }

                await _connectionManager.UpdateAgentInfoAsync(tenantId, agentId, connectionId, request.DeviceInfo);

                await Clients.Caller.SendAsync("AgentRegistered", new
                {
                    Message = "Agent registered successfully",
                    AgentId = agentId,
                    TenantId = tenantId,
                    OrganizationalUnitId = organizationalUnitId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Agent {AgentId} registered successfully for tenant {TenantId} and unit {UnitId}",
                    agentId, tenantId, organizationalUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to register agent",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Unregister agent from the system
        /// </summary>
        public async Task UnregisterAgent()
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Unregistering agent {AgentId} for tenant {TenantId}", agentId, tenantId);

            try
            {
                await _connectionManager.UnregisterAgentAsync(tenantId, agentId, Context.ConnectionId);

                await Clients.Caller.SendAsync("AgentUnregistered", new
                {
                    Message = "Agent unregistered successfully",
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Agent {AgentId} unregistered successfully for tenant {TenantId}",
                    agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to unregister agent",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Send heartbeat to keep connection alive
        /// </summary>
        public async Task Heartbeat()
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            try
            {
                await _connectionManager.UpdateHeartbeatAsync(tenantId, agentId, Context.ConnectionId);

                _logger.LogDebug("Heartbeat received from agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat from agent {AgentId} for tenant {TenantId}",
                    agentId, tenantId);
            }
        }

        /// <summary>
        /// Report device status change
        /// </summary>
        public async Task ReportDeviceStatus(DeviceStatusReportRequest request)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Device status report from agent {AgentId} for tenant {TenantId} - Device: {DeviceId}, Status: {Status}",
                agentId, tenantId, request.DeviceId, request.Status);

            try
            {
                await _connectionManager.UpdateDeviceStatusAsync(tenantId, agentId, request.DeviceId,
                    request.Status, request.Details);

                // Notify connected clients about device status change
                await Clients.Group($"tenant_{tenantId}").SendAsync("DeviceStatusChanged", new
                {
                    AgentId = agentId,
                    DeviceId = request.DeviceId,
                    Status = request.Status,
                    Details = request.Details,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Device status report processed successfully for agent {AgentId}, device {DeviceId}",
                    agentId, request.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device status report from agent {AgentId} for device {DeviceId}",
                    agentId, request.DeviceId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to process device status report",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Report command execution result
        /// </summary>
        public async Task ReportCommandResult(CommandResponseBase response)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Command result reported from agent {AgentId} for tenant {TenantId} - Command: {CommandId}, Success: {Success}",
                agentId, tenantId, response.CommandId, response.Success);

            try
            {
                await _commandProcessor.ProcessCommandResponseAsync(tenantId, agentId, response);

                // Notify waiting clients about command result
                await Clients.Group($"command_{response.CommandId}").SendAsync("CommandResult", new
                {
                    CommandId = response.CommandId,
                    Success = response.Success,
                    Response = response,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Command result processed successfully for command {CommandId}",
                    response.CommandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command result from agent {AgentId} for command {CommandId}",
                    agentId, response.CommandId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to process command result",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #region Command Execution Methods

        /// <summary>
        /// Execute terminal command on agent
        /// </summary>
        public async Task ExecuteTerminalCommand(TerminalCommandRequest request)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Executing terminal command {CommandType} on agent {AgentId} for tenant {TenantId}",
                request.CommandType, agentId, tenantId);

            try
            {
                await _commandProcessor.QueueTerminalCommandAsync(tenantId, agentId, request.CommandType,
                    request.CommandData, request.Timeout);

                _logger.LogInformation("Terminal command {CommandType} queued successfully for agent {AgentId}",
                    request.CommandType, agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue terminal command {CommandType} for agent {AgentId}",
                    request.CommandType, agentId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to queue terminal command",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Execute fiscal printer command on agent
        /// </summary>
        public async Task ExecuteFiscalPrinterCommand(FiscalPrinterCommandRequest request)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Executing fiscal printer command {CommandType} on agent {AgentId} for tenant {TenantId}",
                request.CommandType, agentId, tenantId);

            try
            {
                await _commandProcessor.QueueFiscalPrinterCommandAsync(tenantId, agentId, request.CommandType,
                    request.CommandData, request.Timeout);

                _logger.LogInformation("Fiscal printer command {CommandType} queued successfully for agent {AgentId}",
                    request.CommandType, agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue fiscal printer command {CommandType} for agent {AgentId}",
                    request.CommandType, agentId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to queue fiscal printer command",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Cancel command execution
        /// </summary>
        public async Task CancelCommand(Guid commandId)
        {
            var tenantId = GetTenantId();
            var agentId = GetAgentId();

            _logger.LogInformation("Cancelling command {CommandId} on agent {AgentId} for tenant {TenantId}",
                commandId, agentId, tenantId);

            try
            {
                await _commandProcessor.CancelCommandAsync(tenantId, agentId, commandId);

                _logger.LogInformation("Command {CommandId} cancelled successfully for agent {AgentId}",
                    commandId, agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel command {CommandId} for agent {AgentId}",
                    commandId, agentId);

                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Failed to cancel command",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion

        #region Helper Methods

        private Guid GetTenantId()
        {
            // Try to get tenant ID from headers first, then from user context
            if (Context.GetHttpContext().Request.Headers.TryGetValue("Tenant-Id", out var tenantIdValue) &&
                Guid.TryParse(tenantIdValue, out var tenantId))
            {
                return tenantId;
            }

            // Fallback to current user's tenant
            return _currentTenant.Id ?? Guid.Empty;
        }

        private string GetAgentId()
        {
            // Try to get agent ID from headers first, then generate from connection
            if (Context.GetHttpContext().Request.Headers.TryGetValue("Agent-Id", out var agentIdValue))
            {
                return agentIdValue!;
            }

            // Fallback to connection-based ID
            return $"agent_{Context.ConnectionId}";
        }

        private Guid GetOrganizationalUnitId()
        {
            // Try to get organizational unit ID from headers first, then from context
            if (Context.GetHttpContext().Request.Headers.TryGetValue("OrganizationalUnit-Id", out var unitIdValue) &&
                Guid.TryParse(unitIdValue, out var unitId))
            {
                return unitId;
            }

            // Fallback to current organizational unit context
            return _currentOrganizationalUnit.Id ?? Guid.Empty;
        }

        #endregion
    }
}