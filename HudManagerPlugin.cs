using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace ShaedyHudManager;

public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.3.1";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private const float DispatchInterval = 0.25f;
    private const int ClientDurationBufferSeconds = 1;
    private const int MinimumClientDurationSeconds = 2;
    private const string BrandMarker = "<!-- shaedy-brand -->";
    private const string BrandBadgeHtml = "<div style='text-align:center;font-family:Arial;font-size:10px;color:#888;letter-spacing:3px;margin-bottom:3px;'>shaedy</div>";

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
            return;
        }

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
                player.PrintToCenterHtml(ApplyBranding(html), clientDuration);
                _lastShownSequence[steamId] = sequenceId;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.steamId));
        ClearPreviouslyShown(players, activeIds);

        foreach (var sid in _lastShownSequence.Keys.ToList())
        {
            if (!activeIds.Contains(sid))
                _lastShownSequence.Remove(sid);
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

    private static string ApplyBranding(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Contains(BrandMarker, StringComparison.Ordinal))
            return html;

        var bodyStart = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (bodyStart >= 0)
        {
            var bodyEnd = html.IndexOf('>', bodyStart);
            if (bodyEnd >= 0)
            {
                return html.Insert(bodyEnd + 1, BrandMarker + BrandBadgeHtml);
            }
        }

        return "<html><body style='margin:0;padding:0;'>" + BrandMarker + BrandBadgeHtml + html + "</body></html>";
    }
}
