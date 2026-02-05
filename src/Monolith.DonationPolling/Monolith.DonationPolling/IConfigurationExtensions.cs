namespace Monolith.DonationPolling;

public static class IConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        T? value = configuration.GetValue<T>(key);
        if (value == null)
        {
            throw new KeyNotFoundException($"Failed to find '{key}' in the configurations.");
        }
        return value;
    }
}
