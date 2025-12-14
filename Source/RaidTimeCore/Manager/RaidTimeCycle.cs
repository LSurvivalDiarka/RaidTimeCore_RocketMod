using System;

namespace RaidTimeCore.Manager;

public enum RaidPhase
{
    Peace = 0,
    Raid = 1,
}

public readonly struct RaidCycleSnapshot
{
    public RaidCycleSnapshot(RaidPhase phase, TimeSpan timeRemaining)
    {
        Phase = phase;
        TimeRemaining = timeRemaining < TimeSpan.Zero ? TimeSpan.Zero : timeRemaining;
    }

    public RaidPhase Phase { get; }

    public TimeSpan TimeRemaining { get; }
}

public sealed class RaidTimeCycle
{
    private readonly DateTimeOffset _anchorUtc;
    private readonly TimeSpan _peace;
    private readonly TimeSpan _raid;
    private readonly TimeSpan _cycle;

    public RaidTimeCycle(DateTimeOffset anchorUtc, TimeSpan peace, TimeSpan raid)
    {
        if (peace <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(peace));
        if (raid <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(raid));

        _anchorUtc = anchorUtc;
        _peace = peace;
        _raid = raid;
        _cycle = peace + raid;
    }

    public RaidCycleSnapshot GetSnapshot(DateTimeOffset nowUtc)
    {
        var elapsed = nowUtc - _anchorUtc;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        var pos = TimeSpan.FromTicks(elapsed.Ticks % _cycle.Ticks);
        if (pos < _peace) return new RaidCycleSnapshot(RaidPhase.Peace, _peace - pos);
        return new RaidCycleSnapshot(RaidPhase.Raid, _cycle - pos);
    }

    public TimeSpan PeaceDuration => _peace;
    public TimeSpan RaidDuration => _raid;
}
