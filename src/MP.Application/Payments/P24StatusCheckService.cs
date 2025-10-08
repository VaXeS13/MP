using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Application.Services;

namespace MP.Application.Payments
{
    public class P24StatusCheckService : ApplicationService, ITransientDependency
    {
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly ILogger<P24StatusCheckService> _logger;

        public P24StatusCheckService(
            IBackgroundJobManager backgroundJobManager,
            ILogger<P24StatusCheckService> logger)
        {
            _backgroundJobManager = backgroundJobManager;
            _logger = logger;
        }

        public async Task ScheduleStatusCheckAsync(int delayMinutes = 15)
        {
            try
            {
                var args = new P24StatusCheckJobArgs
                {
                    ScheduledTime = DateTime.UtcNow.AddMinutes(delayMinutes)
                };

                await _backgroundJobManager.EnqueueAsync(args, delay: TimeSpan.FromMinutes(delayMinutes));

                _logger.LogInformation("P24 status check job scheduled to run in {DelayMinutes} minutes", delayMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling P24 status check job");
                throw;
            }
        }

        public async Task StartPeriodicStatusCheckAsync()
        {
            try
            {
                // Schedule immediate check
                await ScheduleStatusCheckAsync(0);

                // Schedule first delayed check
                await ScheduleStatusCheckAsync(15);

                _logger.LogInformation("P24 periodic status check started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting P24 periodic status check");
                throw;
            }
        }
    }
}