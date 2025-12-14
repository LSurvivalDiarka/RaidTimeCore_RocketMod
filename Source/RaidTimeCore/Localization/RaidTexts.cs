using System;

namespace RaidTimeCore.Localization;

internal static class RaidTexts
{
    public static string NormalizeLang(string? lang)
        => Normalize(lang);

    private static string Normalize(string? lang)
        => (lang ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "" => "en",
            var v when v.StartsWith("es") => "es",
            var v when v.StartsWith("en") => "en",
            var v when v.StartsWith("ru") => "ru",
            var v when v.StartsWith("pt") => "br",
            var v when v.StartsWith("br") => "br",
            _ => "en",
        };

    public static string GetNotReady(string? lang)
        => Normalize(lang) switch
        {
            "es" => "RaidTimeCore aún no está listo.",
            "ru" => "RaidTimeCore еще не готов.",
            "br" => "RaidTimeCore ainda não está pronto.",
            _ => "RaidTimeCore is not ready yet.",
        };

    public static string GetRaidStartTemplate(string? lang)
        => Normalize(lang) switch
        {
            "es" => "ALARMA: Los escudos han caído. El raid está ACTIVO por {RAID_DURATION}.",
            "ru" => "ТРЕВОГА: Щиты отключены. РЕЙД АКТИВЕН на {RAID_DURATION}.",
            "br" => "ALERTA: Escudos desativados. O raid está ATIVO por {RAID_DURATION}.",
            _ => "ALERT: Shields are down. Raid is ACTIVE for {RAID_DURATION}.",
        };

    public static string GetRaidEndTemplate(string? lang)
        => Normalize(lang) switch
        {
            "es" => "Calma: Los escudos están activos. El daño a estructuras está desactivado.",
            "ru" => "Спокойно: Щиты активны. Урон по постройкам отключён.",
            "br" => "Calma: Escudos ativados. Dano a estruturas desativado.",
            _ => "Calm: Shields are up. Structure damage is disabled.",
        };

    public static string GetRaidStartAnnouncement(string? lang, string raidDuration)
        => GetRaidStartTemplate(lang).Replace("{RAID_DURATION}", raidDuration ?? string.Empty);

    public static string GetRaidEndAnnouncement(string? lang)
        => GetRaidEndTemplate(lang);

    public static string GetCommandPeace(string? lang, string remaining)
        => Normalize(lang) switch
        {
            "es" => $"<color=#00FF7A>Estado: PAZ</color>. Faltan <b>{remaining}</b> para el próximo raid.",
            "ru" => $"<color=#00FF7A>Статус: МИР</color>. До следующего рейда <b>{remaining}</b>.",
            "br" => $"<color=#00FF7A>Estado: PAZ</color>. Próximo raid em <b>{remaining}</b>.",
            _ => $"<color=#00FF7A>Status: PEACE</color>. Next raid in <b>{remaining}</b>.",
        };

    public static string GetCommandRaid(string? lang, string remaining)
        => Normalize(lang) switch
        {
            "es" => $"<color=#FF3B30>Estado: RAID ACTIVO</color>. Quedan <b>{remaining}</b> para que termine.",
            "ru" => $"<color=#FF3B30>Статус: РЕЙД</color>. Закончится через <b>{remaining}</b>.",
            "br" => $"<color=#FF3B30>Estado: RAID ATIVO</color>. Termina em <b>{remaining}</b>.",
            _ => $"<color=#FF3B30>Status: RAID ACTIVE</color>. Ends in <b>{remaining}</b>.",
        };

    public static string FormatHms(TimeSpan t)
        => $"{(int)t.TotalHours:00}h {t.Minutes:00}m {t.Seconds:00}s";
}
