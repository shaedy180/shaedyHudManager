using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.3.2";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private readonly Dictionary<ulong, float> _visibleUntilTime = new();
    private const float DispatchInterval = 0.25f;
    private const int ClientDurationBufferSeconds = 1;
    private const int MinimumClientDurationSeconds = 2;

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;

    public override void Load(bool hotReload)
    {
        AddTimer(DispatchInterval, Tick, TimerFlags.REPEAT);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        if (hotReload)
            InitializeGameRules();
    }

    private void OnMapStart(string mapName)
    {
        _gameRules = null;
        _gameRulesInitialized = false;
    }

    private void InitializeGameRules()
    {
        if (_gameRulesInitialized) return;

        var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        _gameRules = gameRulesProxy?.GameRules;
        _gameRulesInitialized = _gameRules != null;
    }

    private void Tick()
    {
        if (!_gameRulesInitialized)
        {
            InitializeGameRules();
            if (!_gameRulesInitialized) return;
        }

        if (_gameRules != null)
        {
            _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
        }

        var messages = HudManager.CollectActive();
        var players = Utilities.GetPlayers();

        if (messages.Count == 0)
        {
            ClearExpiredDisplays(players, new HashSet<ulong>());
            return;
        }

        float now = Server.CurrentTime;
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot)
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var (steamId, html, duration, sequenceId) in messages)
        {
            bool sequenceChanged = !_lastShownSequence.TryGetValue(steamId, out var lastSeq) || lastSeq != sequenceId;
            if (!sequenceChanged)
                continue;

            if (playersBySteamId.TryGetValue(steamId, out var player))
            {
                int clientDuration = Math.Max(MinimumClientDurationSeconds, duration + ClientDurationBufferSeconds);
                player.PrintToCenterHtml(html, clientDuration);
                _lastShownSequence[steamId] = sequenceId;
                _visibleUntilTime[steamId] = now + clientDuration;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
        ClearExpiredDisplays(players, activeIds);

        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (!activeIds.Contains(sid) && !_visibleUntilTime.ContainsKey(sid))
                _lastShownSequence.Remove(sid);
        }
    }

    private void ClearExpiredDisplays(List<CCSPlayerController> players, HashSet<ulong> activeIds)
    {
        float now = Server.CurrentTime;
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot)
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (activeIds.Contains(sid))
                continue;

            if (_visibleUntilTime.TryGetValue(sid, out var visibleUntil) && now < visibleUntil)
                continue;

            if (playersBySteamId.TryGetValue(sid, out var player))
                player.PrintToCenterHtml(" ", 1);

            _lastShownSequence.Remove(sid);
            _visibleUntilTime.Remove(sid);
        }

        foreach (var sid in _visibleUntilTime.Keys.ToList())
        {
            if (!_lastShownSequence.ContainsKey(sid))
                _visibleUntilTime.Remove(sid);
        }
    }
}
