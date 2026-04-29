# shaedy HUD Manager

A CounterStrikeSharp plugin that provides a centralized, priority-based overlay queue for CS2 plugins. Prevents multiple plugins from fighting over the center-screen HUD.

## How It Works

Other shaedy plugins (AFK, Bounty, Clutch, Flash, InstaDefuse, Kobe, MapChooser, Ranks) connect to this manager at runtime via reflection. The manager keeps lower-priority overlays queued per player, shows the highest-priority entry, and restores the next-best overlay automatically when a stronger one expires.

## Features

- Priority-based overlay system with 5 levels: Critical, High, Medium, Low, Background
- Per-player overlay queue with automatic restore of lower-priority overlays
- Same-priority overlays replace older entries, so countdown/status updates refresh cleanly
- Automatic expiration of stale overlays with active HUD clearing when nothing remains
- Sequence-based dispatch with a small client-side duration buffer to avoid white flashing from repeated redraws
- Thread-safe with internal locking
- Runtime connection via reflection - no compile-time dependency needed for other plugins

## Priority Levels

| Priority | Value | Used by |
|----------|-------|---------|
| Critical | 100 | Clutch, InstaDefuse |
| High | 75 | Bounty (warnings, claim flash) |
| Medium | 50 | AFK, Ranks (MMR float, kill streak, rank panel) |
| Low | 25 | Flash, Kobe |
| Background | 10 | MapChooser (next map badge, vote progress) |

A higher priority overlay is shown first, but lower-priority overlays remain queued and come back automatically once the stronger message expires. Same-priority overlays are replaced by newer ones.

## Installation

1. Download `shaedyHudManager-plugin.zip` from the latest release.
2. Extract the zip into your CounterStrikeSharp `plugins` directory: `csgo/addons/counterstrikesharp/plugins/`
3. After extracting, the plugin should be located at `csgo/addons/counterstrikesharp/plugins/shaedyHudManager/shaedyHudManager.dll`.
4. Restart your server.

No shared copy is required. Other shaedy plugins connect to this manager at runtime via reflection.

## For Plugin Developers

Include `HudManagerProxy.cs` in your plugin project. It uses reflection to connect to the already-loaded HudManager, so no compile-time dependency is needed:

```csharp
using ShaedyHudManager;

// Instead of:
// player.PrintToCenterHtml(html, 3);

// Use:
HudManagerProxy.Show(player.SteamID, html, HudManagerProxy.Priority.High, 3);
HudManagerProxy.Clear(player.SteamID);
```

Priority constants: `HudManagerProxy.Priority.Critical` (100), `.High` (75), `.Medium` (50), `.Low` (25), `.Background` (10).

If HudManager is not installed, calls silently no-op.

## Support

If you find a bug, have a feature request, or something isn't working as expected, feel free to [open an issue](../../issues). I'll take a look when I can.

Custom plugins are available on request, potentially for a small fee depending on scope. Reach out via an issue or at access@shaedy.de.

> Note: These repos may not always be super active since most of my work happens in private repositories. But issues and requests are still welcome.

## Donate

If you want to support my work: [ko-fi.com/shaedy](https://ko-fi.com/shaedy)
