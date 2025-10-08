using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp;
using MP.Application.Contracts.Dashboard;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [Route("api/app/dashboard")]
    public class DashboardController : AbpControllerBase
    {
        private readonly IDashboardAppService _dashboardAppService;

        public DashboardController(IDashboardAppService dashboardAppService)
        {
            _dashboardAppService = dashboardAppService;
        }

        [HttpGet]
        [Route("overview")]
        public Task<DashboardOverviewDto> GetOverviewAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetOverviewAsync(filter);
        }

        [HttpGet]
        [Route("sales-analytics")]
        public Task<SalesOverviewDto> GetSalesAnalyticsAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetSalesAnalyticsAsync(filter);
        }

        [HttpGet]
        [Route("booth-occupancy")]
        public Task<BoothOccupancyOverviewDto> GetBoothOccupancyAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetBoothOccupancyAsync(filter);
        }

        [HttpGet]
        [Route("financial-reports")]
        public Task<FinancialOverviewDto> GetFinancialReportsAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetFinancialReportsAsync(filter);
        }

        [HttpGet]
        [Route("customer-analytics")]
        public Task<CustomerAnalyticsDto> GetCustomerAnalyticsAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetCustomerAnalyticsAsync(filter);
        }

        [HttpPost]
        [Route("export-sales-report")]
        public Task<byte[]> ExportSalesReportAsync(PeriodFilterDto filter, ExportFormat format)
        {
            return _dashboardAppService.ExportSalesReportAsync(filter, format);
        }

        [HttpPost]
        [Route("export-financial-report")]
        public Task<byte[]> ExportFinancialReportAsync(PeriodFilterDto filter, ExportFormat format)
        {
            return _dashboardAppService.ExportFinancialReportAsync(filter, format);
        }

        [HttpGet]
        [Route("payment-analytics")]
        public Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(PeriodFilterDto filter)
        {
            return _dashboardAppService.GetPaymentAnalyticsAsync(filter);
        }
    }
}