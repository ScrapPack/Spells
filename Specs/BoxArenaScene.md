# Box Arena Scene — Requirements Spec

## Goal

A minimal Unity scene where two players can spawn inside an enclosed rectangular arena and play through a complete game cycle: **Round → Draft → Round → … → Match End**. No procedural generation, no PvE, no biomes. Purpose: validate the full match loop end-to-end with the simplest possible environment.

---

## 1. Arena Geometry

**A single static GameObject** ("BoxArena") composed of five primitive colliders:

| Part | Shape | Role |
|------|-------|------|
| Floor | Box Collider 2D | Ground players stand on |
| Left Wall | Box Collider 2D | Bounds / wall-jump surface |
| Right Wall | Box Collider 2D | Bounds / wall-jump surface |
| Ceiling | Box Collider 2D | Top bound (prevents escape) |
| Kill Zone | Box Trigger 2D | Below the floor — triggers player death on fall-through |

**Suggested dimensions:** 24 units wide × 14 units tall. Floor at Y=0, ceiling at Y=14, walls at X=±12.

All colliders use `PhysicsMaterial2D` with zero friction on walls (to match the wall-slide behavior in `WallSlidingState`). Floor uses default friction.

All five parts use **Layer: Ground** so `PhysicsCheck.cs` detects them correctly.

---

## 2. Spawn Points

Two child `Transform` objects on the BoxArena GameObject, tagged **SpawnPoint**:

| Name | Position |
|------|----------|
| SpawnPoint_P1 | (-5, 1, 0) |
| SpawnPoint_P2 | ( 5, 1, 0) |

These are passed to `PlayerSpawnManager.SetSpawnPoints()` via the inspector `spawnPoints[]` array. Spawn points must be above the floor with clearance for the player collider.

---

## 3. Required Manager GameObjects

Each manager is a separate GameObject. Wire all references via inspector.

### 3a. GameManager
Holds: `MatchManager`, `RoundManager`, `DraftManager`

**`MatchManager` configuration:**
- `winsToWinMatch` = 3 (shorter loop for testing; default is 5)
- `roundManager` → RoundManager ref
- `draftManager` → DraftManager ref
- `spawnManager` → PlayerSpawnManager ref
- `multiCamera` → MultiTargetCamera ref
- `announcer` → RoundAnnouncer ref
- `killFeed` → KillFeed ref
- `charSelect` = **null** (skip CharSelect — see §5)
- `monsterSpawnManager` = null
- `chestSpawnManager` = null
- `arenaBuilder` = null
- `levelGenerator` = null

**`RoundManager` configuration:**
- `zoomDelaySeconds` = 20
- `maxRoundSeconds` = 60
- `enableTimeout` = true
- `overtimeSeconds` = 5

**`DraftManager` configuration:**
- `optionsPerPick` = 4
- `generalPoolRatio` = 0.4
- `allCards` → drag all `PowerCardData` assets from `Data/Cards/`

### 3b. PlayerManager
Holds: `PlayerSpawnManager`, `PlayerInputManager` (Unity component)

**`PlayerSpawnManager` configuration:**
- `spawnPoints[0]` → SpawnPoint_P1
- `spawnPoints[1]` → SpawnPoint_P2
- `defaultClassData` → WizardData asset (or any ClassData asset from `Data/Classes/`)
- `matchManager` → MatchManager ref
- `multiTargetCamera` → MultiTargetCamera ref

**`PlayerInputManager` configuration:**
- Player Prefab → Player prefab from `Prefabs/Player/`
- Joining Behavior = Join Players When Button Is Pressed
- Max Players = 2
- Notification Behavior = Send Messages

### 3c. Camera
Holds: `MultiTargetCamera`

- `offset` = (0, 0, -10)
- `smoothTime` = 0.5
- `minZoom` = 8 (orthographic size)
- `maxZoom` = 5 (zoomed-in size at full compression)
- `padding` = 2

