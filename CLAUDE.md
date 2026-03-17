# CLAUDE.md — Spells Project

## Project Overview

**Spells** is a 4-player party arena brawler built in Unity (C#), inspired by *Rounds*. Players pick spellcaster classes and fight on 2D planes in 3D fantasy environments. After each round, losers draft power cards (each with positive AND negative effects). First to 5 round wins takes the match.

The full design vision lives in [GDD.md](GDD.md).

---

## Repository Layout

```
Spells/                         # Repo root
├── GDD.md                      # Game Design Document (520 lines)
├── CLAUDE.md                   # This file
├── Specs/                      # Feature requirements specs
│   ├── BoxArenaScene.md        # Box arena scene spec
│   ├── CelesteMovement.md      # Celeste-style movement spec
│   └── ManagerRemoval.md       # Removal of Core manager layer spec
└── Spells/                     # Unity project root
    └── Assets/
        ├── Scenes/
        │   ├── SampleScene.unity
        │   └── CombatTestArena.unity
        ├── Settings/
        └── _Project/           # All game content lives here
            ├── Art/Sprites/
            ├── Data/           # ScriptableObject assets
            │   ├── Biomes/     # 5 biome theme assets
            │   ├── Cards/      # 18+ PowerCardData assets
            │   ├── Classes/    # 8 ClassData assets
            │   ├── Combat/     # Per-class CombatData assets
            │   ├── Movement/   # SharedMovement asset
            │   └── Settings/   # DefaultGameSettings
            ├── Input/
            ├── Prefabs/
            │   ├── Player/
            │   ├── Projectiles/
            │   └── Environment/
            ├── Scripts/        # 120 C# files (~15,600 LOC)
            └── Tests/
                ├── EditMode/
                └── PlayMode/
```

---

## Scripts Folder Structure

```
Scripts/
├── Core/           # Match/round/level management
├── Player/         # Controller, state machine, class system
├── Combat/         # Health, parry, projectiles, behaviors, status effects
├── Cards/          # Power card data, inventory, SpellEffect system
│   └── Effects/    # 18+ individual SpellEffect implementations
├── Data/           # ScriptableObject definitions (ClassData, CombatData, etc.)
├── UI/             # All UI controllers (Draft, HUD, CharSelect, Scoreboard…)
├── Camera/         # MultiTargetCamera
├── Environment/    # Platforms, hazards, chests, destructibles
├── Items/          # Temporary item behaviors and registry
├── Utilities/      # PhysicsCheck, CombatAnalytics
├── Editor/         # Custom Unity editor tools
└── Tests/          # Edit mode + play mode test files
```

---

## Core Systems

### Core Scene Builders — `Scripts/Core/`
| File | Role |
|------|------|
| `BoxArenaBuilder.cs` | Self-contained 2-player match runner — builds geometry, spawns players, owns round/match/draft loop inline. No external managers required |
| `TestArenaBuilder.cs` | Movement-test scene builder — static + procedural arena paths, handles `OnPlayerJoined` inline via SendMessages |
| `ProceduralLevelGenerator.cs` | 6-phase compositional pipeline for arena generation |
| `ModularArenaBuilder.cs` | Builds arenas from `ArenaLayoutData` using prefab pieces |
| `LevelValidator.cs` | Reachability validation for generated layouts |
| `CombatAnalytics.cs` | Event-driven analytics utility |

### Player — `Scripts/Player/`
| File | Role |
|------|------|
| `PlayerController.cs` | Physics-based movement (move, jump, slide, wall-jump, wave-land, dash, corner correction). `FacingDirection`, `ApplyDash()`, `EndDash()`, `TryCornerCorrect()` |
| `PlayerStateMachine.cs` | Orchestrates states: Grounded / Airborne / WallSlide / Hitstun / SurfaceTraversal / **Dash**. Owns `DashesRemaining`, `WallStamina`, `StartFreezeFrame()` |
| `IInputProvider.cs` | Interface — includes `DashPressed`, `DashHeld`, `ConsumeDash()` |
| `PlayerInputHandler.cs` | Implements `IInputProvider` via Unity InputSystem. Handles `OnDash(InputValue)` via Send Messages |
| `ClassManager.cs` | Applies `ClassData` to player; initializes combat/movement; applies card modifiers |
| `PlayerIdentity.cs` | Player ID + team |
| `PlayerDeathHandler.cs` | Death events and feedback |
| `TemporaryItemInventory.cs` | Manages runtime temporary items |

Player states are separate files in `States/`: `GroundedState.cs`, `AirborneState.cs`, `WallSlidingState.cs`, `HitstunState.cs`, `SurfaceTraversalState.cs`, **`DashState.cs`**.

> **Note:** The wall-slide state class is `WallSlidingState` (file: `WallSlidingState.cs`). The property on `PlayerStateMachine` is named `WallSlideState` (typed `WallSlidingState`). Don't confuse the two.

### Combat — `Scripts/Combat/`
| File | Role |
|------|------|
| `HealthSystem.cs` | HP, damage, iframes, knockback, heal, revive |
| `ParrySystem.cs` | 6-8 frame timing window; reflects projectile; whiff recovery |
| `ProjectileSpawner.cs` | Fires projectiles; ammo system (Warrior axes); cooldown |
| `Projectile.cs` | Base projectile: hit detection, reflection |

**Projectile behaviors** (`Combat/Behaviors/`): `HomingBehavior`, `RicochetBehavior`, `ExplosiveBehavior`, `SplitBehavior`, `MagneticReturnBehavior`, `AmbushProjectileBehavior`

**Status effects / modifiers**: `ProjectileModifier`, `ProjectileModifierSystem`, `HexMarkStatus`

**Other**: `ClassAbility`, `SpawnProtection`, `CombatEventRouter`, `PotionZone`, `PotionZoneSpawner`, `MonsterEntity`

### Power Card System — `Scripts/Cards/`
| File | Role |
|------|------|
| `PowerCardData.cs` | Card definition: positive/negative effects, tier, stacking rules |
| `CardInventory.cs` | Tracks held cards; handles stacking and modifier application |
| `StatModifier.cs` | Numeric modifier applied to `CombatData`/`MovementData` |
| `SpellEffect.cs` | Abstract base for special card behaviors (OnApply, OnRemove, OnRoundStart, OnRoundEnd) |
| `SpellEffectRegistry.cs` | Instantiates and manages `SpellEffect` lifecycle |

**Implemented SpellEffects** (`Cards/Effects/`):
`BloodPactEffect`, `LichFormEffect`, `VampiricEffect`, `SmokeBombEffect`, `LuckyBounceEffect`, `BerserkerEffect`, `HeavyThrowEffect`, `MagneticReturnEffect`, `SoulSiphonEffect`, `HexMarkEffect`, `SpiritBondEffect`, `StickyBrewEffect`, `VolatileMixEffect`, `AmbushEffect`, `SecondWindEffect`, `DarkTetherEffect`, `VenomDartEffect`, `AncestralTotemEffect`

### Data ScriptableObjects — `Scripts/Data/`
| Class | Purpose |
|-------|---------|
| `ClassData` | Class definition: CombatData ref, projectile prefab, card pool tags, color/icon |
| `CombatData` | HP, projectile speed/damage, knockback, parry window, iframes |
| `MovementData` | Speed, acceleration, turnaround accel, jump, gravity, wall slide/climb/stamina, wave-land, dash, corner correction parameters |
| `BiomeData` | Biome structure rules: bounds, platform count/height, walls, hazards, visuals |
| `ArenaLayoutData` | Output of procedural generation; list of placed arena pieces |
| `ItemData` | Temporary item definition |
| `MonsterData` | Enemy configuration |
| `GameSettings` | Global settings |
| `AudioEvent` | Sound event definition |

---

## Architecture Patterns

**Data-Driven Design** — All combat/movement tuning values are ScriptableObject assets. Adding a new class means creating data assets, not changing code.

**Composition over Inheritance** — Players are a flat composition of MonoBehaviours (no deep hierarchies). Systems communicate via events and interfaces.

**State Machine** — `PlayerStateMachine` manages movement states.

**Event-Driven** — UnityEvents decouple systems: `OnRoundEnd`, `OnDeath`, `OnCardAdded`, `OnParrySuccess`, etc. Analytics listens to events rather than being called directly.

**Modifier System** — `StatModifier` applies numeric deltas to `CombatData`/`MovementData`. Card stacking multiplies modifiers (e.g., BloodPact ×3 = 3× HP cost).

**SpellEffect Registry** — Cards with special behaviors instantiate a `SpellEffect` subclass at runtime via the registry. The hook pattern (OnApply/OnRemove/OnRoundStart/OnRoundEnd) enables complex stateful effects without touching core systems.

**Input Abstraction** — `IInputProvider` interface decouples movement from input. `PlayerInputHandler` is the real implementation; `TestInputProvider` is the mock used in play-mode tests. Adding a new input (e.g. dash) requires changes to all three: the interface, handler, and test mock.

**Procedural Generation Pipeline** — 6 phases: ground → features → spatial placement → connectivity → perturbation → spawn points. Feature weight tables control biome personality. Reachability is validated against player jump physics constants.

---

## Classes (8)

| Class | Flavor |
|-------|--------|
| Wizard | Arcane bolts; card pool: ArcaneBarrage, SpellShield, ManaOverflow, WildMagic |
| Warrior | Thrown axes (ammo system); card pool: HeavyThrow, MagneticReturn, Berserker, Forge |
| Warlock | High risk/reward; card pool: BloodPact, SoulSiphon, DarkTether, LichForm |
| Shaman | Spirit summons; card pool: SpiritBond, AncestralTotem, PackMentality, Possession |
| Alchemist | Potion zones; card pool: StickyBrew, VolatileMix, TransmuteGround, PhilosophersStone |
| Rogue | Back-attack bonuses; card pool: Ambush, SmokeBomb, FanOfKnives, ShadowStep |
| Witch Doctor | Debuffs/curses; card pool: VenomDart, HexMark, PuppetStrings, VoodooDoll |
| Jester | Chaos/bounce; card pool: LuckyBounce, Copycat, Jackpot, Shuffle |

---

## Biomes (5)

`VolcanicCaldera`, `ForestTemple`, `DesertRuins`, `StormCitadel`, `CrystalCavern`

Each defines structure type (MultiLevel, Islands, Bridge, Vertical, Arena), ground coverage, platform count/height, walls, ramps, hazards, and visual theme colors.

---

## Testing

**Edit Mode** (`Tests/EditMode/`): `AudioEventTests`, `ProjectileModifierTests`, `CombatAnalyticsTests`, `TemporaryItemTests`, `SpellEffectBehaviorTests`

**Play Mode** (`Tests/PlayMode/`): `MovementPhysicsTests`, `WallJumpTests`, `SlopeTests`, `PlayModeTestHelper`, `TestInputProvider`

Play-mode tests use `TestInputProvider` (implements `IInputProvider`) to drive the player without real input.

---

## Key Conventions

- ScriptableObject assets live in `_Project/Data/` mirroring their script folder structure.
- Prefabs live in `_Project/Prefabs/` organized by type (Player, Projectiles, Environment).
- New SpellEffects: extend `SpellEffect`, register in `SpellEffectRegistry`, create a matching `PowerCardData` asset in `Data/Cards/`.
- New classes: create `ClassData` + `CombatData` assets in `Data/Classes/` and `Data/Combat/`; no code changes needed unless adding a unique class ability.
- All stat tuning (damage, speed, HP, parry window) is done in the ScriptableObject assets, not in code.
- Feature requirements specs live in `Specs/` at the repo root.

## Celeste Movement System

Implemented via `Specs/CelesteMovement.md`. Key mechanics:

| Mechanic | Implementation |
|----------|---------------|
| **8-dir dash** | `DashState.cs` — snaps input to 8 directions, zeroes gravity, holds velocity for `dashDuration`. 1 air charge refills on ground or wall-jump |
| **Freeze frame** | `PlayerStateMachine.StartFreezeFrame(duration)` — pauses `Update`/`FixedUpdate` for a short hitstop at dash start |
| **Dash-jump / wavedash** | Jump during `DashState` → `EndDash(preserveHorizontal:true)` + jump force. Emergent wavedash: diagonal-down dash → immediate jump |
| **Turnaround acceleration** | `MoveHorizontal()` detects input opposite to velocity, substitutes `turnAroundAcceleration` for normal accel rate |
| **Corner correction** | `TryCornerCorrect()` in `PlayerController` — called on every jump; nudges player past ceiling corners |
| **Wall climb stamina** | `WallStamina` on `PlayerStateMachine` — drains while sliding, 2× while climbing up, refills on ground |
| **Wall grab (no hold)** | `AirborneState` no longer requires holding toward wall — any wall contact (not holding away, stamina > 0) triggers `WallSlidingState` |
| **Wall-jump refills dash** | `WallSlidingState` resets `DashesRemaining` on wall-jump |

> **Required Unity setup:** Add a **"Dash"** action (Button type) to your Input Action Asset mapped to `Shift` / South gamepad button. `PlayerInputHandler` receives it via `OnDash` Send Messages automatically.

## Runtime Builder Pattern

`BoxArenaBuilder` and `TestArenaBuilder` build their scenes entirely at runtime — no scene hierarchy setup required. There are no external manager dependencies.

### BoxArenaBuilder
Owns the full 2-player match loop inline:
- `Start()` builds geometry, spawns players, creates `SpellEffectRegistry`, then starts the first round.
- **Round loop**: `StartRound()` → subscribes to `HealthSystem.OnDeath` → `OnPlayerDied()` → `EndRound()` → `AutoPickCard()` for losers → `DelayedNextRound()` coroutine → `StartRound()`.
- `AutoPickCard(playerIndex)` draws randomly from the player's assigned `PowerCardData[]` pool and calls `CardInventory.AddCard()`.
- `EndMatch(winnerIndex)` fires when a player reaches `winsToWinMatch` round wins.
- `SpellEffectRegistry` is created as a standalone GameObject in `Start()`; `CardInventory.AddCard()` relies on `SpellEffectRegistry.Instance` being set before any card is picked.
- Between rounds, `PlayerDeathHandler.ResetForRound()` must be called on dead players before repositioning — it re-enables the GameObject, colliders, rigidbody, and input.
- `EnvironmentHazard` damage fields are private `[SerializeField]`; for instant-kill triggers, use `InstantKillTrigger` (defined in `BoxArenaBuilder.cs`) instead.

### TestArenaBuilder
Movement-test scene only — no match logic:
- `SetupPlayerSpawningWithSpawns()` adds `PlayerInputManager` and stores spawn points.
- `OnPlayerJoined(PlayerInput)` is called by `PlayerInputManager` via **SendMessages** (no C# event subscription needed). Handles position, color, `PlayerIdentity`, and camera registration inline.
