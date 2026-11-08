using System.Resources;

namespace Application.Localization.Resources;

public static class FieldNames
{
    public static ResourceManager ResourceManager { get; } = new(
        "Application.Localization.Resources.FieldNames",
        typeof(FieldNames).Assembly);

    public static string Content_Type => Get(nameof(Content_Type));

    public static string File => Get(nameof(File));

    public static string File_Content => Get(nameof(File_Content));

    public static string File_Name => Get(nameof(File_Name));

    public static string Id => Get(nameof(Id));

    public static string Limit => Get(nameof(Limit));

    public static string Maximum_File_Size => Get(nameof(Maximum_File_Size));

    public static string Name => Get(nameof(Name));

    public static string Purpose => Get(nameof(Purpose));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