### 3d. UI (Canvas, Screen Space — Overlay)
Child GameObjects on the Canvas:

| GameObject | Component | Notes |
|------------|-----------|-------|
| RoundAnnouncer | `RoundAnnouncer` | Center screen text |
| KillFeed | `KillFeed` | Top-right corner |
| Scoreboard | `Scoreboard` | Top-center |
| DraftUI | `DraftUI` | Full-screen panel, hidden during combat |
| CombatHUD | `CombatHUD` | Bottom HUD strip |

Wire each UI component reference in `MatchManager`.

---

## 4. Game Cycle Flow

The scene drives this flow automatically through events — no manual calls after hitting Play:

```
Play pressed
    │
    ▼
MatchManager.Start() → ChangeState(Setup)
    │  (charSelect is null → skip CharSelect)
    ▼
MatchManager.StartMatch() → StartNextRound()
    │
    ▼
[ROUND ACTIVE]
  PlayerInputManager spawns players on button press
  PlayerSpawnManager.OnPlayerJoined() positions + initializes each player
  RoundManager tracks HP / deaths
  Camera zooms in over ~60 seconds
    │
    ▼  (last player standing OR timeout)
RoundManager fires OnRoundEnd(winnerID)
    │
    ▼
[DRAFT]
  Loser(s) see 4 card options via DraftUI
  Winner gains a level (no card pick)
  DraftManager fires OnDraftComplete when all picks done
    │
    ▼
  Check: has any player reached 3 wins?
  YES → MatchEnd state, RoundAnnouncer shows winner
  NO  → StartNextRound() → loop
```

---

## 5. CharSelect Skip

To skip CharSelect for this scene, `MatchManager.charSelect` must be **null**. Add this to `MatchManager.Start()` handling: if `charSelect == null`, call `StartMatch()` directly after `ChangeState(Setup)`.

> **Note:** `MatchManager.cs` already guards `if (charSelect != null)` before calling `BeginSelection`, but `StartMatch()` must still be triggered. The simplest wiring: add a `[SerializeField] private bool skipCharSelect = false;` flag to `MatchManager` and call `StartMatch()` in `Start()` when it's true. Alternatively, call `matchManager.StartMatch()` from a test bootstrap MonoBehaviour on scene load.

---

## 6. Player Prefab Requirements

The Player prefab must have all of the following components (should already exist from `Prefabs/Player/`):

- `PlayerController` (with `MovementData` assigned — use `SharedMovement` asset)
- `PlayerStateMachine`
- `PlayerInputHandler`
- `ClassManager`
- `PlayerIdentity`
- `HealthSystem`
- `ProjectileSpawner`
- `ParrySystem`
- `CardInventory`
- `PhysicsCheck`
- `Rigidbody2D` (Gravity Scale ~3, Collision Detection: Continuous, Constraints: Freeze Z rotation)
- `CapsuleCollider2D`
- `SpriteRenderer`
- `PlayerInput` (Unity component, Actions asset from `Input/`)

---

## 7. Out of Scope for This Scene

These systems should be **null / disabled** in the inspector:

- `ProceduralLevelGenerator` — no arena generation
- `ModularArenaBuilder` — static geometry only
- `MonsterSpawnManager` — no PvE
- `ChestSpawnManager` — no item chests
- `CharacterSelectManager` — skipped (see §5)

---

## 8. Acceptance Criteria

- [ ] Two players spawn inside the box on button press
- [ ] Players can move, jump, wall-jump, wall-slide, and shoot at each other
- [ ] Killing a player ends the round and fires `OnRoundEnd`
- [ ] Draft screen appears after each round; loser picks a card
- [ ] Card is applied and persists into the next round (visible in `CardInventory`)
- [ ] Camera zooms in during a round and resets between rounds
- [ ] Match ends after one player reaches 3 round wins
- [ ] Round announcer text fires at round start, round win, and match win
