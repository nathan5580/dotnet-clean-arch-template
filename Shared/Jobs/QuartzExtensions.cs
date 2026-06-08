using Quartz;

namespace Shared.Jobs;

public static class QuartzExtensions
{
    public static IServiceCollection AddAppQuartzJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(q =>
        {
            // Register your jobs here
            // var jobKey = new JobKey("ExampleJob");
            // q.AddJob<ExampleJob>(opts => opts.WithIdentity(jobKey));
            // q.AddTrigger(opts => opts
            //     .ForJob(jobKey)
            //     .WithCronSchedule("0 0 6 * * ?"));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
