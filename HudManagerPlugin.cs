using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using System.Text.Json;

namespace ShaedyHudManager;

[MinimumApiVersion(247)]
public class HudManagerPlugin : BasePlugin
{
    public override string ModuleName => "shaedy HUD Manager";
    public override string ModuleVersion => "1.4.1";
    public override string ModuleAuthor => "shaedy";

    private readonly Dictionary<ulong, long> _lastShownSequence = new();
    private readonly Dictionary<ulong, float> _visibleUntilTime = new();
    private const float DispatchInterval = 0.10f;
    private const int MinimumDisplaySeconds = 3;
    private const int MaximumDisplaySeconds = 10;

    public HudManagerConfig Config { get; private set; } = new();

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;
    private string _configFilePath = string.Empty;

    public override void Load(bool hotReload)
    {
        _configFilePath = Path.Combine(ModuleDirectory, "shaedyHudManagerConfig.json");
        LoadConfig();
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        AddTimer(DispatchInterval, DispatchHud, TimerFlags.REPEAT);

        if (hotReload)
            InitializeGameRules();
    }

    private void OnMapStart(string mapName)
    {
        _gameRules = null;
        _gameRulesInitialized = false;
        _lastShownSequence.Clear();
        _visibleUntilTime.Clear();
    }

    private void InitializeGameRules()
    {
        if (_gameRulesInitialized) return;

        var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        _gameRules = gameRulesProxy?.GameRules;
        _gameRulesInitialized = _gameRules != null;
    }

    private void LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            Config = new HudManagerConfig();
            SaveConfig();
            LogDebug("Created default HUD manager config.");
            return;
        }

        try
        {
            Config = JsonSerializer.Deserialize<HudManagerConfig>(File.ReadAllText(_configFilePath)) ?? new HudManagerConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[shaedyHudManager] Failed to load config, using defaults. " + ex.Message);
            Config = new HudManagerConfig();
        }

        SaveConfig();

        if (Config.EnableDebugLogging)
        {
            Console.WriteLine(
                "[shaedyHudManager] Debug enabled. GameRestart workaround="
                + Config.EnableGameRestartHtmlWorkaround
                + ", onlyWhileHudActive="
                + Config.GameRestartWorkaroundOnlyWhileHudActive);
        }
    }

    private void SaveConfig()
    {
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void ApplyGameRestartHtmlWorkaroundIfNeeded(bool hasActiveHud)
    {
        if (!Config.EnableGameRestartHtmlWorkaround)
            return;

        if (Config.GameRestartWorkaroundOnlyWhileHudActive && !hasActiveHud)
            return;

        if (!_gameRulesInitialized)
        {
            InitializeGameRules();
            if (!_gameRulesInitialized)
                return;
        }

        if (_gameRules == null)
            return;

        // This workaround can help with CenterHTML flicker, but it may also
        // provoke native restart text such as "Game restarting in 1...".
        // Keep it opt-in and apply it only around actual HUD paints unless
        // the admin explicitly disables the active-HUD guard.
        _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
        LogDebug("Applied GameRestart HTML workaround.");
    }

    private void DispatchHud()
    {
        var messages = HudManager.CollectActive();
        var players = Utilities.GetPlayers();

        if (Config.EnableGameRestartHtmlWorkaround && !Config.GameRestartWorkaroundOnlyWhileHudActive)
            ApplyGameRestartHtmlWorkaroundIfNeeded(hasActiveHud: messages.Count > 0);

        if (messages.Count == 0)
        {
            ForgetExpiredDisplays();
            return;
        }

        float now = Server.CurrentTime;
        var playersBySteamId = players
            .Where(player => player.IsValid && !player.IsBot && IsConnected(player))
            .ToDictionary(player => player.SteamID, player => player);

        foreach (var message in messages)
        {
            bool sequenceChanged = !_lastShownSequence.TryGetValue(message.SteamId, out var lastSeq) || lastSeq != message.SequenceId;

            if (message.NativeCenterBusy && sequenceChanged)
            {
                LogDebug("Delayed HUD because native center channel is busy for SteamID " + message.SteamId + ".");
                continue;
            }

            if (!sequenceChanged)
                continue;

            if (playersBySteamId.TryGetValue(message.SteamId, out var player))
            {
                if (Config.EnableGameRestartHtmlWorkaround && Config.GameRestartWorkaroundOnlyWhileHudActive)
                    ApplyGameRestartHtmlWorkaroundIfNeeded(hasActiveHud: true);

                // Center HTML is a single shared channel per player. Only the
                // manager should write to it directly so priority handling stays consistent.
                int clientDuration = Math.Clamp(message.Duration, MinimumDisplaySeconds, MaximumDisplaySeconds);
                player.PrintToCenterHtml(message.Html, clientDuration);
                HudManager.MarkPainted(message.SteamId, message.SequenceId);
                _lastShownSequence[message.SteamId] = message.SequenceId;
                _visibleUntilTime[message.SteamId] = now + clientDuration;
            }
        }

        var activeIds = new HashSet<ulong>(messages.Select(m => m.SteamId));
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

    private void LogDebug(string message)
    {
        if (!Config.EnableDebugLogging)
            return;

        Console.WriteLine("[shaedyHudManager] " + message);
    }

    private static bool IsConnected(CCSPlayerController player)
    {
        var stateName = player.Connected.ToString();
        return stateName == "Connected" || stateName == "PlayerConnected";
    }
}
