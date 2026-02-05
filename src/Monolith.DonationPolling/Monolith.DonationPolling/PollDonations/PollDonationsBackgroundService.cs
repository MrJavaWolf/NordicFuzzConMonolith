
namespace Monolith.DonationPolling.PollDonations;

public class PollDonationsBackgroundService(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<PollDonationsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan pollRate = configuration.GetRequiredValue<TimeSpan>("DonationPlatform:PollingRate");
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Polls new data from donation endpoint");
            using var scope = services.CreateScope();
            var pollDonations = scope.ServiceProvider.GetRequiredService<PollDonationService>();
            await pollDonations.Poll(stoppingToken);
            logger.LogInformation("Will poll again in {pollRate}", pollRate);
            await Task.Delay(pollRate, stoppingToken);
        }
    }
}
