using Application.Modules.Files;
using System.Reflection;

namespace Api.Test.Architecture;

public sealed class ApiArchitectureTest
{
    [Fact]
    public void Removed_feature_namespaces_are_absent()
    {
        Assembly[] assemblies = [typeof(Program).Assembly, typeof(StoredFileModel).Assembly, typeof(Domain.Entities.BaseEntity).Assembly];
        string[] forbidden = [".Chat", ".Email", ".Identity", ".Notifications", ".Accounts"];

        string[] typeNames = assemblies.SelectMany(assembly => assembly.GetTypes()).Select(type => type.FullName ?? string.Empty).ToArray();
        foreach (string value in forbidden)
            Assert.DoesNotContain(typeNames, name => name.Contains(value, StringComparison.Ordinal));
    }
}
