# Celeste-Style Player Controller — Requirements Spec

## Goal

Rewrite the player movement system so it feels identical to Celeste (Maddy Thorson / Noel Berry, 2018). Celeste's controller is the gold standard for 2D platformer feel: crisp acceleration, tight air control, satisfying wall mechanics, and an 8-directional dash that is also the core identity of the game.

This spec covers every mechanic gap between the **current system** and Celeste's published behavior, in priority order.

---

## Reference Values

Celeste runs at 60 fps, ~320×180 resolution. Characters are ~8 px wide. Scale factor used throughout this document: **1 Celeste pixel = 0.1 Unity unit** (player ≈ 0.8 u wide, consistent with current prefab).

| Celeste value | px/s or px/s² | Unity units equivalent |
|---|---|---|
| Max run speed | 90 | **9** |
| Ground accel | 1000 | **100** |
| Ground decel / turnaround | 1000 / 1200 | **100 / 120** |
| Air accel | 900 | **90** |
| Air decel | 900 | **90** |
| Gravity (Unity equivalent) | — | **gravityScale ≈ 5** |
| Jump velocity | 285 | **28.5** |
| Fall gravity multiplier | 1.8× jump gravity | **fallGravityMult ≈ 2.0** |
| Max fall speed | 160 | **16** |
| Fast fall speed | 240 | **24** |
| Wall slide max speed | 20 | **2** |
| Wall jump horizontal | 130 | **13** |
| Wall jump vertical | 285 | **28.5** |
| Dash speed | 240 | **24** |
| Dash duration | 0.15 s | **0.15 s** |
| Post-dash retained speed | 140 | **14** |
| Coyote time | 6 frames | **0.1 s** |
| Jump buffer | 4 frames | **0.067 s** |
| Wall jump lockout | 16 frames | **0.267 s** |
| Corner correction | 4 px | **0.4 u** |

> These are starting targets. All values live in `MovementData` (ScriptableObject) and are tuned in the asset — no hardcoded magic numbers.

---

## Priority 1 — Tune Existing Mechanics to Celeste Values

These mechanics already exist. They need data changes in `SharedMovement.asset` and minor code adjustments.

### 1a. Horizontal Movement

**Current issues:**
- `moveSpeed = 10` (target: **9**)
- `acceleration = 100` (correct)
- `deceleration = 80` (target: same as accel, **100**)
- No distinction between decel and turnaround (direction reversal)

**Required change — `MovementData.cs`:**
Add `[Range(5f, 300f)] public float turnAroundAcceleration = 120f;`

**Required change — `PlayerController.MoveHorizontal()`:**
When `targetSpeed` and current velocity are in opposite directions AND `Mathf.Abs(velocity.x) > 1f`, use `turnAroundAcceleration` instead of `deceleration`. This makes direction reversal snappier than stopping.

### 1b. Jump

**Current issues:**
- `jumpForce = 14` gives a low, floaty apex compared to Celeste (target: **~19** with adjusted gravity)
- `jumpCutMultiplier = 0.4` — target is to cut to **50%** of current vy on release (`jumpCutMultiplier = 0.5`)
- Peak hang-time zone too narrow (`peakVelocityThreshold = 2`, target: **3–4**)

**Asset targets for `SharedMovement`:**

| Field | Current | Target |
|---|---|---|
| `moveSpeed` | 10 | 9 |
| `acceleration` | 100 | 100 |
| `deceleration` | 80 | 100 |
| `airAcceleration` | 65 | 90 |
| `airDeceleration` | 50 | 90 |
| `jumpForce` | 14 | 19 |
| `jumpCutMultiplier` | 0.4 | 0.5 |
| `gravityScale` | 3 | 5 |
| `fallGravityMultiplier` | 2.5 | 2.0 |
| `peakGravityMultiplier` | 0.4 | 0.3 |
| `peakVelocityThreshold` | 2 | 3.5 |
| `maxFallSpeed` | 20 | 16 |
| `coyoteTimeDuration` | 0.12 | 0.10 |
| `jumpBufferDuration` | 0.12 | 0.067 |
| `wallJumpLockoutTime` | 0.15 | 0.267 |
| `wallSlideSpeedMin` | 3 | 0.5 |
| `wallSlideSpeedMax` | 15 | 2 |
| `wallJumpForce` | (12, 16) | (13, 19) |
| `fastFallMaxSpeed` | 30 | 24 |

### 1c. Wall Slide — Remove "Hold Toward Wall" Requirement

