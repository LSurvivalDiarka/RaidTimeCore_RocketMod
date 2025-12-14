using System;
using HarmonyLib;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using RaidTimeCore.Configuration;
using RaidTimeCore.Localization;
using RaidTimeCore.Manager;
using RaidTimeCore.Patches;
using UnityEngine;

using RocketLogger = Rocket.Core.Logging.Logger;

namespace RaidTimeCore;

public sealed class RaidTimeCorePlugin : RocketPlugin<RaidTimeCoreConfiguration>
{
    private Harmony? _harmony;

    internal static RaidTimeCorePlugin? Instance { get; private set; }

    internal RaidTimeManager? Manager { get; private set; }

    internal RaidTimeCoreConfiguration ConfigurationInstance => Configuration.Instance;

    protected override void Load()
    {
        Instance = this;

        MainThreadDispatcher.Initialize();

        EnsureCfg();

        Manager = new RaidTimeManager(ConfigurationInstance);
        Manager.Start();

        _harmony = new Harmony("lsurvival.raidtimecore");
        try
        {
            var patched = RaidTimeDamagePatcher.Patch(_harmony);
            RocketLogger.Log($"[RaidTimeCore] Harmony patched methods: {patched}");
        }
        catch (Exception ex)
        {
            RocketLogger.LogException(ex);
            RocketLogger.Log("[RaidTimeCore] Harmony patching failed. Damage guard disabled.");
        }

        RocketLogger.Log($"[RaidTimeCore] Loaded. Phase={Manager.CurrentPhase}, Peace={Manager.Cycle.PeaceDuration}, Raid={Manager.Cycle.RaidDuration}");

        if (ConfigurationInstance.AnnounceStateChanges)
            MainThreadDispatcher.Enqueue(() => BroadcastPhaseChange(Manager.CurrentPhase, Manager.Cycle));
    }

    protected override void Unload()
    {
        try
        {
            _harmony?.UnpatchAll("lsurvival.raidtimecore");
        }
        catch (Exception ex)
        {
            RocketLogger.LogException(ex);
        }

        try
        {
            Manager?.Stop();
            Manager?.Dispose();
        }
        catch (Exception ex)
        {
            RocketLogger.LogException(ex);
        }

        Manager = null;
        _harmony = null;
        Instance = null;

        MainThreadDispatcher.Shutdown();

        RocketLogger.Log("[RaidTimeCore] Unloaded.");
    }

    private void EnsureCfg()
    {
        var cfg = ConfigurationInstance;
        var dirty = false;

        if (string.IsNullOrWhiteSpace(cfg.Language)) { cfg.Language = "en"; dirty = true; }

        if (cfg.AnchorUnixSecondsUtc <= 0) { cfg.AnchorUnixSecondsUtc = 1735689600; dirty = true; }
        if (cfg.PeaceSeconds <= 0) { cfg.PeaceSeconds = 4 * 60 * 60; dirty = true; }
        if (cfg.RaidSeconds <= 0) { cfg.RaidSeconds = 45 * 60; dirty = true; }

        if (string.IsNullOrWhiteSpace(cfg.BypassPermission)) { cfg.BypassPermission = "lsurvival.raid.bypass"; dirty = true; }
        if (string.IsNullOrWhiteSpace(cfg.MessageRaidStart)) { cfg.MessageRaidStart = RaidTexts.GetRaidStartTemplate("en"); dirty = true; }
        if (string.IsNullOrWhiteSpace(cfg.MessageRaidEnd)) { cfg.MessageRaidEnd = RaidTexts.GetRaidEndTemplate("en"); dirty = true; }
        if (string.IsNullOrWhiteSpace(cfg.RaidIconUrl)) { cfg.RaidIconUrl = "https://i.imgur.com/8fK4h6G.png"; dirty = true; }
        if (string.IsNullOrWhiteSpace(cfg.PeaceIconUrl)) { cfg.PeaceIconUrl = "https://i.imgur.com/8r3JQqQ.png"; dirty = true; }

        if (!dirty) return;

        Configuration.Save();
        RocketLogger.Log("[RaidTimeCore] Config autogenerada/actualizada con valores por defecto.");
    }

    internal static void BroadcastPhaseChange(RaidPhase phase, RaidTimeCycle cycle)
    {
        var inst = Instance;
        if (inst == null) return;

        var cfg = inst.ConfigurationInstance;
        var lang = RaidTexts.NormalizeLang(cfg.Language);

        if (phase == RaidPhase.Raid)
        {
            var msg = cfg.UseCustomMessages
                ? (cfg.MessageRaidStart ?? string.Empty).Replace("{RAID_DURATION}", FormatDuration(cycle.RaidDuration))
                : RaidTexts.GetRaidStartAnnouncement(lang, FormatDuration(cycle.RaidDuration));
            SendChat(null, msg, Color.red, NormalizeIconUrl(cfg.RaidIconUrl), true);
            return;
        }

        SendChat(null,
            cfg.UseCustomMessages ? (cfg.MessageRaidEnd ?? string.Empty) : RaidTexts.GetRaidEndAnnouncement(lang),
            Color.green,
            NormalizeIconUrl(cfg.PeaceIconUrl),
            true);
    }

    internal static void SendChat(Rocket.Unturned.Player.UnturnedPlayer? toPlayer, string message, Color fallbackColor, string? iconUrl, bool useRichText)
    {
        try
        {
            SDG.Unturned.SteamPlayer? toSteamPlayer = null;
            if (toPlayer != null) toSteamPlayer = SDG.Unturned.PlayerTool.getSteamPlayer(toPlayer.CSteamID);

            SDG.Unturned.ChatManager.serverSendMessage(
                message,
                fallbackColor,
                default(SDG.Unturned.SteamPlayer),
                toSteamPlayer,
                SDG.Unturned.EChatMode.GLOBAL,
                iconUrl,
                useRichText);
        }
        catch (Exception ex)
        {
            RocketLogger.LogException(ex);
            if (toPlayer == null) UnturnedChat.Say(message, fallbackColor);
            else UnturnedChat.Say(toPlayer, message, fallbackColor);
        }
    }

    internal static string? NormalizeIconUrl(string? url) => string.IsNullOrWhiteSpace(url) ? null : url;

    private static string FormatDuration(TimeSpan t)
    {
        var totalHours = (int)t.TotalHours;
        if (totalHours > 0) return $"{totalHours:00}h {t.Minutes:00}m";
        return $"{t.Minutes:00}m {t.Seconds:00}s";
    }
}
