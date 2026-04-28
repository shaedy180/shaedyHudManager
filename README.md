# shaedy HUD Manager

A CounterStrikeSharp plugin that provides a centralized, priority-based overlay queue for CS2 plugins. Prevents multiple plugins from fighting over the center-screen HUD.

## Features

- Priority-based overlay system with 5 levels: Critical, High, Medium, Low, Background
- Per-player overlay queue so each player sees the most relevant message
- Automatic expiration of stale overlays
- Single 300ms tick that dispatches to all players at once
- Thread-safe with internal locking
- Used by shaedyAFK, shaedyBounty, shaedyClutch, shaedyFlash, shaedyInstadefuse, shaedyKobe, shaedyMapChooser, and shaedyRanks

## Priority Levels

| Priority | Value | Used by |
|----------|-------|---------|
| Critical | 100 | shaedyClutch, shaedyInstadefuse |
| High | 75 | shaedyBounty (bounty warnings, claim flash) |
| Medium | 50 | shaedyAFK, shaedyRanks (MMR float, kill streak, rank panel) |
| Low | 25 | shaedyFlash, shaedyKobe |
| Background | 10 | shaedyMapChooser (next map badge, vote progress) |

A higher priority overlay always replaces a lower priority one. Same-priority overlays are replaced by newer ones.

## Installation

1. Drop the shaedyHudManager plugin folder into your CounterStrikeSharp `plugins` directory.
2. Restart your server.

No configuration is needed. Other plugins that depend on this will automatically register with the HUD manager.

## For Plugin Developers

Add this project as a project reference to your plugin. Then use `HudManager.Show(steamId, html, priority, seconds)` instead of `player.PrintToCenterHtml(html, seconds)`.

```csharp
using ShaedyHudManager;

// Instead of:
// player.PrintToCenterHtml(html, 3);

// Use:
HudManager.Show(player.SteamID, html, HudPriority.High, 3);
```

## Support

If you find a bug, have a feature request, or something isn't working as expected, feel free to [open an issue](../../issues). I'll take a look when I can.

Custom plugins are available on request, potentially for a small fee depending on scope. Reach out via an issue or at access@shaedy.de.

> Note: These repos may not always be super active since most of my work happens in private repositories. But issues and requests are still welcome.

## Donate

If you want to support my work: [ko-fi.com/shaedy](https://ko-fi.com/shaedy)