**Current behavior:** `AirborneState` only transitions to `WallSlidingState` if the player is actively holding the stick toward the wall.

**Celeste behavior:** Wall slide activates whenever you touch a wall while falling (or moving slowly upward), regardless of input direction. Input is only checked to *exit* the wall.

**Required change — `AirborneState.Execute()`:**
Remove `holdingTowardWall` check for the wall slide transition. Enter `WallSlidingState` on any wall contact while `vy <= 2f` and `WallJumpLockoutTimer <= 0`.

**Required change — `WallSlidingState.Execute()`:**
Exit wall slide when player pushes *away* from the wall (input toward open air), not when they stop holding toward it.

```
// Celeste wall-detach: exit if pushing AWAY from the wall
bool pushingAwayFromWall = (ctx.Physics.WallDirection == 1 && inputX < -0.1f)
                        || (ctx.Physics.WallDirection == -1 && inputX > 0.1f);
```

---

## Priority 2 — Dash (New Feature — Highest Priority Gap)

Dash is Celeste's defining mechanic. The current system has a "dash burst" (speed spike at movement start) which is completely different. The dash burst should remain as a tunable subtlety; this is a full additional mechanic.

### 2a. Behavior

- **8-directional:** Dash velocity vector is derived from the directional input at the moment of press. If no input → dash in the last faced direction (horizontal only).
- **Diagonal dashes** use normalized direction (equal X and Y component), same speed as cardinal.
- **One dash charge in the air.** Refills on touching the ground or a dash crystal (out of scope for now — refill on ground only).
- **Duration:** 0.15 s of full dash speed, then decelerate to retained speed.
- **Post-dash retained speed:** After the dash ends, the player retains `dashRetainSpeed` (14 u/s) in the dash direction. This is then subject to normal air deceleration.
- **Jump cancels dash:** Pressing jump during a dash immediately applies jump velocity (vertical) and carries dash horizontal momentum → creates the "super/hyper dash" feel naturally.
- **Invincibility frames:** Player is invincible during the first 0.05 s of a dash (frames 1–3 at 60fps). Requires calling `HealthSystem.GrantInvincibility(0.05f)`.
- **Freeze frame:** 1 frame (0.016 s) of `Time.timeScale = 0` at dash start for impact feel (optional but strongly recommended — makes dash feel punchy).
- **Ground dash:** Works the same as air dash. Does NOT consume the air dash charge. Refills on ground.

### 2b. New State — `DashState.cs`

```
Enter:
  - Record dash direction from input (normalized) or last faced dir
  - Set velocity = dashDirection * dashSpeed
  - Zero gravity (gravityScale = 0) for dash duration
  - Grant invincibility for dashInvincibilityDuration
  - If this was an air dash, set dashesRemaining = 0
  - If jump buffer active, immediately cancel to jump (see §2c)
  - Optional: Time.timeScale freeze for 1 frame

Execute (each Update):
  - dashTimer += Time.deltaTime
  - If jumpPressed → dash-jump (see §2c)
  - If dashTimer >= dashDuration:
      - Restore gravity
      - Retain horizontal velocity (min of current and dashRetainSpeed in X)
      - Vertical: set to 0 if dashing downward into ground, else carry Y component at dashRetainSpeed
      - Transition to GroundedState (if grounded) or AirborneState

Exit:
  - Restore gravity
  - Ensure invincibility timer is not extended
```

### 2c. Dash-Jump (Super / Hyper Dash)

Pressing jump during a dash cancels the dash and applies jump velocity on top of the dash's horizontal component. This is the most powerful tech in Celeste and must work:

```
On jumpPressed during DashState:
  - Retain current horizontal velocity (up to dashRetainSpeed)
  - Apply jump force vertically (replaces Y velocity, does not add)
  - Consume dash jump
  - Transition to AirborneState
  - dashesRemaining stays 0 (dash was used)
```

### 2d. Dash Refill

- On entering `GroundedState`: `dashesRemaining = maxDashes` (1 by default, power cards can increase).
- An `OnDashRefill` event should fire for visual feedback (dust puff, sprite flash).

### 2e. New Fields in `MovementData.cs`

