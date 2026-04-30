namespace ShaedyHudManager;

public sealed class HudManagerConfig
{
    public bool EnableGameRestartHtmlWorkaround { get; set; } = false;
    public bool GameRestartWorkaroundOnlyWhileHudActive { get; set; } = true;
    public bool EnableDebugLogging { get; set; } = false;
}
