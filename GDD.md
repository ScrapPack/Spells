# SPELLS — Game Design Document
**Version:** 0.1 | **Last Updated:** 2026-03-15 | **Status:** Pre-production

---

## 1. Core Concept

**Spells** is a 4-player party arena brawler inspired by *Rounds* (Landfall Games), reimagined as a fantasy spellcaster game. Players choose from 8 caster classes and battle on 2D planes within fully-rendered 3D fantasy environments. After each round, all losing players draft a power card — each with positive AND negative effects — from a class-influenced card pool. The round winner gains a level, unlocking stranger and more transformative options for when they eventually lose. First to 5 round wins takes the match.

**Elevator pitch:** *Rounds, but everyone's a wizard and the guns are spells.*

**Pillars:**
- **Escalating chaos** — every round adds new powers with tradeoffs. By round 8, every player is a Frankenstein of stacked abilities and curses.
- **Party-game accessible, skill-deep** — easy to pick up (aim + shoot + jump), deep to master (parry timing, build strategy, resource management).
- **Stories emerge from systems** — "remember when Jake had triple Blood Pact and died from casting a heal?"
- **The loser catches up** — the power draft ensures nobody falls hopelessly behind. Winning too much can be dangerous.

**Reference games:** Rounds, Towerfall, Duck Game, Bomberman, Slay the Spire (card design philosophy)

---

## 2. Game Loop

### Match Structure
```
CHARACTER SELECT (all 4 players pick class, duplicates allowed)
    │
    ▼
┌─ ROUND START ─────────────────────────────────┐
│  4 players spawn in arena                      │
│  Fight until 1 player remains (last standing)  │
│  Camera zoom closes arena over time            │
│  Interactive environment degrades              │
└────────────────────┬───────────────────────────┘
                     │
                     ▼
┌─ POWER DRAFT ──────────────────────────────────┐
│  Round winner: gains 1 LEVEL (no card)         │
│  3 losers draft in elimination order:          │
│    1st eliminated → picks first (4 options)    │
│    2nd eliminated → picks second (4 options)   │
│    3rd eliminated → picks third (4 options)    │
│  Each player sees 4 cards from their pool      │
│  All cards have positive + negative effects    │
└────────────────────┬───────────────────────────┘
                     │
                     ▼
           First to 5 wins? ──YES──► MATCH END
                     │
                    NO
                     │
                     ▼
              NEXT ROUND (loop)
```

### Round Timer — Camera Zoom
Instead of a hard timer, the camera slowly zooms in over the course of each round, compressing the playable arena. This:
- Forces encounters (no camping or running forever)
- Creates escalating tension as space shrinks
- Shows the 3D environment extending beyond the shrinking play area (visual spectacle)
- Synergizes with environment destruction — by round's end, the arena is smaller, darker, and more hazardous

**Zoom pacing:** Slow for the first 30 seconds, then accelerates. Full compression by ~90 seconds. Exact tuning TBD in playtesting.

### Match Length
- **First to 5 round wins** — approximately 10-15 minutes per match
- Individual rounds: 30-90 seconds (shorter as players accumulate powers)
- Power draft: ~15 seconds per pick

---

## 3. Combat System

### Philosophy
All combat is projectile-based. Even "melee-inspired" classes use thrown weapons (knives, axes). This keeps the Rounds DNA (aim + shoot) while allowing diverse class expression through projectile behavior.

### Universal Mechanics (all classes)

**Movement:**
- Horizontal movement (ground + air)
- Jump
- Wall jump
- Wall slide (slow descent on walls)
- No class-specific movement abilities at base — movement mods come from power cards

