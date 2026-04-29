using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.3.0";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private readonly Dictionary<ulong, float> _lastSentTime = new();
    private const float DispatchInterval = 0.25f;
    private const float ResendInterval = 0.25f;

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
            ClearPreviouslyShown(players, new HashSet<ulong>());
            _lastShownSequence.Clear();
            _lastSentTime.Clear();
            return;
        }

        float now = Server.CurrentTime;
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot)
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var (steamId, html, duration, sequenceId) in messages)
        {
            bool sequenceChanged = !_lastShownSequence.TryGetValue(steamId, out var lastSeq) || lastSeq != sequenceId;
            bool resendDue = !_lastSentTime.TryGetValue(steamId, out var lastTime) || (now - lastTime) >= ResendInterval;

            if (!sequenceChanged && !resendDue)
                continue;

            if (playersBySteamId.TryGetValue(steamId, out var player))
            {
                player.PrintToCenterHtml(html, duration);
                _lastShownSequence[steamId] = sequenceId;
                _lastSentTime[steamId] = now;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
        ClearPreviouslyShown(players, activeIds);

        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (!activeIds.Contains(sid))
                _lastShownSequence.Remove(sid);
        }
        foreach (var sid in _lastSentTime.Keys.ToList())
        {
            if (!activeIds.Contains(sid))
                _lastSentTime.Remove(sid);
        }
    }

    private void ClearPreviouslyShown(List<CCSPlayerController> players, HashSet<ulong> activeIds)
    {
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot)
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (activeIds.Contains(sid))
                continue;

            if (playersBySteamId.TryGetValue(sid, out var player))
                player.PrintToCenterHtml(" ", 1);
        }
    }
}
