using FluentValidation;
using System.Globalization;
using Application.Extensions;
using Localization.Providers;
using Localization.Abstractions;
using Application.Localization.Resources;
using Application.Modules.Files.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Test.Localization;

public sealed class ApplicationLocalizationTest
{
    [Fact]
    public async Task Application_composition_registers_scoped_localization_and_localized_validators()
    {
        IConfiguration configuration = CreateConfiguration("Json", "Resx");
        var services = new ServiceCollection();
        services.ConfigureApplicationServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
            await using AsyncServiceScope firstScope = provider.CreateAsyncScope();
            await using AsyncServiceScope secondScope = provider.CreateAsyncScope();
            ITextProvider firstTextProvider = firstScope.ServiceProvider.GetRequiredService<ITextProvider>();
            ITextProvider secondTextProvider = secondScope.ServiceProvider.GetRequiredService<ITextProvider>();
            ValidationTextProvider validationText = firstScope.ServiceProvider.GetRequiredService<ValidationTextProvider>();
            IValidator<UploadFileCommand> validator = firstScope.ServiceProvider.GetRequiredService<IValidator<UploadFileCommand>>();

            var command = new UploadFileCommand(string.Empty, string.Empty, Stream.Null, 0, new string('x', 101));
            var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal("Nombre del archivo", firstTextProvider.Get(nameof(FieldNames.File_Name)));
            Assert.Equal("Nombre del archivo es obligatorio.", validationText.Get(
                nameof(ValidationText.Field_Is_Required),
                validationText.Get(nameof(FieldNames.File_Name))));
            Assert.Contains(result.Errors, error => error.ErrorMessage == "Nombre del archivo es obligatorio.");
            Assert.NotSame(firstTextProvider, secondTextProvider);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("en-US", "Record not found.")]
    [InlineData("es-ES", "No se encontró el registro.")]
    public void Resx_catalogs_follow_the_current_UI_culture(string cultureName, string expected)
    {
        IConfiguration configuration = CreateConfiguration("Resx");
        var services = new ServiceCollection();
        services.ConfigureApplicationServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            using IServiceScope scope = provider.CreateScope();

            Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ITextProvider>().Get(nameof(Messages.Record_Not_Found)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    private static IConfiguration CreateConfiguration(params string[] providerOrder)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Engine"] = "SqLite",
            ["ConnectionStrings:Db"] = "Data Source=:memory:;Version=3;Foreign Keys=True;",
            ["Localization:DefaultCulture"] = "en-US",
            ["Localization:FallbackCulture"] = "en-US",
            ["Localization:SupportedCultures:0"] = "en-US",
            ["Localization:SupportedCultures:1"] = "es-ES"
        };

        for (int index = 0; index < providerOrder.Length; index++)
            values[$"Localization:ProviderOrder:{index}"] = providerOrder[index];

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
