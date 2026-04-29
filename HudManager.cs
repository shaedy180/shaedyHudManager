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
    public long ExpiresAt { get; set; }
    public int DisplayDuration { get; set; }
    public long SequenceId { get; set; }
}

public static class HudManager
{
    private static readonly Dictionary<ulong, List<HudEntry>> _active = new();
    private static readonly object _lock = new();
    private static long _sequenceCounter;

    public static void Show(ulong steamId, string html, HudPriority priority, int displaySeconds)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;

            if (!_active.TryGetValue(steamId, out var entries))
            {
                entries = new List<HudEntry>();
                _active[steamId] = entries;
            }

            RemoveExpired(entries, now);

            // Same-priority overlays should behave as replace-in-place, while
            // lower-priority overlays stay queued behind stronger ones.
            entries.RemoveAll(entry => entry.Priority == priority);

            entries.Add(new HudEntry
            {
                Html = html,
                Priority = priority,
                ExpiresAt = DateTime.UtcNow.AddSeconds(displaySeconds).Ticks,
                DisplayDuration = displaySeconds,
                SequenceId = System.Threading.Interlocked.Increment(ref _sequenceCounter)
            });
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

    internal static List<(ulong steamId, string html, int duration, long sequenceId)> CollectActive()
    {
        var result = new List<(ulong, string, int, long)>();
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;

            foreach (var (steamId, entries) in _active.ToList())
            {
                RemoveExpired(entries, now);
                if (entries.Count == 0)
                {
                    _active.Remove(steamId);
                    continue;
                }

                var top = entries
                    .OrderByDescending(entry => entry.Priority)
                    .ThenByDescending(entry => entry.SequenceId)
                    .First();

                var remainingSeconds = Math.Max(1, (int)Math.Ceiling(TimeSpan.FromTicks(top.ExpiresAt - now).TotalSeconds));
                result.Add((steamId, top.Html, remainingSeconds, top.SequenceId));
            }
        }
        return result;
    }

    private static void RemoveExpired(List<HudEntry> entries, long now)
    {
        entries.RemoveAll(entry => entry.ExpiresAt <= now);
    }
}
