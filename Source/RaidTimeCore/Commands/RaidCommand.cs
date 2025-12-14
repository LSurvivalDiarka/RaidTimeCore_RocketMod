using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using RaidTimeCore.Localization;
using RaidTimeCore.Manager;
using UnityEngine;

namespace RaidTimeCore.Commands;

public sealed class RaidCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "raid";
    public string Help => "Muestra el estado del ciclo (PAZ/RAID) y el tiempo restante.";
    public string Syntax => "/raid";
    public List<string> Aliases => new() { "tiempo", "nextraid" };
    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var plugin = RaidTimeCorePlugin.Instance;
        var mgr = plugin?.Manager;
        var lang = RaidTexts.NormalizeLang(plugin?.ConfigurationInstance.Language);

        if (mgr == null)
        {
            if (caller is UnturnedPlayer u)
                RaidTimeCorePlugin.SendChat(u, RaidTexts.GetNotReady(lang), Color.white, null, true);
            return;
        }

        var toPlayer = caller as UnturnedPlayer;
        var snap = mgr.GetSnapshotNowUtc();
        var rem = RaidTexts.FormatHms(snap.TimeRemaining);

        if (snap.Phase == RaidPhase.Peace)
        {
            var icon = RaidTimeCorePlugin.NormalizeIconUrl(plugin!.ConfigurationInstance.PeaceIconUrl);
            RaidTimeCorePlugin.SendChat(toPlayer,
                RaidTexts.GetCommandPeace(lang, rem),
                Color.green,
                icon,
                true);
            return;
        }

        var raidIcon = RaidTimeCorePlugin.NormalizeIconUrl(plugin!.ConfigurationInstance.RaidIconUrl);
        RaidTimeCorePlugin.SendChat(toPlayer,
            RaidTexts.GetCommandRaid(lang, rem),
            Color.red,
            raidIcon,
            true);
    }
}
