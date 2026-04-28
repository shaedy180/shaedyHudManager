# shaedy HUD Manager

A CounterStrikeSharp plugin that provides a centralized, priority-based overlay queue for CS2 plugins. Prevents multiple plugins from fighting over the center-screen HUD.

## How It Works

Other shaedy plugins (AFK, Bounty, Clutch, Flash, InstaDefuse, Kobe, MapChooser, Ranks) connect to this manager at runtime via reflection. The manager shows the highest-priority message for each player, replacing lower-priority ones automatically.

## Features

- Priority-based overlay system with 5 levels: Critical, High, Medium, Low, Background
- Per-player overlay queue so each player sees the most relevant message
- Automatic expiration of stale overlays
- Single 300ms tick that dispatches to all players at once
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

A higher priority overlay always replaces a lower priority one. Same-priority overlays are replaced by newer ones.

## Installation

1. Download `shaedyHudManager-plugin.zip` from the latest release.
2. Extract the zip into your CounterStrikeSharp `plugins` directory. You should have `csgo/addons/counterstrikesharp/plugins/shaedyHudManager/shaedyHudManager.dll`.
3. Restart your server.

The `shared/` copy is no longer required. Other shaedy plugins connect to this manager at runtime via reflection.

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