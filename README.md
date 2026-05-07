# FitVR
**Unity 6.3 LTS (6000.3.8f1)**

### Setup Instructions
1. **Clone and Link**:
   * Clone the repository: `git clone <repository-url>`
   * If you have existing files, run `git init` and `git remote add origin <repository-url>`.
2. **New Branch**:
   * Always switch to a new branch before working: `git checkout -b feature-name`.
3. **Architectural Dependencies**:
   * **Allowed**: `MiniGames` → `Core`
   * **Allowed**: `Lobby` → `Core`
   * **Allowed**: `Services` → `Core`
   * **Prohibited**: `Core` → `!Core` (The Core module must never reference any other module).

### Development Standards
* **Performance**: Maintain high frame rates and low latency for VR comfort.
* **Stability**: Ensure features support multiple simultaneous users without crashes.
* **Security**: Implement secure authentication and data encryption for all new components.