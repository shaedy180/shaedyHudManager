using CounterStrikeSharp.API.Core;

namespace ShaedyHudManager;

public enum HudPriority
{
    Critical = 100,
    High = 75,
    Medium = 50,
    Low = 25,
    Background = 10
}

public class HudEntry
{
    public required string Html { get; set; }
    public HudPriority Priority { get; set; }
    public double ExpiresAt { get; set; }
    public int DisplayDuration { get; set; }
}

public static class HudManager
{
    private static readonly Dictionary<ulong, HudEntry> _active = new();
    private static readonly object _lock = new();

    public static void Show(ulong steamId, string html, HudPriority priority, int displaySeconds)
    {
        lock (_lock)
        {
            if (_active.TryGetValue(steamId, out var existing) && existing.Priority >= priority)
                return;

            _active[steamId] = new HudEntry
            {
                Html = html,
                Priority = priority,
                ExpiresAt = DateTime.UtcNow.AddSeconds(displaySeconds).Ticks,
                DisplayDuration = displaySeconds
            };
        }
    }

    public static void Clear(ulong steamId)
    {
        lock (_lock)
        {
            _active.Remove(steamId);
        }
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _active.Clear();
        }
    }

    internal static List<(ulong steamId, string html, int duration)> CollectActive()
    {
        var result = new List<(ulong, string, int)>();
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;
            var expired = _active.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList();
            foreach (var k in expired) _active.Remove(k);

            foreach (var kv in _active)
                result.Add((kv.Key, kv.Value.Html, kv.Value.DisplayDuration));
        }
        return result;
    }
}
