using System;
using System.Threading;
using System.Threading.Tasks;
using RaidTimeCore.Configuration;

namespace RaidTimeCore.Manager;

public sealed class RaidTimeManager : IDisposable
{
    private readonly RaidTimeCoreConfiguration _cfg;
    private readonly RaidTimeCycle _cycle;
    private readonly CancellationTokenSource _cts = new();

    private volatile RaidPhase _phase;

    public RaidTimeManager(RaidTimeCoreConfiguration config)
    {
        _cfg = config;

        var anchor = DateTimeOffset.FromUnixTimeSeconds(config.AnchorUnixSecondsUtc);
        var peace = TimeSpan.FromSeconds(config.PeaceSeconds);
        var raid = TimeSpan.FromSeconds(config.RaidSeconds);

        _cycle = new RaidTimeCycle(anchor, peace, raid);
        _phase = _cycle.GetSnapshot(DateTimeOffset.UtcNow).Phase;
    }

    public RaidPhase CurrentPhase => _phase;

    public bool IsRaidActive => _phase == RaidPhase.Raid;

    public RaidTimeCycle Cycle => _cycle;

    public void Start() => _ = Task.Run(() => LoopAsync(_cts.Token));

    public void Stop() => _cts.Cancel();

    public RaidCycleSnapshot GetSnapshotNowUtc() => _cycle.GetSnapshot(DateTimeOffset.UtcNow);

    private async Task LoopAsync(CancellationToken token)
    {
        var last = _phase;

        while (!token.IsCancellationRequested)
        {
            var snap = _cycle.GetSnapshot(DateTimeOffset.UtcNow);
            _phase = snap.Phase;

            if (snap.Phase != last)
            {
                last = snap.Phase;
                await OnPhaseChangedAsync(snap.Phase);
            }

            try
            {
                await Task.Delay(1000, token);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    private Task OnPhaseChangedAsync(RaidPhase phase)
    {
        if (!_cfg.AnnounceStateChanges) return Task.CompletedTask;
        MainThreadDispatcher.Enqueue(() => RaidTimeCorePlugin.BroadcastPhaseChange(phase, _cycle));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
