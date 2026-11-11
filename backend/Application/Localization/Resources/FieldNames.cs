using System.Resources;

namespace Application.Localization.Resources;

public static class FieldNames
{
    public static ResourceManager ResourceManager { get; } = new(
        "Application.Localization.Resources.FieldNames",
        typeof(FieldNames).Assembly);

    public static string Animation_Duration => Get(nameof(Animation_Duration));

    public static string Content_Type => Get(nameof(Content_Type));

    public static string Display_Name => Get(nameof(Display_Name));

    public static string Display_Duration => Get(nameof(Display_Duration));

    public static string File => Get(nameof(File));

    public static string File_Content => Get(nameof(File_Content));

    public static string File_Name => Get(nameof(File_Name));

    public static string Id => Get(nameof(Id));

    public static string Limit => Get(nameof(Limit));

    public static string Maximum_File_Size => Get(nameof(Maximum_File_Size));

    public static string Name => Get(nameof(Name));

    public static string Pitch_Semitones => Get(nameof(Pitch_Semitones));

    public static string Playback_Rate => Get(nameof(Playback_Rate));

    public static string Purpose => Get(nameof(Purpose));

    public static string Queue_Priority => Get(nameof(Queue_Priority));

    public static string Volume => Get(nameof(Volume));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
