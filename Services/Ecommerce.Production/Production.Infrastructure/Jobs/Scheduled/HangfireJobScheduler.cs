using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Infrastructure.Jobs.Scheduled;

/// <summary>
/// Configures and schedules recurring jobs using Hangfire
/// </summary>
public static class HangfireJobScheduler
{
    /// <summary>
    /// Configures all recurring jobs for the Product Catalog Service
    /// </summary>
    public static void ConfigureReCurringjobs()
    {
        //Daily analytics - runs
        RecurringJob.AddOrUpdate<ProductCatologjobs>(
            "daily-analytics-afternoon",
            job => job.GenerateDailyAnalyticsAsync(),
            Cron.Daily(14, 0), 
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        RecurringJob.AddOrUpdate<ProductCatologjobs>(
           "cache-cleanup",
           job => job.CleanupOldCacheEntriesAsync(),
           Cron.Hourly(6), // Every 6 hours
           new RecurringJobOptions
           {
               TimeZone = TimeZoneInfo.Utc
           });

        RecurringJob.AddOrUpdate<ProductCatologjobs>(
           "update-popularity-scores",
           job => job.UpdateProductPopularityScoresAsync(),
           Cron.Daily(15, 9), // 14:00 là 2 giờ chiều
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });
        //"0 */12 * * *", // Every 12 hours at minute 0
        //new RecurringJobOptions
        //{
        //    TimeZone = TimeZoneInfo.Utc
        //});

        RecurringJob.AddOrUpdate<ProductCatologjobs>(
           "weekly-inventory-report",
           job => job.GenerateWeeklyInventoryReportAsync(),
           Cron.Weekly(DayOfWeek.Monday, 8, 0), // Monday at 8:00 AM UTC
           new RecurringJobOptions
           {
               TimeZone = TimeZoneInfo.Utc
           });

        RecurringJob.AddOrUpdate<ProductCatologjobs>(
           "monthly-data-cleanup",
           job => job.CleanupOldDataAsync(),
           Cron.Monthly(1, 3, 0), // 1st day of month at 3:00 AM UTC
           new RecurringJobOptions
           {
               TimeZone = TimeZoneInfo.Utc
           });
    }

    public static void RemoveAllRecurringJobs()
    {
        RecurringJob.RemoveIfExists("daily-analytics");
        RecurringJob.RemoveIfExists("cache-cleanup");
        RecurringJob.RemoveIfExists("update-popularity-scores");
        RecurringJob.RemoveIfExists("weekly-inventory-report");
        RecurringJob.RemoveIfExists("monthly-data-cleanup");
    }
}

