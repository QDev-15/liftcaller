using Microsoft.Extensions.Options;

namespace IMIP.TochuForkLift.Api.Services;

public class SignalLightNotifier(IHttpClientFactory httpClientFactory, IOptions<SignalLightOptions> options, ILogger<SignalLightNotifier> logger) : ISignalLightNotifier
{
    public async Task NotifyAsync(string pattern, CancellationToken ct = default)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            logger.LogDebug("SignalLight disabled — skipping pattern {Pattern}", pattern);
            return;
        }

        try
        {
            using var client = httpClientFactory.CreateClient(nameof(SignalLightNotifier));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(opt.TimeoutSeconds));

            var url = $"http://{opt.Host}/api/control?led={pattern}";
            var response = await client.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SignalLight at {Host} returned {StatusCode} for pattern {Pattern}", opt.Host, response.StatusCode, pattern);
            }
        }
        catch (Exception ex)
        {
            // Never let a dead/unreachable light block the actual call workflow — plan.md §6.1 step 5.
            logger.LogWarning(ex, "Failed to notify SignalLight at {Host} with pattern {Pattern}", opt.Host, pattern);
        }
    }
}
