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

    public RatesOptions Rates { get; set; } = new();
    public VipOptions Vip { get; set; } = new();
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

    public class MapEntry
    {
        public string Name { get; set; } = string.Empty;
        public int NormalLevel { get; set; }
        public int? VipLevel { get; set; }
    }
}
