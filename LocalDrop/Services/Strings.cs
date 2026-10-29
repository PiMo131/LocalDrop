using System.Globalization;

namespace LocalDrop.Services;

/// <summary>Tiny built-in localisation: Dutch when the UI culture is nl-*, otherwise English.</summary>
public static class Strings
{
    private static readonly Dictionary<string, string> En = new()
    {
        ["Title"] = "LocalDrop",
        ["Subtitle"] = "Send files from your phone to this device over Wi-Fi",
        ["Running"] = "Receiving",
        ["Stopped"] = "Stopped",
        ["Start"] = "Start",
        ["Stop"] = "Stop",
        ["Code"] = "Code",
        ["ScanHint"] = "Scan the QR code with your phone, or open the address in a browser on the same Wi-Fi and enter the code.",
        ["NoNetwork"] = "No Wi-Fi or LAN connection found. Connect to a network and press Refresh.",
        ["Refresh"] = "Refresh",
        ["OpenFolder"] = "Open folder",
        ["SavedTo"] = "Files are saved to",
        ["Received"] = "Received",
        ["NothingYet"] = "Nothing received yet.",
        ["NewPin"] = "New code",
        ["CopyLink"] = "Copy link",
        ["Copied"] = "Link copied",
        ["Quit"] = "Quit",
        ["Show"] = "Show LocalDrop",
        ["NotifTitle"] = "File received",
        ["NotifBody"] = "{0} from {1}",
        ["ServiceNotif"] = "LocalDrop is receiving files on {0}",
        ["LanOnly"] = "Only devices on this network can reach this page.",
        ["From"] = "from",
    };

    private static readonly Dictionary<string, string> Nl = new()
    {
        ["Title"] = "LocalDrop",
        ["Subtitle"] = "Stuur bestanden van je telefoon naar dit apparaat via wifi",
        ["Running"] = "Ontvangt",
        ["Stopped"] = "Gestopt",
        ["Start"] = "Start",
        ["Stop"] = "Stop",
        ["Code"] = "Code",
        ["ScanHint"] = "Scan de QR-code met je telefoon, of open het adres in een browser op dezelfde wifi en vul de code in.",
        ["NoNetwork"] = "Geen wifi- of LAN-verbinding gevonden. Verbind met een netwerk en druk op Vernieuwen.",
        ["Refresh"] = "Vernieuwen",
        ["OpenFolder"] = "Map openen",
        ["SavedTo"] = "Bestanden komen in",
        ["Received"] = "Ontvangen",
        ["NothingYet"] = "Nog niets ontvangen.",
        ["NewPin"] = "Nieuwe code",
        ["CopyLink"] = "Link kopiëren",
        ["Copied"] = "Link gekopieerd",
        ["Quit"] = "Afsluiten",
        ["Show"] = "LocalDrop tonen",
        ["NotifTitle"] = "Bestand ontvangen",
        ["NotifBody"] = "{0} van {1}",
        ["ServiceNotif"] = "LocalDrop ontvangt bestanden op {0}",
        ["LanOnly"] = "Alleen apparaten op dit netwerk kunnen deze pagina bereiken.",
        ["From"] = "van",
    };

    public static bool IsDutch =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);

    public static string Get(string key)
    {
        var dict = IsDutch ? Nl : En;
        return dict.TryGetValue(key, out var v) ? v : (En.TryGetValue(key, out var e) ? e : key);
    }

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    // Convenience properties for XAML x:Static bindings
    public static string Title => Get("Title");
    public static string Subtitle => Get("Subtitle");
    public static string Code => Get("Code");
    public static string ScanHint => Get("ScanHint");
    public static string Refresh => Get("Refresh");
    public static string OpenFolder => Get("OpenFolder");
    public static string SavedTo => Get("SavedTo");
    public static string Received => Get("Received");
    public static string NewPin => Get("NewPin");
    public static string CopyLink => Get("CopyLink");
    public static string LanOnly => Get("LanOnly");
    public static string Quit => Get("Quit");
    public static string Show => Get("Show");
    public static string From => Get("From");
    public static string NoNetworkText => Get("NoNetwork");
    public static string NothingYetText => Get("NothingYet");
}
