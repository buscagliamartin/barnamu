namespace BarnaMu.Web.Data;

/// <summary>
/// Strongly-typed configuration bound from the "BarnaMu" section of appsettings.json.
/// Restart the app to pick up changes (or use IOptionsMonitor for live reload).
/// </summary>
public class BarnaMuOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ServerName { get; set; } = "BarnaMu";
    public string ServerTagline { get; set; } = string.Empty;
    public string DiscordInviteUrl { get; set; } = string.Empty;

    public string ClientDownloadUrl { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = string.Empty;
    public string ClientChecksum { get; set; } = string.Empty;

    public string ConnectHost { get; set; } = string.Empty;
    public int ConnectPort { get; set; } = 44405;
    public string GameServerHost { get; set; } = string.Empty;
    public int[] GameServerPorts { get; set; } = Array.Empty<int>();

    /// <summary>Public base URL of the site (e.g. https://barnamu.com.ar), used to build
    /// absolute links in emails. Falls back to the current request host if empty.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public RatesOptions Rates { get; set; } = new();
    public VipOptions Vip { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public MapEntry[] Maps { get; set; } = Array.Empty<MapEntry>();

    public class RatesOptions
    {
        public int ExpNormal { get; set; }
        public int ExpVip { get; set; }
        public int MasterExpNormal { get; set; }
        public int MasterExpVip { get; set; }
        public int ZenNormal { get; set; }
        public int ZenVip { get; set; }
        public int DropNormal { get; set; }
        public int DropVip { get; set; }
        public int ExcellentDropNormal { get; set; }
        public int ExcellentDropVip { get; set; }
    }

    public class VipOptions
    {
        public string PriceText { get; set; } = string.Empty;
        public int DurationDays { get; set; } = 30;
        public string PayPalUrl { get; set; } = string.Empty;
        public string WhatsAppUrl { get; set; } = string.Empty;
        public string WhatsAppDisplay { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }

    public class SmtpOptions
    {
        /// <summary>When false, email features are disabled and the app logs reset links
        /// instead of sending them. Lets the site run before SMTP creds are configured.</summary>
        public bool Enabled { get; set; }
        public string Host { get; set; } = "smtp-relay.brevo.com";
        public int Port { get; set; } = 587;
        /// <summary>Brevo SMTP login (your Brevo account email, shown in SMTP &amp; API → SMTP).</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>Brevo SMTP key — the `xsmtpsib-...` value. Keep only in appsettings.Local.json.</summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>Verified sender address in Brevo.</summary>
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "BarnaMu";
    }

    public class MapEntry
    {
        public string Name { get; set; } = string.Empty;
        public int NormalLevel { get; set; }
        public int? VipLevel { get; set; }
    }
}
