using Time.Extensions;
using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Settings.Commands;

internal sealed class UpdateAppSettingsCommandValidator : AbstractValidator<UpdateAppSettingsCommand>
{
    private static readonly string[] Cultures = ["en-US", "es-ES"];
    private static readonly string[] Themes = ["dark", "light"];
    private static readonly string[] DarkPalettes = ["midnight", "graphite", "ocean", "violet", "ember"];
    private static readonly string[] LightPalettes = ["daylight", "forest", "rose", "sunset", "nord"];

    public UpdateAppSettingsCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.Culture).Must(Cultures.Contains)
            .WithMessage(messages.Get("Culture_Is_Not_Supported"));
        RuleFor(command => command.Theme).Must(Themes.Contains)
            .WithMessage(messages.Get("Theme_Is_Not_Supported"));
        RuleFor(command => command)
            .Must(command => command.Theme == "dark"
                ? DarkPalettes.Contains(command.Palette)
                : command.Theme == "light" && LightPalettes.Contains(command.Palette))
            .WithMessage(messages.Get("Palette_Is_Not_Supported"));
        RuleFor(command => command.TimeZone)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get("Time Zone")))
            .MaximumLength(64)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get("Time Zone"), 64))
            .Must(DateTimeUtcExtensions.IsValidTimeZone)
            .WithMessage(messages.Get(nameof(ValidationText.Time_Zone_Is_Not_Valid)));
        RuleFor(command => command.QueueCapacity).InclusiveBetween(1, 1000)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get("Queue Capacity"), 1, 1000));
        RuleFor(command => command.VisualizerSafeMargin).InclusiveBetween(0, 1000)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get("Visualizer Safe Margin"), 0, 1000));
    }
}