```csharp
[Header("Dash")]
[Tooltip("Number of dashes available in the air. Cards can increase this.")]
[Range(1, 3)] public int maxAirDashes = 1;

[Tooltip("Full dash speed in units/s")]
[Range(10f, 40f)] public float dashSpeed = 24f;

[Tooltip("How long the dash travels at full speed")]
[Range(0.05f, 0.3f)] public float dashDuration = 0.15f;

[Tooltip("Speed retained after dash ends (horizontal component)")]
[Range(5f, 20f)] public float dashRetainSpeed = 14f;

[Tooltip("Seconds of invincibility at dash start")]
[Range(0f, 0.2f)] public float dashInvincibilityDuration = 0.05f;

[Tooltip("Seconds before player can dash again (0 = refill-only cooldown)")]
[Range(0f, 0.5f)] public float dashCooldown = 0f;
```

### 2f. New Input in `IInputProvider.cs`

Add `bool DashPressed { get; }` and `bool DashHeld { get; }` alongside `void ConsumeDash()`. Wire in `PlayerInputHandler` to the dash action in the Input Actions asset.

### 2g. Dash Transitions

`AirborneState.Execute()` and `GroundedState.Execute()` check `ctx.Input.DashPressed && ctx.DashesRemaining > 0` and transition to `DashState`.

Add `public int DashesRemaining { get; set; }` to `PlayerStateMachine`.

---

## Priority 3 — Corner Correction

Celeste silently nudges the player horizontally (up to 4 px = 0.4 u) to pass through tight gaps. Without this, players clip the edges of platforms and feel "sticky."

### Behavior

When the player attempts to jump and the apex would clip a corner, the engine runs a secondary cast offset by up to `cornerCorrectionDistance` left and right. If one of those passes, the player is shifted to that position and the jump proceeds.

### Implementation — `PlayerController.cs`

New method: `public bool TryCornerCorrect(float direction, float distance)`

```csharp
// Called in GroundedState just before ApplyJumpForce
// Also called in AirborneState when rising and IsTouchingWall is briefly true
public bool TryCornerCorrect(float castDirection, float castDistance)
{
    // Cast a box/capsule upward from current position offset ±castDirection*castDistance
    // If the shifted cast hits nothing, teleport player by that offset
    // Returns true if correction was applied
}
```

Corner correction also applies **horizontally** when running into walls at head height — shift the player down slightly to slide under overhangs.

### New field in `MovementData.cs`

```csharp
[Header("Corner Correction")]
[Tooltip("Max horizontal nudge on jump to slide through tight gaps (Celeste uses 4px)")]
[Range(0f, 1f)] public float cornerCorrectionDistance = 0.4f;

[Tooltip("Max vertical nudge when walking into an overhang")]
[Range(0f, 1f)] public float edgeSlipDistance = 0.2f;
```

---

## Priority 4 — Wall Climb Stamina

Celeste limits how long players can cling to walls, creating urgency in climbing puzzles. In Spells this also prevents wall camping.

### Behavior

- `wallStamina` starts at `maxWallStamina` (110 units, depletes toward 0).
- **Holding still on wall:** costs `wallGrabStaminaDrain` per second (~45/s).
- **Climbing up:** costs `wallClimbStaminaDrain` per second (~110/s); max climb speed upward = 4 u/s.
- **Wall jumping:** costs flat `wallJumpStaminaCost` (~27.5 units).
- **At 0 stamina:** player cannot climb up. Wall slide continues but grip is lost faster (slide speed increases toward `wallSlideSpeedMax`).
- **Refill:** full refill on any ground contact or dash.

### Implementation

Add to `PlayerStateMachine`:
```csharp
public float WallStamina { get; set; }
```

Add to `WallSlidingState.FixedExecute()`:
- If `inputY > 0.1f` (holding up): apply upward velocity capped at `wallClimbSpeed`, drain `wallClimbStaminaDrain * dt`
- If holding still: drain `wallGrabStaminaDrain * dt`
- If stamina = 0: force slide speed to accelerate toward `wallSlideSpeedMax` immediately

Add to `GroundedState.Enter()` and `DashState.Enter()`:
```csharp
ctx.WallStamina = ctx.Controller.Data.maxWallStamina;
```

### New fields in `MovementData.cs`

```csharp
[Header("Wall Stamina")]
[Range(10f, 200f)] public float maxWallStamina = 110f;
[Tooltip("Stamina drain per second while stationary on wall")]
[Range(0f, 100f)] public float wallGrabStaminaDrain = 45f;
[Tooltip("Stamina drain per second while climbing up")]
[Range(0f, 200f)] public float wallClimbStaminaDrain = 110f;
[Tooltip("Flat stamina cost per wall jump")]
[Range(0f, 50f)] public float wallJumpStaminaCost = 27.5f;
[Tooltip("Max upward climb speed while holding up on wall")]
[Range(1f, 8f)] public float wallClimbSpeed = 4f;
```

