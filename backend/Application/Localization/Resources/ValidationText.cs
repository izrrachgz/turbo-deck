using System.Resources;

namespace Application.Localization.Resources;

public static class ValidationText
{
    public static ResourceManager ResourceManager { get; } = new(
        "Application.Localization.Resources.ValidationText",
        typeof(ValidationText).Assembly);

    public static string Field_Cannot_Be_Read => Get(nameof(Field_Cannot_Be_Read));

    public static string Field_Cannot_Exceed => Get(nameof(Field_Cannot_Exceed));

    public static string Field_Is_Required => Get(nameof(Field_Is_Required));

    public static string Field_Must_Be_Between => Get(nameof(Field_Must_Be_Between));

    public static string Field_Must_Be_Greater_Than => Get(nameof(Field_Must_Be_Greater_Than));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
