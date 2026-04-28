using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "shaedy";

    public override void Load(bool hotReload)
    {
        AddTimer(0.3f, Tick, TimerFlags.REPEAT);
    }

    private void Tick()
    {
        var messages = HudManager.CollectActive();
        if (messages.Count == 0) return;

        var players = Utilities.GetPlayers();
        foreach (var (steamId, html, duration) in messages)
        {
            var player = players.FirstOrDefault(p => p.SteamID == steamId && p.IsValid && !p.IsBot);
            if (player != null)
                player.PrintToCenterHtml(html, duration);
        }
    }
}
