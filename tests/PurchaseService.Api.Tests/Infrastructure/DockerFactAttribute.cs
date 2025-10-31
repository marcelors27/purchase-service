using Xunit;

namespace PurchaseService.Api.Tests.Infrastructure;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DockerFactAttribute : FactAttribute
{
    private const string ToggleVariable = "RUN_DOCKER_TESTS";

    public DockerFactAttribute()
    {
        var toggle = Environment.GetEnvironmentVariable(ToggleVariable);
        if (!string.Equals(toggle, "1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(toggle, "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Docker-based tests disabled. Set {ToggleVariable}=1 to enable.";
        }
    }
}
