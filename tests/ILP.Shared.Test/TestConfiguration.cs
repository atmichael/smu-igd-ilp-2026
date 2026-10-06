using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ILP.Shared.Test.InfoExtraction.Config;

sealed class TestConfiguration(Dictionary<string, string?> values) : IConfiguration
{
    public string? this[string key]
    {
        get => values.GetValueOrDefault(key);
        set => values[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => [];

    public IChangeToken GetReloadToken() => throw new NotSupportedException();

    public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
}
