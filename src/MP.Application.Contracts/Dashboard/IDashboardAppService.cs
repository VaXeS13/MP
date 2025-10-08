using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Dashboard
{
    public interface IDashboardAppService : IApplicationService
    {
        Task<DashboardOverviewDto> GetOverviewAsync(PeriodFilterDto filter);

        Task<SalesOverviewDto> GetSalesAnalyticsAsync(PeriodFilterDto filter);

        Task<BoothOccupancyOverviewDto> GetBoothOccupancyAsync(PeriodFilterDto filter);

        Task<FinancialOverviewDto> GetFinancialReportsAsync(PeriodFilterDto filter);

        Task<CustomerAnalyticsDto> GetCustomerAnalyticsAsync(PeriodFilterDto filter);

        Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(PeriodFilterDto filter);

        Task<byte[]> ExportSalesReportAsync(PeriodFilterDto filter, ExportFormat format);

        Task<byte[]> ExportFinancialReportAsync(PeriodFilterDto filter, ExportFormat format);
    }

    public enum ExportFormat
    {
        Excel,
        Pdf,
        Csv
    }
}