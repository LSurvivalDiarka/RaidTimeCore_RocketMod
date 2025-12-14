using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Rocket.API;
using Rocket.Unturned.Player;
using Steamworks;

using RocketLogger = Rocket.Core.Logging.Logger;

namespace RaidTimeCore.Patches;

internal static class RaidTimeDamageGuards
{
    public static bool ShouldAllowDamage(object[] __args)
    {
        var plugin = RaidTimeCorePlugin.Instance;
        if (plugin == null) return true;
        if (plugin.Manager?.IsRaidActive == true) return true;
        if (LooksLikeVehicleDamage(__args)) return true;

        var hasOwnership = TryExtractOwnership(__args, out var owner, out var group);
        if (hasOwnership && owner == 0UL && group == 0UL) return true;

        if (!TryExtractSteamId(__args, out var steamId))
            return hasOwnership && owner == 0UL && group == 0UL;

        var player = SDG.Unturned.PlayerTool.getPlayer(steamId);
        if (player == null) return true;

        UnturnedPlayer uPlayer;
        try { uPlayer = UnturnedPlayer.FromPlayer(player); }
        catch { return true; }

        var bypass = plugin.ConfigurationInstance.BypassPermission;
        return !string.IsNullOrWhiteSpace(bypass) && IRocketPlayerExtension.HasPermission(uPlayer, bypass);
    }

    private static bool LooksLikeVehicleDamage(object[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is null) continue;

            var t = a.GetType();
            var n = t.FullName ?? t.Name;

            if (n.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("InteractableVehicle", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }

    private static bool TryExtractOwnership(object[] args, out ulong owner, out ulong group)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is null) continue;

            var t = a.GetType();

            if (!TryGetUlongMember(t, a, "owner", out owner)) continue;
            if (!TryGetUlongMember(t, a, "group", out group)) continue;
            return true;
        }

        owner = 0;
        group = 0;
        return false;
    }

    private static bool TryGetUlongMember(Type type, object instance, string name, out ulong value)
    {
        var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (f != null && f.FieldType == typeof(ulong))
        {
            value = (ulong)f.GetValue(instance);
            return true;
        }

        var p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (p != null && p.PropertyType == typeof(ulong) && p.GetIndexParameters().Length == 0)
        {
            value = (ulong)p.GetValue(instance, null);
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryExtractSteamId(object[] args, out CSteamID steamId)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is null) continue;

            if (args[i] is CSteamID id)
            {
                steamId = id;
                return id.m_SteamID != 0;
            }

            if (args[i] is ulong steamIdUlong)
            {
                steamId = new CSteamID(steamIdUlong);
                return steamId.m_SteamID != 0;
            }

            if (args[i] is SDG.Unturned.SteamPlayer steamPlayer)
            {
                steamId = steamPlayer.playerID.steamID;
                return steamId.m_SteamID != 0;
            }

            if (args[i] is Rocket.Unturned.Player.UnturnedPlayer unturnedPlayer)
            {
                steamId = unturnedPlayer.CSteamID;
                return steamId.m_SteamID != 0;
            }
        }

        steamId = default;
        return false;
    }
}

internal static class RaidTimeDamagePatcher
{
    public static int Patch(Harmony harmony)
    {
        var prefix = new HarmonyMethod(AccessTools.Method(typeof(RaidTimeDamagePatcher), nameof(Prefix)));

        var count = 0;
        count += PatchType(harmony, AccessTools.TypeByName("SDG.Unturned.BarricadeManager"), prefix, "BarricadeManager");
        count += PatchType(harmony, AccessTools.TypeByName("SDG.Unturned.StructureManager"), prefix, "StructureManager");
        return count;
    }

    private static int PatchType(Harmony harmony, Type? type, HarmonyMethod prefix, string label)
    {
        if (type == null)
        {
            RocketLogger.Log($"[RaidTimeCore] Type not found: {label}. Damage guard disabled for this type.");
            return 0;
        }

        var targets = FindDamageTargets(type).ToArray();
        var count = 0;

        if (targets.Length == 0)
        {
            RocketLogger.Log($"[RaidTimeCore] No damage targets found for {label}. Damage guard disabled for this type.");
            return 0;
        }

#if DEBUG
        foreach (var t in targets)
            RocketLogger.Log($"[RaidTimeCore] Patch target: {label}.{t.Name}({string.Join(", ", t.GetParameters().Select(p => p.ParameterType.Name))})");
#endif

        foreach (var m in targets)
        {
            try
            {
                harmony.Patch(m, prefix: prefix);
                count++;
            }
            catch (Exception ex)
            {
                RocketLogger.LogException(ex);
                RocketLogger.Log($"[RaidTimeCore] Patch failed: {label} => {m}");
            }
        }

        return count;
    }

    private static IEnumerable<MethodInfo> FindDamageTargets(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var ask = methods.Where(m => string.Equals(m.Name, "askDamage", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (ask.Length != 0) return ask;
        return methods.Where(IsDamageLike);
    }

    private static bool IsDamageLike(MethodInfo m)
        => m.Name switch
        {
            var n when n.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0 => true,
            _ => false,
        };

    private static bool Prefix(object[] __args)
        => RaidTimeDamageGuards.ShouldAllowDamage(__args);
}
