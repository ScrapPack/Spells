# Spells

A 4-player party arena brawler built in Unity. Players pick spellcaster classes and fight in 2D arenas. After each round, losers draft power cards with positive and negative effects. First to the win threshold takes the match.

---

## Requirements

| Tool | Version |
|------|---------|
| **Unity Editor** | **6000.3.11f1** (Unity 6.0) |
| **Render Pipeline** | Universal Render Pipeline (URP) 17.3 |
| **Input System** | com.unity.inputsystem 1.19.0 |

> Unity Hub will prompt you to install the correct editor version when you open the project. Do not use a different version — Unity 6 made breaking changes to Rigidbody2D (`linearVelocity` replaces `velocity`) that older versions do not support.

---

## Opening the Project

1. Install **Unity Hub** from [unity.com/download](https://unity.com/download)
2. In Unity Hub, install editor version **6000.3.11f1**
   - Under *Installs → Add* → select version 6000.3.11f1
   - Include the **Windows / Mac Build Support** module for your platform
3. Clone this repository:
   ```
   git clone <repo-url>
   cd Spells
   ```
4. In Unity Hub → *Projects → Add → Add project from disk* → select the **`Spells/Spells/`** folder (the inner one that contains `Assets/`)
5. Open the project — Unity will import assets on first launch (may take a few minutes)

---

## Required Packages

All packages are listed in `Spells/Packages/manifest.json` and install automatically when you open the project. The key ones are:

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering — required for all materials |
| `com.unity.inputsystem` | 1.19.0 | New Input System — player input, control schemes |
| `com.unity.test-framework` | 1.6.0 | Edit mode + play mode tests |
| `com.unity.2d.sprite` | 1.0.0 | 2D sprite tools |
| `com.unity.cinemachine` | 3.1.6 | Camera utilities (optional/unused at runtime) |
| `com.unity.timeline` | 1.8.11 | Timeline (optional/unused at runtime) |

If packages fail to resolve, open **Window → Package Manager**, select *Packages: In Project*, and click **Refresh**.

---

## Running the Game

1. Open the **`CombatTestArena`** scene:
   `Assets/_Project/Scenes/CombatTestArena.unity`
2. Press **Play**
3. Two players spawn automatically — no scene setup required

### Controls

| Action | Player 1 (WASD) | Player 2 (Arrows) | Gamepad |
|--------|----------------|-------------------|---------|
| Move | WASD | Arrow keys | Left stick |
| Jump | Space | Right Shift | South (A/Cross) |
| Shoot | F | Enter | East (B/Circle) |
| Dash | Left Shift | Left Shift | South (A/Cross) |
| Fast fall | S (while airborne) | Down arrow (airborne) | Down + left stick |

Shoot direction is controlled by your movement input (WASD / left stick) or the right stick on gamepad.

### Match Flow

- Players fight until one dies → round ends after a short delay
- The losing player automatically receives a random power card
- A new round starts — first player to reach the win threshold wins the match
- Win count and card picks are logged to the Unity Console

---

## Project Structure

```
Spells/                     # Repo root
├── CLAUDE.md               # Architecture reference for Claude
├── GDD.md                  # Full game design document
├── Specs/                  # Feature specs
└── Spells/                 # Unity project root
    └── Assets/_Project/    # All game content
        ├── Scripts/        # ~120 C# files
        ├── Prefabs/        # Player, Projectile, Environment prefabs
        ├── Data/           # ScriptableObject assets
        ├── Scenes/         # Unity scenes
        └── Input/          # Input Action Asset
```

See [CLAUDE.md](CLAUDE.md) for a full architecture breakdown.

---

## Common Issues

**"Script has compile errors"** — Make sure you are on Unity **6000.3.11f1**. The codebase uses `Rigidbody2D.linearVelocity` which does not exist in Unity 2022/2023.

**Players not responding to input** — The Input Action Asset must have the `KeyboardWASD` and `KeyboardArrows` control schemes defined. Check `Assets/_Project/Input/PlayerInputActions.inputactions`.

**Blank/pink materials** — The project uses URP. If materials appear pink, go to **Edit → Rendering → Render Pipeline → Upgrade Project Materials to URP**.

**Players spawning but not moving** — Ensure the `PlayerCharacter` prefab (not a projectile prefab) is assigned to both player slots on the `BoxArenaBuilder` component in the scene.
