using System.Resources;

namespace Application.Localization.Resources;

public static class ValidationText
{
    public static ResourceManager ResourceManager { get; } = new(
        "Application.Localization.Resources.ValidationText",
        typeof(ValidationText).Assembly);

    public static string Animation_Type_Is_Not_Supported => Get(nameof(Animation_Type_Is_Not_Supported));

    public static string Display_Duration_Is_Not_Supported => Get(nameof(Display_Duration_Is_Not_Supported));

    public static string Display_Size_Is_Not_Supported => Get(nameof(Display_Size_Is_Not_Supported));

    public static string Field_Cannot_Be_Read => Get(nameof(Field_Cannot_Be_Read));

    public static string Field_Cannot_Exceed => Get(nameof(Field_Cannot_Exceed));

    public static string Field_Is_Required => Get(nameof(Field_Is_Required));

    public static string Field_Must_Be_Between => Get(nameof(Field_Must_Be_Between));

    public static string Field_Must_Be_Greater_Than => Get(nameof(Field_Must_Be_Greater_Than));

    public static string Queue_Priority_Is_Not_Supported => Get(nameof(Queue_Priority_Is_Not_Supported));

    public static string Time_Zone_Is_Not_Valid => Get(nameof(Time_Zone_Is_Not_Valid));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
