using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Application.Devices
{
    /// <summary>
    /// Abstraction layer for SignalR Hub communication - implemented by LocalAgentHub
    /// </summary>
    public interface IDeviceProxyHub
    {
        /// <summary>
        /// Send a command to a specific agent
        /// </summary>
        Task SendCommandToAgentAsync(
            string agentId,
            Guid commandId,
            string commandType,
            object commandData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available agent for a tenant and device type
        /// </summary>
        Task<string?> GetAvailableAgentIdAsync(
            Guid tenantId,
            string deviceType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if an agent is connected
        /// </summary>
        Task<bool> IsAgentConnectedAsync(
            string agentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get device status from connected agent
        /// </summary>
        Task<string?> GetDeviceStatusAsync(
            Guid tenantId,
            string deviceType,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Registry for command responses waiting for delivery
    /// </summary>
    public interface ICommandResponseRegistry
    {
        void RegisterWaitingResponse(Guid commandId, TaskCompletionSource<object> tcs);
        bool TryGetResponse(Guid commandId, out TaskCompletionSource<object>? tcs);
        void UnregisterResponse(Guid commandId);
    }

    /// <summary>
    /// In-memory registry for command responses
    /// </summary>
    public class InMemoryCommandResponseRegistry : ICommandResponseRegistry
    {
        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> Responses = new();

        public void RegisterWaitingResponse(Guid commandId, TaskCompletionSource<object> tcs)
        {
            Responses.TryAdd(commandId, tcs);
        }

        public bool TryGetResponse(Guid commandId, out TaskCompletionSource<object>? tcs)
        {
            return Responses.TryGetValue(commandId, out tcs);
        }

        public void UnregisterResponse(Guid commandId)
        {
            Responses.TryRemove(commandId, out _);
        }
    }
}
