namespace ShaedyHudManager;

public sealed class HudManagerConfig
{
    public bool EnableGameRestartHtmlWorkaround { get; set; } = false;
    public bool GameRestartWorkaroundOnlyWhileHudActive { get; set; } = true;
    public float ProtectedRepaintIntervalSeconds { get; set; } = 0.35f;
    public bool EnableDebugLogging { get; set; } = false;

    public void Normalize()
    {
        ProtectedRepaintIntervalSeconds = Math.Clamp(ProtectedRepaintIntervalSeconds, 0.20f, 1.50f);
    }
}
