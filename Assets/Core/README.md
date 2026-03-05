# FitVR.Core

This assembly contains the foundational systems of the application.

Includes:
- Interfaces (contracts only)
- GameFlow management
- Scene loading abstraction
- App State Machine
- GameContext (runtime shared data)
- ServiceLocator

Rules:
- Core must NOT depend on Lobby or MiniGames.
- Core defines interfaces, but does not depend on implementations.
- No gameplay logic belongs here.
