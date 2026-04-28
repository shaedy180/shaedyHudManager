using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private readonly Dictionary<ulong, float> _lastSentTime = new();
    private const float ResendInterval = 1.5f;

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;

    public override void Load(bool hotReload)
    {
        AddTimer(0.1f, Tick, TimerFlags.REPEAT);
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
        if (messages.Count == 0)
        {
            _lastShownSequence.Clear();
            _lastSentTime.Clear();
            return;
        }

        float now = Server.CurrentTime;
        var players = Utilities.GetPlayers();

        foreach (var (steamId, html, duration, sequenceId) in messages)
        {
            bool sequenceChanged = !_lastShownSequence.TryGetValue(steamId, out var lastSeq) || lastSeq != sequenceId;
            bool resendDue = !_lastSentTime.TryGetValue(steamId, out var lastTime) || (now - lastTime) >= ResendInterval;

            if (!sequenceChanged && !resendDue)
                continue;

            var player = players.FirstOrDefault(p => p.SteamID == steamId && p.IsValid && !p.IsBot);
            if (player != null)
            {
                player.PrintToCenterHtml(html, duration);
                _lastShownSequence[steamId] = sequenceId;
                _lastSentTime[steamId] = now;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
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
}