---

## Priority 5 — Wavedash (Emergent from Dash + Ground)

A true Celeste wavedash emerges naturally from the dash + ground systems:

1. Player air-dashes diagonally down-forward (or straight down)
2. Player immediately lands and ground is detected
3. Because the dash was angled downward, horizontal speed is retained on landing
4. Jump buffer preserves the landing into an immediate jump with horizontal momentum

This **requires no additional code** if:
- `DashState` retains the horizontal component of `dashRetainSpeed` on ground contact
- `GroundedState.Enter()` checks jump buffer and transitions to `AirborneState` (already implemented)

The one tweak: when entering `GroundedState` from `DashState`, do NOT reset `currentMaxSpeed` to `moveSpeed` — preserve the dash horizontal momentum until normal deceleration bleeds it off.

---

## Priority 6 — Freeze Frame at Dash Start (Optional / Polish)

A 1-frame time scale pause at dash start gives the dash a punchy, satisfying feel.

```csharp
// In DashState.Enter():
StartCoroutine(FreezeFrame(1));

private IEnumerator FreezeFrame(int frames)
{
    Time.timeScale = 0f;
    for (int i = 0; i < frames; i++)
        yield return new WaitForEndOfFrame();
    Time.timeScale = 1f;
}
```

> Note: This is a MonoBehaviour coroutine, so `DashState` must invoke it through `PlayerStateMachine` (which IS a MonoBehaviour). Add `public void StartFreezeFrame(int frames)` to `PlayerStateMachine`.

---

## What Does NOT Change

These systems are deliberately kept from the current design despite differences from Celeste:

| Current feature | Reason to keep |
|---|---|
| Wave-land (convert horizontal landing momentum into a slide) | Unique to Spells; plays well with card system (e.g. Haste + wave-land combo) |
| Dash burst (speed spike when starting movement) | Subtle; complements dash without conflicting |
| Ground slide (crouch while running) | Good feel; Celeste doesn't have it but it adds depth |
| Slope physics | Already implemented and correct |
| 3-zone gravity (peak/rising/falling) | Already matches Celeste's approach |
| Spider Shoes surface traversal | Card-system mechanic; out of Celeste scope |

---

## New Files Required

| File | Purpose |
|---|---|
| `Scripts/Player/States/DashState.cs` | New state for the 8-directional dash |

## Modified Files

| File | Change |
|---|---|
| `MovementData.cs` | Add dash, corner correction, wall stamina fields |
| `PlayerController.cs` | Add `TryCornerCorrect()`, `ApplyDash()`, `turnAroundAcceleration` to `MoveHorizontal()` |
| `PlayerStateMachine.cs` | Add `DashesRemaining`, `WallStamina`, `StartFreezeFrame()` |
| `IInputProvider.cs` | Add `DashPressed`, `DashHeld`, `ConsumeDash()` |
| `PlayerInputHandler.cs` | Wire dash action from Input Actions asset |
| `TestInputProvider.cs` | Stub `DashPressed = false` |
| `AirborneState.cs` | Remove hold-toward-wall requirement; add dash transition |
| `WallSlidingState.cs` | Fix detach condition; add climb up; add stamina drain |
| `GroundedState.cs` | Add dash transition; add `turnAroundAcceleration` pass-through; add corner correction call |
| `SharedMovement.asset` | Retune all values per the table in §1b |

---

## Acceptance Criteria

- [ ] Jump arc matches Celeste: high apex, snappy descent, satisfying hang at peak
- [ ] Releasing jump early noticeably reduces jump height (variable height control)
- [ ] Direction reversal on ground is snappier than stopping from full speed
- [ ] Air control is tight but committed — mid-air direction changes take ~0.1 s
- [ ] Wall slide activates on contact with any wall while falling — no "hold toward wall" required
- [ ] Wall jump launches player away from wall; horizontal input toward wall is locked for ~0.27 s
- [ ] Dash fires in the 8-direction matching input, travels 0.15 s at full speed, then decelerates
- [ ] Only 1 dash available in the air; refills on ground contact
- [ ] Pressing jump during dash cancels dash and preserves horizontal momentum
- [ ] Corner correction silently nudges player through gaps ≤ 0.4 u wide
- [ ] Wall stamina depletes; at 0, player cannot climb up but can still slide
- [ ] Wavedash works emergently: diagonal-down dash into ground + jump = horizontal speed burst
- [ ] All movement values live in `SharedMovement.asset`; no magic numbers in code
