using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Deck.Commands;

internal sealed class UpsertCellConfigCommandValidator : AbstractValidator<UpsertCellConfigCommand>
{
    public UpsertCellConfigCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.PageId)
            .GreaterThan(0)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
        RuleFor(command => command.RowIndex)
            .InclusiveBetween(0, 3)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get("Row"), 0, 3));
        RuleFor(command => command.ColumnIndex)
            .InclusiveBetween(0, 7)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get("Column"), 0, 7));
        RuleFor(command => command.Label)
            .MaximumLength(80)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get("Label"), 80));
        RuleFor(command => command.MediaAssetId)
            .GreaterThan(0)
            .When(command => command.MediaAssetId.HasValue)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
        RuleFor(command => command.ThumbnailMediaAssetId)
            .GreaterThan(0)
            .When(command => command.ThumbnailMediaAssetId.HasValue)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
        RuleFor(command => command.Volume)
            .InclusiveBetween(0m, 1m)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Between),
                messages.Get(nameof(FieldNames.Volume)),
                0,
                1));
        RuleFor(command => command.PlaybackRate)
            .InclusiveBetween(0.25m, 4m)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Between),
                messages.Get(nameof(FieldNames.Playback_Rate)),
                0.25,
                4));
        RuleFor(command => command.PitchSemitones)
            .InclusiveBetween(-24m, 24m)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Between),
                messages.Get(nameof(FieldNames.Pitch_Semitones)),
                -24,
                24));
        RuleFor(command => command.DisplayDurationMilliseconds)
            .Must(duration => duration is 3000 or 5000 or 10000)
            .WithMessage(messages.Get(nameof(ValidationText.Display_Duration_Is_Not_Supported)));
        RuleFor(command => command.DisplaySize)
            .Must(size => size is "small" or "medium" or "large" or "fullscreen")
            .WithMessage(messages.Get(nameof(ValidationText.Display_Size_Is_Not_Supported)));
        RuleFor(command => command.AnimationInType)
            .Must(IsSupportedAnimation)
            .WithMessage(messages.Get(nameof(ValidationText.Animation_Type_Is_Not_Supported)));
        RuleFor(command => command.AnimationOutType)
            .Must(IsSupportedAnimation)
            .WithMessage(messages.Get(nameof(ValidationText.Animation_Type_Is_Not_Supported)));
        RuleFor(command => command.AnimationInDurationMilliseconds)
            .InclusiveBetween(0, 10000)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Between),
                messages.Get(nameof(FieldNames.Animation_Duration)),
                0,
                10000));
        RuleFor(command => command.AnimationOutDurationMilliseconds)
            .InclusiveBetween(0, 10000)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Between),
                messages.Get(nameof(FieldNames.Animation_Duration)),
                0,
                10000));
        RuleFor(command => command.QueuePriority)
            .Must(priority => priority is "normal" or "override")
            .WithMessage(messages.Get(nameof(ValidationText.Queue_Priority_Is_Not_Supported)));
    }

    private static bool IsSupportedAnimation(string type) => type is
        "none" or "fade" or "slide" or "zoom" or "spin" or "flip" or "bounce" or "morph" or "orbit" or "blur";
}