**Parry:**
- Universal timing-based mechanic (same for all classes)
- **Window:** 6-8 frames at 60fps (~100-133ms)
- **On success:** Reflects the projectile back toward the attacker
- **On whiff:** Brief recovery frames (vulnerability window) — prevents spam
- **In 4-player context:** Parry is strongest in 1v1 duels but risky in free-for-all (you're focused on one player while two others shoot at your back). This naturally balances the mechanic.
- **Power cards can modify parry** (wider window but weaker reflect, explosive parry, etc.)

**Damage/HP:**
- HP varies by class (2-4 hits — see Class section)
- No healing at base — healing comes from specific power cards
- Damage per hit is standardized at 1 HP (power cards can modify)
- Knockback on hit (strength varies by projectile type)

### No Special Abilities
Classes have NO unique special ability button. All variety beyond the base projectile comes from power cards. This ensures:
- Every ability in the game is subject to the positive/negative card treatment
- No sacred abilities exempt from the chaos system
- Simpler development (no per-class special animations/balance)
- The card draft IS the entire progression system

---

## 4. Classes (8)

All classes share identical movement. Classes are differentiated by **projectile behavior**, **HP**, and **power card pool composition**.

### Wizard
| Stat | Value |
|------|-------|
| HP | 3 |
| Projectile | Rapid arcane bolts — fast fire rate, moderate damage, straight trajectory |
| Pool | Broadest card pool of any class. Access to almost every spell category. High variety, high RNG — less likely to see the same card twice |
| Identity | Jack-of-all-trades. Easy to learn. Unpredictable builds due to pool breadth |

### Warlock
| Stat | Value |
|------|-------|
| HP | 3 |
| Projectile | Slow, heavy dark orbs — low fire rate, high knockback, larger hitbox |
| Pool | Dark pact cards. Many cards trade HP for power. Cards that punish or drain opponents |
| Identity | Risk-reward. Warlock players gamble their own health for devastating effects |

### Alchemist
| Stat | Value |
|------|-------|
| HP | 3 |
| Projectile | Potion lobs — arcing trajectory, leave lingering zones on impact (acid, slime, fire) |
| Pool | Zone control cards. Persistent area effects, terrain modification, trap placement |
| Identity | Area denial. Alchemists control WHERE fights happen. Strong on platforms, weak in open space |

### Shaman
| Stat | Value |
|------|-------|
| HP | 2 |
| Projectile | Standard spirit bolt — moderate speed, moderate damage |
| Pool | Summon/companion cards. Spirit allies, totems, autonomous entities that fight alongside |
| Identity | Action economy. Shaman builds accumulate helpers that create numerical advantage. Low HP forces careful positioning |

### Jester
| Stat | Value |
|------|-------|
| HP | 2 |
| Projectile | Bouncing trick shots — projectiles ricochet off walls and platforms unpredictably |
| Pool | Chaos/random effect cards. Cards with high variance — sometimes amazing, sometimes backfires |
| Identity | The wild card. Jester games are unpredictable. Thrives in enclosed spaces where bounces create chaos |

### Witch Doctor
| Stat | Value |
|------|-------|
| HP | 3 |
| Projectile | Curse darts — moderate speed, apply debuffs on hit instead of (or in addition to) raw damage |
| Pool | Hex/debuff cards. Weaken opponents rather than deal direct damage. Slow, confuse, drain, curse |
| Identity | Attrition. Witch Doctor wears opponents down through stacking debuffs. Wins long fights, vulnerable to burst |

### Rogue
| Stat | Value |
|------|-------|
| HP | 2 |
| Projectile | Throwing knives — fast burst of 2-3 knives in quick succession, short effective range, high DPS up close |
| Pool | Speed/stealth/evasion cards. Smoke, backstab bonuses, movement-while-attacking |
| Identity | Aggression. Rogue wants to be in your face. Glass cannon with burst potential. High skill ceiling |

### Warrior
| Stat | Value |
|------|-------|
| HP | 4 |
| Projectile | Lobbed axes — arcing throw, axes land and stick in surfaces. Must be retrieved (walk over them) to reuse. Starts with 3 axes |
| Pool | Fortify/tank cards. Damage reduction, knockback resistance, ground-pound, intimidation |
| Identity | Resource management. Warrior players juggle limited ammo and high HP. Retrieving axes in combat is the core tension — exposure vs. running out of ammo |

---

## 5. Power Card System

### Design Philosophy
Power cards are the heart of Spells. Every card has **at least one positive and one negative effect**. This prevents power creep, creates interesting decisions, and generates the emergent chaos that defines late-round gameplay.

### Card Anatomy
```
┌─────────────────────────────────┐
│  ☠ BLOOD PACT                   │
│  ─────────────────────────────  │
│  ✦ Spells deal 2x damage       │
│  ✗ Spells cost 1 HP to cast    │
│                                 │
│  [Class: Warlock]  [Tier: 1]    │
└─────────────────────────────────┘
```

- **Name** — thematic, memorable
- **Positive effect(s)** (✦) — the reason you want this card
- **Negative effect(s)** (✗) — the tradeoff
- **Class tag** — which class pool(s) this card belongs to (some are General)
- **Tier** — determines when it appears (Tier 1 = always available, Tier 2 = Level 2+, Tier 3 = Level 3+)

### Card Pools
Each time a loser draws 4 options, the cards are pulled from:
- **General pool** (~40% of draw) — available to all classes
- **Class-specific pool** (~60% of draw) — unique to that class

Pool size varies by class:
- **Wizard:** Largest pool (access to most general + class cards) — high variety, low repeatability
- **Warrior:** Smallest pool — sees the same strong options frequently, builds stack faster

### Stacking
Taking the same card multiple times compounds BOTH the positive and negative:
- **Blood Pact x1:** 2x damage, costs 1 HP per spell
- **Blood Pact x2:** 4x damage, costs 2 HP per spell
- **Blood Pact x3:** 8x damage, costs 3 HP per spell (you die from casting 2 spells on a 3 HP class)

Some cards have stacking caps or transform at higher stacks.

### Winner Levels & Tiers
- **Level 0** (start): Tier 1 cards only — straightforward effects
- **Level 1** (1 win): Tier 1 + rare Tier 2 cards — stranger effects, bigger tradeoffs
- **Level 2** (2 wins): Tier 1 + Tier 2 + rare Tier 3 cards — transformative, game-warping
- **Level 3+** (3+ wins): All tiers available, Tier 3 weighted higher — maximum chaos

Tier 3 cards aren't strictly stronger — they're **stranger**. They transform how you play rather than just boosting numbers.

### Example Cards

#### General Pool (available to all classes)
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Stone Skin** | +1 max HP | -20% movement speed | 1 |
| **Haste** | +30% movement speed | Projectiles deal 0.5x damage | 1 |
| **Vampiric Touch** | Heal 1 HP per kill | Lose 1 HP at round start | 1 |
| **Glass Cannon** | +50% projectile speed | -1 max HP | 1 |
| **Second Wind** | Double jump | Can't wall slide | 2 |
| **Echo Cast** | Spells fire twice (delayed echo) | 2-second cooldown between casts | 2 |
| **Chaos Orb** | Every 5th shot is a random spell from any class | Every 5th shot might target you | 3 |
| **Mirror World** | Parry window is 3x wider | All your projectiles move backwards (you shoot behind you) | 3 |

#### Warlock-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Blood Pact** | Spells deal 2x damage | Casting costs 1 HP | 1 |
| **Soul Siphon** | Gain 1 HP when an opponent is eliminated | Lose 1 HP when you eliminate someone directly | 1 |
| **Dark Tether** | Your orbs home slightly toward nearest opponent | Your orbs also home slightly toward you on return | 2 |
| **Lich Form** | Revive once per round with 1 HP | Permanent -1 max HP for the rest of the match | 3 |

#### Wizard-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Arcane Barrage** | Fire rate doubled | Each bolt deals half damage | 1 |
| **Spell Shield** | Parry also blocks all projectiles in a small AOE | Parry cooldown increased to 3 seconds | 1 |
| **Mana Overflow** | All spells explode on impact (small AOE) | Explosions can damage you | 2 |
| **Wild Magic** | Random spell fires automatically every 2 seconds | You can't control which spell or direction | 3 |

#### Warrior-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Heavy Throw** | Axes deal 2x damage and pierce through targets | Axes travel 50% slower | 1 |
| **Magnetic Return** | Axes automatically return after 3 seconds | Returning axes can hit you | 1 |
| **Berserker** | +1 damage when below half HP | Can't parry when below half HP | 2 |
| **Forge** | Standing still for 2s generates a new axe (no retrieval needed) | Maximum 2 axes (down from 3) | 2 |

#### Jester-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Lucky Bounce** | Bouncing projectiles gain damage with each bounce | First hit (direct, no bounce) deals zero damage | 1 |
| **Copycat** | Your projectile mimics the last spell that hit you | Lose your default bouncing behavior | 2 |
| **Jackpot** | When you get a kill, all opponents also take 1 damage | When you die, all opponents heal 1 HP | 2 |
| **Shuffle** | At round start, randomly swap one power card with each opponent | You might give away your best card | 3 |

#### Rogue-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Ambush** | First hit on an unaware opponent (facing away) deals 2x damage | Non-ambush hits deal 0.75x damage | 1 |
| **Smoke Bomb** | On taking damage, become invisible for 1.5 seconds | While invisible, you can't attack | 1 |
| **Fan of Knives** | Throw 5 knives in a spread instead of 2-3 | Knives deal 0.5x damage each | 2 |
| **Shadow Step** | After parrying, teleport behind the attacker | Miss the parry timing and you teleport INTO the projectile | 3 |

#### Alchemist-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Sticky Brew** | Potion zones last twice as long | Potion zones are half the size | 1 |
| **Volatile Mix** | Potion zones explode when an opponent steps in them | You also trigger your own zones | 1 |
| **Transmute Ground** | Standing on your own zones heals 1 HP over 3s | Opponents standing on your zones gain +25% speed | 2 |
| **Philosopher's Stone** | Your potions create permanent terrain (walls, platforms) | You can't destroy your own terrain | 3 |

#### Shaman-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Spirit Bond** | Summons share your movement (they mirror you) | Summons also share your damage (you take hits when they do) | 1 |
| **Ancestral Totem** | Drop a totem that shoots at nearby enemies | Totem also shoots at you if you're closest | 1 |
| **Pack Mentality** | Each active summon increases your damage by 25% | Each summon death reduces your max HP by 0.5 for the round | 2 |
| **Possession** | Take control of a summon directly (you become invulnerable, summon gets your full moveset) | Your real body stands still and can be killed | 3 |

#### Witch Doctor-Specific
| Card | Positive | Negative | Tier |
|------|----------|----------|------|
| **Venom Dart** | Hits apply poison (1 damage over 5 seconds) | Your own projectiles move 20% slower | 1 |
| **Hex Mark** | Cursed opponents take +1 damage from all sources | You can only curse one opponent at a time | 1 |
| **Puppet Strings** | Cursed opponent's controls reverse for 1.5 seconds on hit | Miss 3 shots in a row and YOUR controls reverse briefly | 2 |
| **Voodoo Doll** | Pick an opponent at round start — 50% of damage they deal is also dealt to them | 50% of damage YOU take is also dealt to your linked ally | 3 |

---

## 6. Arena Design

### Philosophy
Arenas are fully-rendered 3D fantasy environments with 2D gameplay planes. The 3D depth creates visual spectacle and atmosphere while combat stays on the 2D plane. Arenas have interactive elements that degrade through gameplay.

### Interactive Environment Principles
- **Destructible light sources** — shooting a torch removes light AND creates a small fire hazard on the ground
- **Breakable platforms** — some platforms crack after being stood on or hit, eventually falling
- **Destructible cover** — barricades, crates, and walls that break apart over the round
- **Environmental hazards** — lava flows, spike traps, swinging chains that exist independently of player action

By round's end, the arena should look noticeably different: darker (torches shot out), broken (platforms destroyed), more hazardous (fire on the ground), and smaller (camera zoom).

### Arena Concepts (6 for MVP)
1. **Castle Courtyard** — stone platforms, torch-lit walls, chandelier that can be shot down. Classic medieval starting arena.
2. **Dragon's Lair** — cavern with lava below, stalactites that fall when hit, dragon breathing fire periodically in the background (environmental hazard).
3. **Floating Islands** — sky arena with clouds, gaps between islands, wind gusts that affect projectile trajectory. Falling off is instant elimination.
4. **Alchemist's Tower** — vertical arena with rising bubbling liquid below, shelves of potion bottles that shatter into random effects when hit.
5. **Haunted Library** — floating bookshelves as platforms (some are ghosts that disappear), shifting layout, dim lighting.
6. **The Colosseum** — simple symmetrical arena for competitive play. Minimal gimmicks, fair sightlines, tournament-standard.

---

## 7. Art Direction

### Visual Identity
**64x64 pixel art character sprites on fully-rendered 3D environments with real-time lighting.**

This contrast IS the visual identity of Spells. Classic RPG sprites — the kind that evoke Final Fantasy 6, Chrono Trigger, early Fire Emblem — standing in a world with modern 3D rendering, dynamic lighting, particle effects, and depth.

### Character Art
- 64x64 pixel sprites with limited palette per class
- Each class has a distinct silhouette readable at 64x64
- Animation states: idle, run, jump, wall slide, cast (shoot), parry, hit, death
- Power cards add visible modifications (glowing effects, color overlays, summon sprites)
- Pixel art explosions, projectile trails, and impact effects

### Environment Art
- Fully 3D modeled and lit environments
- Fantasy architecture with real-time shadows, volumetric lighting
- Particle effects: torch flames, floating embers, magical dust
- Destructible elements have 3D break animations
- Camera zoom reveals environment detail as it closes in

### Tone
- **Not grimdark, not cutesy** — stylized fantasy with personality
- Characters are small pixel sprites doing absurd things (lobbing axes, hexing each other, summoning spirit wolves) on beautiful stages
- The humor comes from the systems (stacking Blood Pact until you die from casting) not from the art style
- VFX should be satisfying and readable — every spell hit, parry reflect, and power activation should feel punchy

### UI Direction
- Clean, minimal HUD — HP pips, level indicator, equipped powers
- Power draft screen: cards displayed with clear positive/negative, class tag, tier
- Character select: pixel sprites on 3D pedestals with class name and stats
- Scoreboard: round wins per player, current level

---

## 8. Multiplayer

### Modes
- **Local** (couch co-op) — 4 players on one screen. Primary mode.
- **Online** — 4 players matchmade or invite-based. Same gameplay as local.
- **Local + Online hybrid** — 2 local players + 2 online (stretch goal)

### Networking Model
- **Client-server architecture** — one player hosts (or dedicated server for ranked)
- **Rollback netcode** — essential for the parry mechanic's tight timing window. Input delay would make parry unusable online.
- **State sync:** player positions, HP, active power effects, projectile states, environment destruction state

### Match Types
- **Quick Match** — first to 5 with random arena
- **Custom Match** — configurable: rounds to win (3/5/7), arena selection, class restrictions, card bans
- **Practice** — 1v1 against AI or solo exploration of arenas and cards

---

## 9. Progression & Meta (Between Matches)

### Core principle: NO pay-to-win. All gameplay content available from the start.

### Unlockables (cosmetic only)
- **Character skins** — palette swaps and alternate pixel art designs per class
- **Projectile trails** — cosmetic visual effects on spells/projectiles
- **Arena variants** — visual themes (night versions, seasonal, etc.)
- **Card backs** — cosmetic card frame designs for the draft screen
- **Titles/badges** — earned through achievements

### Unlock method
- Match XP based on rounds played (not just wins)
- Achievement-based unlocks for specific feats ("reflect 3 projectiles in one round", "win with 1 HP", "win a match with 4 Blood Pacts")

---

## 10. Sound Design

### Music
- Fantasy orchestral meets chiptune — matching the pixel-on-3D visual identity
- Each arena has a unique theme
- Music intensifies as camera zooms in (layers add, tempo increases)
- Draft screen: calm, contemplative — moment of strategic thinking

### SFX
- Punchy, satisfying hit sounds (screen shake on impact)
- Each class's projectile has a distinct audio signature
- Parry has a high-priority, crisp sound (players need audio confirmation of timing)
- Environment destruction sounds (glass break, wood crack, stone crumble)
- Power card activation jingle (distinct for positive vs. negative effects)

### Voice
- Minimal — character grunts/shouts on hit, cast, parry, death
- Announcer for round start, round end, match point, match win (optional, toggleable)

---

## 11. Technical Spec

### Engine
**TBD — evaluate UE5 vs Godot 4**

| Factor | UE5 | Godot 4 |
|--------|-----|---------|
| 3D environment quality | Excellent (Lumen, Nanite) | Good (not AAA-grade) |
| 2D sprite rendering | Adequate (Paper2D or custom) | Excellent (native 2D) |
| Networking | Mature (replication system) | Improving (ENet, WebRTC) |
| Vibe-coding speed | Slow (C++ compile cycles) | Fast (GDScript, hot reload) |
| Rollback netcode | Manual implementation or plugin | Manual implementation |
| Build size | Large (~1-2 GB minimum) | Small (~50-100 MB) |

**Recommendation:** Godot 4 for prototype/MVP. If visual quality needs to hit AAA for the 3D backgrounds, migrate to UE5 after core gameplay is proven.

### Target Platforms
- **PC** (Steam) — primary
- **Console** (Switch, PlayStation, Xbox) — stretch goal
- **Steam Deck** — should be compatible by default (Linux build)

### Performance Targets
- 60 FPS minimum (parry timing depends on consistent frame rate)
- Support for 4 players with 20+ active projectiles, summons, and environmental effects simultaneously

---

## 12. Scope & MVP

### MVP (Playable Prototype)
- [ ] 2 classes (Wizard + Warrior — most mechanically distinct)
- [ ] 1 arena (Castle Courtyard)
- [ ] Core movement (run, jump, wall jump, wall slide)
- [ ] Basic projectile combat (shoot, hit, damage)
- [ ] Parry mechanic
- [ ] Camera zoom round timer
- [ ] 4-player local multiplayer
- [ ] 8 power cards (4 general, 2 per class)
- [ ] Basic power draft between rounds
- [ ] First to 5 win condition
- [ ] Placeholder pixel art + basic 3D arena

### Alpha (Full Gameplay)
- [ ] All 8 classes playable
- [ ] 3 arenas
- [ ] 40+ power cards (general + per-class)
- [ ] Level-on-win tier system
- [ ] Environment interaction (destructible torches, platforms)
- [ ] Power stacking
- [ ] Basic UI (character select, draft screen, HUD, scoreboard)
- [ ] Online multiplayer (basic)

### Beta (Polish)
- [ ] All 6 arenas
- [ ] 80+ power cards
- [ ] Rollback netcode
- [ ] Final pixel art for all classes and animations
- [ ] Full 3D environment art with lighting
- [ ] Sound design and music
- [ ] Cosmetic unlocks
- [ ] Balance pass (card tuning, HP adjustments, parry window)

### Stretch Goals
- Additional classes post-launch
- Ranked/competitive mode
- Tournament spectator mode
- Map editor / custom arena support
- Seasonal events with themed cards
- Cross-platform play

---

## Appendix A: Design Decisions Log

| Decision | Options Considered | Chosen | Rationale |
|----------|-------------------|--------|-----------|
| Who gets power cards? | Winner only / All losers / All players | All losers | Creates catch-up mechanic; winning feels dangerous (only player not powered up) |
| Class specials? | Yes (unique E ability) / No | No specials | Every ability must be subject to card chaos. Specials would be exempt or require 8x card design |
| Melee classes? | Traditional melee / Ranged-only / "Melee through ranged" | Melee through ranged | Rogue throws knives, Warrior lobs axes. Keeps all combat projectile-based while preserving melee fantasy |
| Duplicate classes? | Unique pick / Draft / Duplicates allowed | Duplicates allowed | Party game accessibility. 4 Jesters should be possible |
| Card selection count? | 3 / 4 / 5 options | 4 | Enough variety without decision paralysis |
| Draft order? | Simultaneous / Sequential by elimination | Sequential | First eliminated = first pick. Softens elimination sting, adds strategic layer |
| Winner reward? | Nothing / Flat stat bonus / Level system | Level (unlocks stranger card tiers) | Future investment rather than immediate power. Prevents sandbagging |
| HP model? | Universal / Per-class | Per-class (2-4) | Class identity includes durability. Warrior=4 HP justifies axe retrieval risk |
| Art style? | Full 3D / Full 2D / 2D-on-3D pixel | 64x64 pixel sprites on 3D | Distinctive visual identity. Faster character production. Evokes classic RPG nostalgia |
| Round timer? | Clock / Shrinking stage / Camera zoom | Camera zoom | Visual drama, synergizes with environment destruction, shows 3D depth |
| Power creep control? | Card rarity / Card limits / Positive+negative | Cards always have negatives | Core to the Rounds identity. Negatives create stories, prevent snowballing |
| Power stacking? | No / Yes / Yes with caps | Yes (some caps) | Enables focused builds. Stacked negatives are the natural limiter |

---

## Appendix B: Open Questions

- [ ] Exact parry window (6 vs 8 frames) — needs playtesting
- [ ] Camera zoom speed curve — needs playtesting
- [ ] Card balance methodology — spreadsheet? Playtest-driven? Both?
- [ ] How many starting axes for Warrior? (Currently 3 — may need adjustment)
- [ ] Do environment hazards (fire, lava) interact with power cards? (e.g., "immune to fire" card)
- [ ] Should eliminated players spectate or have a mini-game while waiting?
- [ ] Announcer personality — serious fantasy or comedic?
- [ ] How to handle disconnects mid-match in online mode?
- [ ] Should there be a "rematch" quick option after a match ends?
- [ ] AI opponents for practice mode — how sophisticated?
