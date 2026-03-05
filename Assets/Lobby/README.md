# FitVR.Lobby

Contains the Lobby module.

Includes:
- LobbyController
- Lobby UI
- Portals
- Menu navigation

Rules:
- Lobby may depend on Core and Services.
- Lobby must NOT reference MiniGames directly.
- Scene transitions must go through IGameFlowManager.
- Lobby must not load scenes directly.
