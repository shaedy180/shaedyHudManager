using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();

    public override void Load(bool hotReload)
    {
        AddTimer(0.3f, Tick, TimerFlags.REPEAT);
    }

    private void Tick()
    {
        var messages = HudManager.CollectActive();
        if (messages.Count == 0)
        {
            _lastShownSequence.Clear();
            return;
        }

        var players = Utilities.GetPlayers();
        foreach (var (steamId, html, duration, sequenceId) in messages)
        {
            if (_lastShownSequence.TryGetValue(steamId, out var lastSeq) && lastSeq == sequenceId)
                continue;

            var player = players.FirstOrDefault(p => p.SteamID == steamId && p.IsValid && !p.IsBot);
            if (player != null)
            {
                player.PrintToCenterHtml(html, duration);
                _lastShownSequence[steamId] = sequenceId;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (!activeIds.Contains(sid))
                _lastShownSequence.Remove(sid);
        }
    }
}