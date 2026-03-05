# FitVR.Services

Contains concrete implementations of shared services.

Examples:
- PlayerProfileService
- FitnessTrackingService
- AudioService
- InputService
- SaveSystem

Rules:
- Services implement interfaces defined in FitVR.Core.
- Services may depend on Core.
- Services must NOT depend on Lobby or MiniGames.
- No scene-specific gameplay logic here.
