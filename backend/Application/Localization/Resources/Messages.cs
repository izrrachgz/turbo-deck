using System.Resources;

namespace Application.Localization.Resources;

public static class Messages
{
    public static ResourceManager ResourceManager { get; } = new(
        "Application.Localization.Resources.Messages",
        typeof(Messages).Assembly);

    public static string Bad_Gateway => Get(nameof(Bad_Gateway));

    public static string Conflict => Get(nameof(Conflict));

    public static string File_Content_Does_Not_Match_Extension => Get(nameof(File_Content_Does_Not_Match_Extension));

    public static string File_Exceeds_Maximum_Size => Get(nameof(File_Exceeds_Maximum_Size));

    public static string File_Is_Required => Get(nameof(File_Is_Required));

    public static string File_Type_Not_Allowed => Get(nameof(File_Type_Not_Allowed));

    public static string Forbidden => Get(nameof(Forbidden));

    public static string Invalid_Model => Get(nameof(Invalid_Model));

    public static string Not_Found => Get(nameof(Not_Found));

    public static string Record_Not_Found => Get(nameof(Record_Not_Found));

    public static string Server_Error => Get(nameof(Server_Error));

    public static string Service_Unavailable => Get(nameof(Service_Unavailable));

    public static string Too_Many_Requests => Get(nameof(Too_Many_Requests));

    public static string Unauthorized => Get(nameof(Unauthorized));

    private static string Get(string key) => ResourceManager.GetString(key) ?? key;
}
