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
    public long CreatedAt { get; set; }
    public float DurationSeconds { get; set; }
    public long? FirstPaintedAt { get; set; }
    public long? ExpiresAt { get; set; }
    public long SequenceId { get; set; }
}

public static class HudManager
{
    private const int MinimumDisplaySeconds = 3;
    private const int MaximumDisplaySeconds = 10;
    private const float MaximumBusySeconds = 10.0f;

    private static readonly Dictionary<ulong, List<HudEntry>> _active = new();
    private static readonly Dictionary<ulong, long> _nativeBusyUntil = new();
    private static readonly object _lock = new();
    private static long _sequenceCounter;

    public static void Show(ulong steamId, string html, HudPriority priority, int displaySeconds)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;
            int seconds = Math.Clamp(displaySeconds, MinimumDisplaySeconds, MaximumDisplaySeconds);

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
                CreatedAt = now,
                DurationSeconds = seconds,
                FirstPaintedAt = null,
                ExpiresAt = null,
                SequenceId = System.Threading.Interlocked.Increment(ref _sequenceCounter)
            });
        }
    }

    public static void MarkPainted(ulong steamId, long sequenceId)
    {
        lock (_lock)
        {
            if (!_active.TryGetValue(steamId, out var entries))
                return;

            var entry = entries.FirstOrDefault(item => item.SequenceId == sequenceId);
            if (entry == null || entry.FirstPaintedAt.HasValue)
                return;

            var now = DateTime.UtcNow.Ticks;
            entry.FirstPaintedAt = now;
            entry.ExpiresAt = DateTime.UtcNow.AddSeconds(entry.DurationSeconds).Ticks;
        }
    }

    public static void Clear(ulong steamId)
    {
        lock (_lock)
        {
            _active.Remove(steamId);
            _nativeBusyUntil.Remove(steamId);
        }
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _active.Clear();
            _nativeBusyUntil.Clear();
        }
    }

    public static void NotifyNativeCenterBusy(ulong steamId, float seconds)
    {
        if (steamId == 0)
            return;

        var clampedSeconds = Math.Clamp(seconds, 0.0f, MaximumBusySeconds);
        if (clampedSeconds <= 0.0f)
            return;

        lock (_lock)
        {
            var busyUntil = DateTime.UtcNow.AddSeconds(clampedSeconds).Ticks;

            if (_nativeBusyUntil.TryGetValue(steamId, out var existing) && existing > busyUntil)
                return;

            _nativeBusyUntil[steamId] = busyUntil;
        }
    }

    internal static List<ActiveHudState> CollectActive()
    {
        var result = new List<ActiveHudState>();
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;
            RemoveExpiredBusyWindows(now);

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

                var remainingSeconds = top.ExpiresAt.HasValue
                    ? Math.Max(1, (int)Math.Ceiling(TimeSpan.FromTicks(top.ExpiresAt.Value - now).TotalSeconds))
                    : Math.Max(1, (int)Math.Ceiling(top.DurationSeconds));
                var nativeCenterBusy = _nativeBusyUntil.TryGetValue(steamId, out var busyUntil) && busyUntil > now;

                result.Add(new ActiveHudState(
                    steamId,
                    top.Html,
                    remainingSeconds,
                    top.SequenceId,
                    nativeCenterBusy));
            }
        }
        return result;
    }

    private static void RemoveExpired(List<HudEntry> entries, long now)
    {
        entries.RemoveAll(entry => entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= now);
    }

    private static void RemoveExpiredBusyWindows(long now)
    {
        foreach (var steamId in _nativeBusyUntil
                     .Where(entry => entry.Value <= now)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            _nativeBusyUntil.Remove(steamId);
        }
    }
}
