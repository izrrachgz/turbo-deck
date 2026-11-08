using System.Reflection;
using Domain.Entities;

namespace Application.Test.Architecture;

public sealed class BaseArchitectureTest
{
    [Fact]
    public void Domain_has_no_infrastructure_or_feature_dependencies()
    {
        Assembly domain = typeof(BaseEntity).Assembly;
        string[] references = domain.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();

        Assert.DoesNotContain(references, reference => reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, reference => reference.Contains("Persistence", StringComparison.Ordinal));
        Assert.DoesNotContain(references, reference => reference.Contains("Npgsql", StringComparison.Ordinal));
    }
}
