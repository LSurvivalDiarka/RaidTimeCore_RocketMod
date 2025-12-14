using Rocket.API;

namespace RaidTimeCore.Configuration;

public sealed class RaidTimeCoreConfiguration : IRocketPluginConfiguration
{
    public string Language { get; set; } = "en";

    public bool UseCustomMessages { get; set; }

    public long AnchorUnixSecondsUtc { get; set; }

    public int PeaceSeconds { get; set; }

    public int RaidSeconds { get; set; }

    public string BypassPermission { get; set; } = "lsurvival.raid.bypass";

    public bool AnnounceStateChanges { get; set; }

    public string RaidIconUrl { get; set; } = "https://i.imgur.com/8fK4h6G.png";

    public string PeaceIconUrl { get; set; } = "https://i.imgur.com/8r3JQqQ.png";

    public string MessageRaidStart { get; set; } = "ALERT: Shields are down. Raid is ACTIVE for {RAID_DURATION}.";

    public string MessageRaidEnd { get; set; } = "Calm: Shields are up. Structure damage is disabled.";

    public void LoadDefaults()
    {
        Language = "en";
        UseCustomMessages = false;

        AnchorUnixSecondsUtc = 1735689600;

        PeaceSeconds = 4 * 60 * 60;
        RaidSeconds = 45 * 60;

        AnnounceStateChanges = true;
        BypassPermission = "lsurvival.raid.bypass";

        RaidIconUrl = "https://i.imgur.com/8fK4h6G.png";
        PeaceIconUrl = "https://i.imgur.com/8r3JQqQ.png";

        MessageRaidStart = "ALERT: Shields are down. Raid is ACTIVE for {RAID_DURATION}.";
        MessageRaidEnd = "Calm: Shields are up. Structure damage is disabled.";
    }
}
