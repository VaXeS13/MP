using System;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

namespace MP.LocalAgent.Contracts.Interfaces
{
    /// <summary>
    /// Interface for communicating with local agents
    /// </summary>
    public interface ILocalAgentClient
    {
        /// <summary>
        /// Send terminal payment authorization command
        /// </summary>
        Task<TerminalPaymentResponse> AuthorizePaymentAsync(AuthorizeTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send terminal payment capture command
        /// </summary>
        Task<TerminalPaymentResponse> CapturePaymentAsync(CaptureTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send terminal payment refund command
        /// </summary>
        Task<TerminalPaymentResponse> RefundPaymentAsync(RefundTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send terminal payment cancel command
        /// </summary>
        Task<TerminalPaymentResponse> CancelPaymentAsync(CancelTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check terminal status
        /// </summary>
        Task<TerminalStatusResponse> CheckTerminalStatusAsync(CheckTerminalStatusCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print fiscal receipt
        /// </summary>
        Task<FiscalReceiptResponse> PrintFiscalReceiptAsync(PrintFiscalReceiptCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print non-fiscal document
        /// </summary>
        Task<SimpleCommandResponse> PrintNonFiscalDocumentAsync(PrintNonFiscalDocumentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get daily fiscal report
        /// </summary>
        Task<FiscalReportResponse> GetDailyFiscalReportAsync(GetDailyFiscalReportCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel last fiscal receipt
        /// </summary>
        Task<SimpleCommandResponse> CancelLastFiscalReceiptAsync(CancelLastFiscalReceiptCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check fiscal printer status
        /// </summary>
        Task<FiscalPrinterStatusResponse> CheckFiscalPrinterStatusAsync(CheckFiscalPrinterStatusCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if agent is online and connected
        /// </summary>
        Task<bool> IsAgentConnectedAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get agent connection status
        /// </summary>
        Task<Enums.AgentConnectionStatus> GetAgentConnectionStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for local agent to communicate with cloud API
    /// </summary>
    public interface ILocalAgentService
    {
        /// <summary>
        /// Register agent with cloud API
        /// </summary>
        Task RegisterAgentAsync(Guid tenantId, string agentId, string deviceInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregister agent from cloud API
        /// </summary>
        Task UnregisterAgentAsync(Guid tenantId, string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send heartbeat to keep connection alive
        /// </summary>
        Task SendHeartbeatAsync(Guid tenantId, string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Report device status changes
        /// </summary>
        Task ReportDeviceStatusAsync(Guid tenantId, string agentId, string deviceId, Enums.DeviceStatus status, string? details = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Report command execution results
        /// </summary>
        Task ReportCommandResultAsync(Guid tenantId, string agentId, CommandResponseBase response, CancellationToken cancellationToken = default);
    }
}