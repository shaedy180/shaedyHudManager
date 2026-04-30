namespace ShaedyHudManager;

internal sealed record ActiveHudState(
    ulong SteamId,
    string Html,
    int Duration,
    long SequenceId,
    bool NativeCenterBusy);
