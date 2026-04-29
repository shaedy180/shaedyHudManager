using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

[MinimumApiVersion(247)]
public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.3.2";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private readonly Dictionary<ulong, float> _visibleUntilTime = new();
    private const float DispatchInterval = 0.10f;
    private const int MinimumDisplaySeconds = 3;
    private const int MaximumDisplaySeconds = 10;

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(MaintainCenterHtmlWorkaround);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        AddTimer(DispatchInterval, DispatchHud, TimerFlags.REPEAT);

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

    private void MaintainCenterHtmlWorkaround()
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
    }

    private void DispatchHud()
    {
        MaintainCenterHtmlWorkaround();

        var messages = HudManager.CollectActive();
        var players = Utilities.GetPlayers();

        if (messages.Count == 0)
        {
            ForgetExpiredDisplays();
            return;
        }

        float now = Server.CurrentTime;
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot && player.Connected == PlayerConnectedState.Connected)
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var (steamId, html, duration, sequenceId) in messages)
        {
            bool sequenceChanged = !_lastShownSequence.TryGetValue(steamId, out var lastSeq) || lastSeq != sequenceId;
            if (!sequenceChanged)
                continue;

            if (playersBySteamId.TryGetValue(steamId, out var player))
            {
                // Center HTML is a single shared channel per player. Only the
                // manager should write to it directly so priority handling stays consistent.
                int clientDuration = Math.Clamp(duration, MinimumDisplaySeconds, MaximumDisplaySeconds);
                player.PrintToCenterHtml(html, clientDuration);
                _lastShownSequence[steamId] = sequenceId;
                _visibleUntilTime[steamId] = now + clientDuration;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
        ForgetExpiredDisplays(activeIds);
    }

    private void ForgetExpiredDisplays()
    {
        ForgetExpiredDisplays(new HashSet<ulong>());
    }

    private void ForgetExpiredDisplays(HashSet<ulong> activeIds)
    {
        float now = Server.CurrentTime;

        foreach (var sid in _visibleUntilTime.Keys.ToList())
        {
            if (activeIds.Contains(sid))
                continue;

            if (_visibleUntilTime.TryGetValue(sid, out var visibleUntil) && now < visibleUntil)
                continue;

            _visibleUntilTime.Remove(sid);
            _lastShownSequence.Remove(sid);
        }
    }
}
