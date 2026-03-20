# CLAUDE.md — Spells Project

## Project Overview

**Spells** is a 4-player party arena brawler built in Unity (C#), inspired by *Rounds*. Players pick spellcaster classes and fight on 2D planes in 3D fantasy environments. After each round, losers draft power cards (each with positive AND negative effects). First to 5 round wins takes the match.

The full design vision lives in [GDD.md](GDD.md).

---

## Repository Layout

```
Spells/                         # Repo root
├── GDD.md                      # Game Design Document
├── CLAUDE.md                   # This file
├── README.md                   # Build & setup instructions
├── Specs/                      # Feature requirements specs
│   ├── BoxArenaScene.md        # Box arena scene spec
│   ├── CelesteMovement.md      # Celeste-style movement spec
│   ├── CoyoteTimeShortHop.md   # Coyote time + short hop spec
│   └── ManagerRemoval.md       # Removal of Core manager layer spec
└── Spells/                     # Unity project root
    └── Assets/
        ├── Scenes/
        │   ├── SampleScene.unity
        │   └── CombatTestArena.unity   # Active development scene
        ├── Settings/
        └── _Project/           # All game content lives here
            ├── Art/Sprites/
            ├── Data/           # ScriptableObject assets
            │   ├── Biomes/     # 5 biome theme assets
            │   ├── Classes/    # 8 ClassData assets
            │   ├── Combat/     # Per-class CombatData assets
            │   ├── Movement/   # SharedMovement asset
            │   └── Settings/   # DefaultGameSettings
            ├── Input/
            │   └── PlayerInputActions.inputactions
            ├── Resources/
            │   └── Cards/      # PowerCardData assets (loaded via Resources.LoadAll)
            ├── Prefabs/
            │   ├── Player/     # PlayerCharacter.prefab
            │   ├── Projectiles/# WizardBolt.prefab, WarriorAxe.prefab
            │   └── Environment/
            ├── Scripts/        # C# source
            └── Tests/
                ├── EditMode/
                └── PlayMode/
```

---

## Scripts Folder Structure

```
Scripts/
├── Core/           # Scene builders and arena tools
├── Player/         # Controller, state machine, class system
│   └── States/     # One file per movement state
├── Combat/         # Health, parry, projectiles, behaviors, status effects
│   ├── Behaviors/  # Projectile behavior components
│   └── Abilities/  # Class ability components
├── Cards/          # Power card data, inventory, SpellEffect system
│   └── Effects/    # SpellEffect implementations
├── Data/           # ScriptableObject definitions
├── UI/             # All UI controllers
├── Camera/         # MultiTargetCamera
├── Environment/    # Platforms, hazards, chests, destructibles
├── Items/          # Temporary item behaviors and registry
├── Utilities/      # PhysicsCheck, CombatAnalytics
├── Editor/         # Custom Unity editor tools
└── Tests/          # Edit mode + play mode test files
```

---

## Core Systems

### Scene Builders — `Scripts/Core/`

The match loop lives **inline** inside the scene builder — there are no external manager objects.

| File | Role |
|------|------|
| `BoxArenaBuilder.cs` | Self-contained 2-player match runner. Builds geometry and kill zones at runtime, spawns players via `PlayerInput.Instantiate`, owns the full round/match/draft loop inline |
| `TestArenaBuilder.cs` | Movement-test scene — no match logic. Handles `OnPlayerJoined` inline via SendMessages |
| `ProceduralLevelGenerator.cs` | 6-phase compositional pipeline for arena generation |
| `ModularArenaBuilder.cs` | Builds arenas from `ArenaLayoutData` using prefab pieces |
| `LevelValidator.cs` | Reachability validation for generated layouts |
| `CombatAnalytics.cs` | Event-driven analytics utility |

### Player — `Scripts/Player/`

| File | Role |
|------|------|
| `PlayerController.cs` | Physics-based movement (move, jump, slide, wall-jump, wave-land, dash, corner correction). `FacingDirection`, `ApplyDash()`, `EndDash()`, `TryCornerCorrect()` |
| `PlayerStateMachine.cs` | Orchestrates states: Grounded / Airborne / WallSlide / Hitstun / SurfaceTraversal / Dash. Owns `DashesRemaining`, `WallStamina`, `LastWallDirection`, `StartFreezeFrame()` |
| `IInputProvider.cs` | Interface — `MoveInput`, `JumpPressed`, `JumpHeld`, `DashPressed`, `ShootPressed`, `SpecialPressed`, `AimDirection`, `CrouchHeld` and their Consume methods |
| `PlayerInputHandler.cs` | Implements `IInputProvider` via Rewired (`ReInput.players.GetPlayer(id)`) — polls axes and buttons each frame |
| `ClassManager.cs` | Applies `ClassData` to player; initializes combat/movement; applies card modifiers |
| `PlayerIdentity.cs` | Player ID + team |
| `PlayerDeathHandler.cs` | Death events, feedback, `ResetForRound()` |
| `PlayerVisualFeedback.cs` | Visual effects on hit, death, etc. |
| `TemporaryItemInventory.cs` | Manages runtime temporary items |

Player states (`States/`): `GroundedState`, `AirborneState`, `WallSlidingState`, `HitstunState`, `SurfaceTraversalState`, `DashState`.

> **Note:** The wall-slide state class is `WallSlidingState`. The property on `PlayerStateMachine` is `WallSlideState` (typed `WallSlidingState`).

### Combat — `Scripts/Combat/`

| File | Role |
|------|------|
| `HealthSystem.cs` | HP, damage, iframes, knockback, heal, revive |
| `ParrySystem.cs` | 6-8 frame timing window; reflects projectile; whiff recovery |
| `ProjectileSpawner.cs` | Fires projectiles on Shoot input (F key / buttonEast). Direction = right stick or MoveInput (WASD). `CanHitOwner = true` — friendly fire enabled. Spawn point cleared past player's BoxCollider2D extents |
| `Projectile.cs` | Base projectile: movement, lifetime, bouncing, self-damage. SerializeField overrides: `lifetimeMultiplier` (default 5×), `bulletGravity` (default 0.15), `bulletBounces` (default 3) |
| `ProjectileSpawner.cs` | Fires projectiles; ammo system (Warrior axes); cooldown. `IsChargingShot` flag blocks normal fire. `ConsumeAmmo(n)` / `StartRefillIfEmpty()` API for SpellEffects that handle firing themselves |
| `ProjectileTrail.cs` | Visual trail on projectiles |
| `ClassAbility.cs` | Per-class special ability base. Activates on `SpecialPressed` input (Q / North face button). Cooldown system with green ready-flash (0.1s). Subclasses override `Activate()` and optionally `Tick()` |
| `CombatEventRouter.cs` | Routes combat events between systems |
| `SpawnProtection.cs` | Iframes on round start |
| `MonsterEntity.cs` | PvE enemy |
| `SpiritEntity.cs` | Spirit summon (Shaman/SpiritBond) |
| `TotemEntity.cs` | Totem summon (Shaman/AncestralTotem) |
| `PotionZone.cs` / `PotionZoneSpawner.cs` | Alchemist potion area |
| `HexMarkStatus.cs` / `PoisonStatus.cs` | Persistent status effects |

**Projectile behaviors** (`Combat/Behaviors/`): `HomingBehavior`, `RicochetBehavior`, `ExplosiveBehavior`, `SplitBehavior`, `MagneticReturnBehavior`, `AmbushProjectileBehavior`, `LuckyBounceBehavior`, `DarkTetherBehavior`

**Modifier system**: `ProjectileModifier`, `ProjectileModifierSystem`

### Power Card System — `Scripts/Cards/`

| File | Role |
|------|------|
| `PowerCardData.cs` | Card definition: positive/negative effects, tier, stacking rules |
| `CardInventory.cs` | Tracks held cards; handles stacking and modifier application |
| `StatModifier.cs` | Numeric modifier applied to `CombatData`/`MovementData` |
| `SpellEffect.cs` | Abstract base for special card behaviors (OnApply, OnRemove, OnRoundStart, OnRoundEnd) |
| `SpellEffectRegistry.cs` | Instantiates and manages `SpellEffect` lifecycle |

**Implemented SpellEffects** (`Cards/Effects/`):
`AmbushEffect`, `AncestralTotemEffect`, `BerserkerEffect`, `BloodPactEffect`, `BuckshotEffect`, `BulletStormEffect`, `ChainLightningEffect`, `ChargeShotEffect`, `DarkTetherEffect`, `DeathBlossomEffect`, `ExplosiveRoundsEffect`, `ExtendedClipEffect`, `FragmentationEffect`, `FullAutoEffect`, `GlassCannonEffect`, `HairTriggerEffect`, `HeavyThrowEffect`, `HexMarkEffect`, `JackpotEffect`, `LichFormEffect`, `LuckyBounceEffect`, `MagneticReturnEffect`, `NukeEffect`, `OverdriveEffect`, `QuickDrawEffect`, `RicochetHellEffect`, `RicochetRoundsEffect`, `SecondWindEffect`, `SeekingRoundsEffect`, `SmokeBombEffect`, `SoulSiphonEffect`, `SpiritBondEffect`, `StickyBrewEffect`, `TwinBarrelEffect`, `VampiricEffect`, `VenomDartEffect`, `VolatileMixEffect`

### Data ScriptableObjects — `Scripts/Data/`

| Class | Purpose |
|-------|---------|
| `ClassData` | Class definition: CombatData ref, projectile prefab, `abilityClassName` (string resolved at runtime), card pool tags, color/icon |
| `CombatData` | HP, projectile speed/damage/lifetime/radius/gravity/bounces, knockback, parry window, iframes |
| `MovementData` | Speed, acceleration, turnaround accel, jump, gravity, wall slide/climb/stamina, wave-land, dash, corner correction, fast fall parameters |
| `BiomeData` | Biome structure rules: bounds, platform count/height, walls, hazards, visuals |
| `ArenaLayoutData` | Output of procedural generation; list of placed arena pieces |
| `ItemData` | Temporary item definition |
| `MonsterData` | Enemy configuration |
| `GameSettings` | Global settings |
| `AudioEvent` | Sound event definition |

### Camera — `Scripts/Camera/`

`MultiTargetCamera` — smooth orthographic camera that tracks all registered targets. Projectile tracking is enabled by default (`trackProjectiles = true`): live `Projectile` instances are found each `LateUpdate` via `FindObjectsByType` and folded into the bounds. `SetZoomProgress(0–1)` drives round-end compression.

---

## Architecture Patterns

**Data-Driven Design** — All combat/movement tuning is in ScriptableObject assets. Adding a new class = create data assets, no code changes.

**Composition over Inheritance** — Players are a flat composition of MonoBehaviours.

**Inline Match Loop** — `BoxArenaBuilder` owns the full round/match/draft flow with no external manager dependencies. All state is local fields.

**State Machine** — `PlayerStateMachine` manages movement states with pre-allocated plain C# state objects.

**Event-Driven** — UnityEvents decouple systems: `OnDeath`, `OnCardAdded`, `OnParrySuccess`, etc.

**Modifier System** — `StatModifier` applies numeric deltas to `CombatData`/`MovementData`. Card stacking multiplies modifiers.

**SpellEffect Registry** — Cards with special behaviors instantiate a `SpellEffect` subclass at runtime. Hook pattern (OnApply/OnRemove/OnRoundStart/OnRoundEnd) enables stateful effects without touching core systems.

**Input Abstraction** — `IInputProvider` decouples movement from input. `PlayerInputHandler` is the real implementation; `TestInputProvider` is the play-mode test mock. Adding a new input requires changes to all three.

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
| Jester | Chaos/bounce; card pool: LuckyBounce, Jackpot, GlassCannon, Shuffle |

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
- **PowerCardData assets** live in `_Project/Resources/Cards/` so they can be loaded at runtime via `Resources.LoadAll<PowerCardData>("Cards")`.
- Prefabs live in `_Project/Prefabs/` organized by type.
- New SpellEffects: extend `SpellEffect`, register in `SpellEffectRegistry`, create a matching `PowerCardData` asset in `Resources/Cards/`. Also add a runtime fallback in `BoxArenaBuilder.EnsureRuntimeCards()`.
- New classes: create `ClassData` + `CombatData` assets — no code changes needed unless adding a unique class ability.
- All stat tuning (damage, speed, HP, parry window) is done in ScriptableObject assets, not in code.
- Feature requirement specs live in `Specs/` at the repo root.
- **SpellEffect caching caveat**: `SpellEffect.Initialize()` caches `Spawner`, `Class`, `Identity` etc. via `GetComponent` at card-pick time. If these refs are null (e.g., component not yet initialized), the subclass should lazily re-fetch via `GetComponent` in `Update()`.

---

## Celeste Movement System

Implemented via `Specs/CelesteMovement.md`. Key mechanics:

| Mechanic | Implementation |
|----------|---------------|
| **8-dir dash** | `DashState` — snaps input to 8 directions, zeroes gravity, holds velocity for `dashDuration`. 1 air charge; refills on landing or wall-jump |
| **Freeze frame** | `PlayerStateMachine.StartFreezeFrame(duration)` — pauses Update/FixedUpdate for hitstop at dash start |
| **Dash-jump / wavedash** | Jump during `DashState` → `EndDash(preserveHorizontal:true)` + jump force. Emergent wavedash: diagonal-down dash → immediate jump |
| **Short hop / variable height** | `AirborneState` checks `!jumpCut && !JumpHeld && vy > 0` each frame → `CutJumpVelocity()` multiplies vy by `jumpCutMultiplier` (default 0.4). `jumpCut` flag prevents double-cut. Jump action uses `Press(behavior=2)` (PressAndRelease) so `JumpHeld` correctly goes false on release |
| **Fast fall** | Holding down while `vy <= 0` → `MoveTowards` toward `-fastFallMaxSpeed` at rate `fastFallGravityMultiplier × gravityScale × 3` each FixedUpdate |
| **Turnaround acceleration** | `MoveHorizontal()` detects input opposite to velocity, substitutes `turnAroundAcceleration` |
| **Dash startup burst** | `UpdateDashBurst()` — instant velocity kick from standstill; `currentMaxSpeed` overshoots then decays via `dashDecayRate` |
| **Corner correction** | `TryCornerCorrect()` — called on every jump; nudges player past ceiling corners |
| **Wall climb stamina** | `WallStamina` — drains while sliding, 2× climbing up, refills on ground |
| **Wall grab (no hold)** | Any wall contact (not holding away, stamina > 0, not rising fast) triggers `WallSlidingState` |
| **Wall-jump refills dash** | `WallSlidingState` resets `DashesRemaining` on wall-jump |
| **Wall coyote time** | `WallSlidingState` sets `CoyoteTimer` + `LastWallDirection` on detach. `AirborneState` checks `CoyoteTimer > 0 && LastWallDirection != 0` before ground coyote → `ApplyWallJump`, refills dash, sets lockout. `LastWallDirection` cleared on landing |

> **Required Unity setup:** Input is handled via **Rewired** (not Unity Input System). Action names must match those in the Rewired Input Manager asset: `"Move Horizontal"`, `"Move Vertical"`, `"Jump"`, `"Dash"`, `"Shoot"`, `"Parry"`, `"Special"`, `"Aim Horizontal"`, `"Aim Vertical"`. `PlayerInputHandler` polls `GetButton`/`GetButtonDown`/`GetAxis` each frame.

---

## Runtime Builder Pattern — `BoxArenaBuilder`

Builds the entire 2-player match scene at runtime — no scene hierarchy setup needed.

**Arena geometry** is created in `BuildBoxArena()`:
- Floor (`Ground` layer), left/right walls (`Wall` layer, zero friction), ceiling
- Four **kill zone** trigger strips outside the arena (`killZonePadding`, default 6 units). Anything entering a kill zone: players take 9999 damage (instant kill), projectiles are destroyed

**Card pool auto-population**: `AutoPopulateCardPools()` runs in `Start()`. Loads all `PowerCardData` from `Resources/Cards/` via `Resources.LoadAll`. `EnsureRuntimeCards()` creates any missing card definitions (e.g. Charge Shot) at runtime so no editor batch step is required. If inspector card arrays are empty, both players get the full pool.

**Debug Card Shop**: Press **Start/Select** on gamepad or **G** on keyboard to open a card grid overlay. Navigate with **stick/d-pad/WASD**, confirm with **Jump/A/Enter**, switch target player with **bumpers/Tab**, close with **Start/G/Escape**. Grants the selected card to the chosen player immediately.

**Player spawning** uses Rewired for input. Players are instantiated with `Rewired.ReInput.players.GetPlayer(id)`:
- Rewired action names: `"Move Horizontal"`, `"Move Vertical"`, `"Jump"`, `"Dash"`, `"Shoot"`, `"Parry"`, `"Special"`, `"Aim Horizontal"`, `"Aim Vertical"`
- P1 (Rewired player 0), P2 (Rewired player 1) — mapped in Rewired Input Manager asset
- Gamepad: left stick move, South jump, East shoot, South dash, North special

**Round loop**: `StartRound()` → subscribes `HealthSystem.OnDeath` → `OnPlayerDied()` → `EndRoundAfterDelay()` → `EndRound()` → `AutoPickCardThenNextRound(loserIndex)` coroutine → `StartRound()`.

**Runtime UI created in `Start()`:**
- `PlayerHUDOverlay` — health bars and ammo display per player
- `RoundTimerUI` — 60-second countdown, starts/stops with each round

**Key rules:**
- `PlayerDeathHandler.ResetForRound()` must be called on dead players before repositioning each round
- `SpellEffectRegistry` is created as a standalone GameObject in `Start()` before any card is added
- `InstantKillTrigger` (inner class) handles both player kills and projectile destruction in `OnTriggerEnter2D`
- `ClassManager.Initialize()` resolves `abilityClassName` via assembly scan and adds the ability component at runtime

---

## Projectile System

Projectiles are configured by `CombatData` at spawn time, with **prefab-level SerializeField overrides** on `Projectile.cs`:

| SerializeField | Default | Effect |
|----------------|---------|--------|
| `lifetimeMultiplier` | `5` | Multiplies CombatData lifetime |
| `bulletGravity` | `0.15` | Rigidbody gravity scale — slow arc |
| `bulletBounces` | `3` | Bounce count before destroying |

Spawn position is computed using `col.bounds` (world-space AABB — correct when player is flipped) projected onto the aim direction, plus `projectileRadius + 0.1f` clearance. `CanHitOwner = true` by default — friendly fire and self-damage are enabled.

Bounce normals use `Physics2D.Distance` for accurate reflection off floors, walls, and ceilings.

`Projectile.DisableBouncing()` — public method to zero out bounce state after `Initialize()`, overriding prefab defaults. Used by abilities that spawn projectiles without bouncing.

---

## Class Ability System

Class abilities are activated via the **Special** input (Q key / North face button on gamepad). The system is data-driven: `ClassData.abilityClassName` stores a string (e.g. `"WizardFireball"`), and `ClassManager.Initialize()` resolves the type at runtime via assembly scan and calls `AddComponent`.

**Base class:** `ClassAbility` (abstract MonoBehaviour)
- `Update()` checks `SpecialPressed` → `TryActivate()` → `Activate()` (abstract)
- Cooldown system with `CooldownRemaining`, `IsReady`, `CooldownProgress`
- Green flash on player sprite when cooldown finishes (0.1s, configurable)
- `Tick()` virtual — called every frame while `IsActive` (for channeled/sustained abilities)
- `ResetCooldown()` called by `ClassManager.ResetForRound()`

**Implemented abilities** (`Combat/Abilities/`):

| Ability | Class | Description |
|---------|-------|-------------|
| `WizardFireball` (Arcane Shield) | Wizard | 2s invincibility shield. Shield GO is on `Wall` layer — projectiles bounce off it like walls. Non-trigger `CircleCollider2D` blocks players physically and knocks them back on cast. Blocks shooting while active. 8s cooldown |
| `TeleportAbility` | Wizard (alt) | Short-range blink in aim direction. Raycast-limited, brief iframes. 4s cooldown |
| `ShieldBashAbility` | Warrior | Close-range stun |

**Adding a new ability:**
1. Create a new class extending `ClassAbility` in `Scripts/Combat/Abilities/`
2. Override `Activate()` (and optionally `Tick()`)
3. Set `abilityClassName` on the class's `ClassData` asset (or update `PatchClassAbilities()` in `SetupMVPAssets.cs`)

**Input bindings:**
- P1: `Q` key (`KeyboardWASD` scheme)
- Gamepad: `buttonNorth` (Y / Triangle)
- P2 (`KeyboardArrows`): not yet bound

---

## UI Systems — `Scripts/UI/`

| File | Role |
|------|------|
| `RoundTimerUI.cs` | 60-second countdown timer (configurable `matchDuration`). Displays `SS:mm` in red at top-center via `OnGUI`. `StartTimer()` / `StopTimer()` called by `BoxArenaBuilder` round loop. `TimeRemaining` property for other systems |
| `PlayerHUDOverlay.cs` | Runtime Canvas (`ScreenSpaceOverlay`). Health bars and ammo counters per player. Positioned via `WorldToScreenPoint` in `LateUpdate()` |
| `RoundAnnouncer.cs` | Large centered text for round start/end ("ROUND 1", "FIGHT!", player wins). `OnGUI` with fade-out |
| `Scoreboard.cs` | Round win tracking display |
| `DraftUI.cs` | Post-round card selection panel (TextMeshPro) |
| `CharacterSelectUI.cs` | Character select screen |
| `CombatHUD.cs` | Combat-time health/card display |
| `KillFeed.cs` | Kill notifications |
| `PostMatchSummary.cs` | End-of-match summary |
