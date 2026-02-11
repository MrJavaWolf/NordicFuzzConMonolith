namespace Monolith.DonationPolling;

public static class IConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);
        if (section == null)
        {
            throw new KeyNotFoundException($"Failed to find '{key}' in the configurations.");
        }
        
        T? value = section.Get<T>();
        if (value == null)
        {
            throw new KeyNotFoundException($"Failed to convert '{key}' to '{typeof(T).FullName}' in the configurations.");
        }
        return value;
    }
}
