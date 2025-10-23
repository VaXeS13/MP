using System;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

namespace MP.Application.Devices
{
    /// <summary>
    /// Abstraction layer for communicating with local agents and their devices via SignalR
    /// </summary>
    public interface IRemoteDeviceProxy
    {
        /// <summary>
        /// Authorize a payment on the terminal device
        /// </summary>
        Task<TerminalPaymentResponse> AuthorizePaymentAsync(
            AuthorizeTerminalPaymentCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Capture a previously authorized payment
        /// </summary>
        Task<TerminalPaymentResponse> CapturePaymentAsync(
            CaptureTerminalPaymentCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refund a captured payment
        /// </summary>
        Task<TerminalPaymentResponse> RefundPaymentAsync(
            RefundTerminalPaymentCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel a payment transaction
        /// </summary>
        Task<TerminalPaymentResponse> CancelPaymentAsync(
            CancelTerminalPaymentCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check terminal device availability and status
        /// </summary>
        Task<TerminalStatusResponse> CheckTerminalStatusAsync(
            CheckTerminalStatusCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Print a fiscal receipt on the fiscal printer
        /// </summary>
        Task<FiscalReceiptResponse> PrintFiscalReceiptAsync(
            PrintFiscalReceiptCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Print a non-fiscal document
        /// </summary>
        Task<SimpleCommandResponse> PrintNonFiscalDocumentAsync(
            PrintNonFiscalDocumentCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get daily fiscal report from printer
        /// </summary>
        Task<FiscalReportResponse> GetDailyFiscalReportAsync(
            GetDailyFiscalReportCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check fiscal printer status
        /// </summary>
        Task<FiscalPrinterStatusResponse> CheckFiscalPrinterStatusAsync(
            CheckFiscalPrinterStatusCommand request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a device is available for the current tenant
        /// </summary>
        Task<bool> IsDeviceAvailableAsync(
            string deviceType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get device status information
        /// </summary>
        Task<string> GetDeviceStatusAsync(
            string deviceType,
            CancellationToken cancellationToken = default);
    }
}
