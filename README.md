# Tank Arena

A top-down 2D tank arena shooter built with **Unity 6** and **C#**, inspired by *Tanki Online*.
Drive your tank, aim the turret with the mouse, survive waves of enemy tanks that hunt you down, and grab supply boxes for temporary power-ups.

> University project · Work in progress · **Status: Week 1 demo**

[Main menu]
image.png

---

## Table of contents

1. [Concept](#concept)
2. [Week 1 progress report](#week-1-progress-report)
3. [Controls](#controls)
4. [Game mechanics](#game-mechanics)
5. [Technical overview](#technical-overview)
6. [Project structure](#project-structure)
7. [Assets and credits](#assets-and-credits)
8. [Getting started](#getting-started)
9. [Roadmap](#roadmap)
10. [Known limitations](#known-limitations)
11. [Author](#author)

---

## Concept

*Tank Arena* is a fast arena shooter. You are alone in an enclosed map; enemy tanks keep spawning outside your view, chase you, and shoot back. Every enemy destroyed adds to your **kill count**. When you die the run ends and the map restarts from zero.

The gameplay ideas are borrowed from *Tanki Online*:

- a tank made of two independent parts: a **hull** that drives and a **turret** that aims on its own
- **supply boxes** that spawn on the map and give temporary bonuses
- a **garage** for customizing the tank (hull, turret, color) *(planned)*
- several **weapon types** *(planned)*

The game is 2D instead of 3D, which keeps the scope realistic for one semester and lets the focus go to gameplay systems and polish.

---

## Week 1 progress report

The original goal for week 1 was a barebones demo (driving, aiming, shooting, one target). That core is finished, and several systems planned for later weeks are already in place.

### Status overview

| Area | Status |
|---|---|
| Player tank (hull movement, independent turret, shooting) | Done |
| Health and damage system | Done |
| Enemy tanks with AI (chase, keep distance, aim, shoot) | Done |
| Off-screen enemy spawning | Done |
| Power-up system (3 power-ups, collectible boxes) | Done |
| Kill counter and death / restart flow | Done |
| HUD (health bar, kills, power-up timers) | Done |
| Main menu with map selection (maps are scenes) | Done |
| Animations and visual effects | Done |
| Sound system | Done (final sound effects still to be added) |
| Garage (tank customization) | Placeholder screen only |
| Additional weapons and maps | Planned |

### What was built

**Core gameplay**
- Tank driving with physics (forward/back and hull rotation), collisions with walls and obstacles
- Turret that rotates independently and follows the mouse cursor
- Shooting with a fire-rate cooldown; bullets with damage, lifetime and "who fired me" information so teams don't hurt themselves
- Reusable health component used by the player and every enemy
- Smooth camera that follows the player on a map bigger than the screen

**Enemy AI**
- Enemies turn their hull toward the player and drive after them
- They hold a preferred distance: they stop at a comfortable range and **back away** if the player gets too close, so they never ram the player
- The turret rotates separately from the hull at its own speed and only fires when it is actually pointing at the player and in range
- Optional line-of-sight check so enemies don't shoot through walls
- Enemies **deliberately collect power-ups** that are very close to them (details below)

**Spawning**
- Enemies spawn at random positions that are outside the camera view and not inside any obstacle, with limits for the maximum number of enemies and the spawn interval
- Power-up boxes spawn randomly and far apart from each other

**Power-ups** — three effects, 15 seconds each, usable by the player and by enemies (see [Game mechanics](#power-ups))

**Interface**
- Main menu with *Select Map*, *Garage* and *Exit*; each map is its own scene, so a new map is just a new scene and one button
- Animated panel transitions, button hover / press effects with sounds, fade between scenes
- In-game HUD: health bar (with a "damage trail"), kill counter, and one card per active power-up
- Low-health warning (pulsing red screen edges) and a death screen showing the final kill count

**Visuals and audio**
- Sprite-based tanks (separate hull and turret sprites) with animated tracks
- Sprite-sheet animations for explosions and exhaust smoke
- Tiled ground texture, textured walls and obstacles (crates, rocks, barrels, tank traps)
- Pixel-art UI kit: panels, buttons in three states, bars and icons
- Sound helper with distance-based volume; UI and power-up sounds

**Tooling**
- Custom Unity editor tools that build a complete map (ground, walls, obstacles) and the UI screens with one menu click, so new maps and layout changes take seconds

---

## Controls

| Input | Action |
|---|---|
| `W` / `Up Arrow` | Drive forward |
| `S` / `Down Arrow` | Drive backward |
| `A` / `Left Arrow` | Rotate hull left |
| `D` / `Right Arrow` | Rotate hull right |
| Mouse | Aim the turret |
| Hold `Left Mouse Button` | Fire |

---

## Game mechanics

### Tank movement
The hull is a physics object (`Rigidbody2D`). Forward and backward input sets the velocity along the direction the tank is facing; left and right set its angular velocity. Because the movement goes through the physics engine, the tank stops correctly at walls instead of passing through them. The turret is a child object that rotates toward the mouse position in world space, independent of where the hull points.

### Shooting and damage
A single `Weapon` component is shared by the player and enemies. It enforces the fire rate and spawns a bullet prefab at the barrel tip. Bullets are trigger colliders: on contact they deal damage to anything with a `Health` component, spawn their hit effect and disappear. Each bullet remembers the tag of its shooter and ignores that team.

### Health and death
Every tank has a `Health` component. When health reaches zero the tank plays an explosion effect and sound, then is destroyed. If it was an enemy, a kill is added to the score; if it was the player, the game-over flow starts.

### Enemy AI
Each physics frame an enemy compares its distance to the player with two thresholds:

| Distance to player | Behavior |
|---|---|
| Farther than the preferred distance | Drive toward the player |
| Between the two thresholds | Stand still |
| Closer than the "too close" distance | Reverse away while still facing the player |

The gap between the two thresholds prevents jittering back and forth. The turret is handled separately: it rotates toward the player at its own speed and fires only when the angle error is small and the target is in range. This makes enemies fair and dodgeable.

### Enemy spawning
The spawner picks random points on the map and rejects any point that is visible on screen (checked with viewport coordinates) or overlaps a collider. It keeps trying until it finds a valid point, and respects a maximum enemy count so a new enemy appears soon after one is destroyed.

### Power-ups

| Box | Icon | Effect (15 s) |
|---|---|---|
| Red | Starburst | **Double damage**: bullets deal twice the damage |
| Blue | Shield | **Double armor**: incoming damage is halved (equivalent to doubled health) |
| Yellow | Lightning bolt | **Double speed**: movement speed is doubled |

- Collect a box by driving into it. Boxes bob, glow and blink shortly before they disappear.
- Collecting the same type again **refreshes** the timer to 15 s; different types run **at the same time**.
- A colored aura appears under any tank with an active effect, enemies included.
- Boxes spawn **sparsely**: a minimum distance between boxes, a minimum distance from the player, a maximum count at once, and a lifetime.
- **Enemies can collect them too, and do so intentionally**, but only with some common sense: the box must be within a short radius, the enemy ignores a box if it already has that effect with time left, and it won't leave the fight while the player is right next to it. While driving to a box its turret keeps aiming and shooting at the player.

Implementation note: every tank (player or enemy) has a `PowerUpReceiver` that remembers when each effect ends and sets multipliers on the weapon, health and movement scripts. The same code therefore works for both.

### Score and game flow
The score is the **kill count**. It starts at 0 whenever a map loads. When the player dies, a "YOU DIED" screen shows the final kills and, after a few seconds, the map reloads, which resets everything including the counter.

### Maps
Each map is a separate Unity scene (the menu is one too), so adding a map means duplicating a scene and adding a menu button. A custom editor tool places the ground, outer walls and a symmetric layout of obstacles (bunkers, crate clusters, tank-trap lines, rocks, barrel groups) and can be re-run after tweaking the layout.

---

## Technical overview

### Tech stack

| | |
|---|---|
| Engine | Unity 6 (2D project) |
| Language | C# |
| Input | Unity Input System package |
| UI | Unity UI (uGUI) and TextMeshPro, 9-slice sprites, Canvas Scaler (1920 × 1080 reference) |
| Animation | Animator controllers and sprite-sheet clips |
| Physics | Rigidbody2D, Collider2D, triggers |
| Version control | Git |

### Main scripts

| Script | Responsibility |
|---|---|
| `TankMovement`, `TurretAim`, `PlayerShooting` | Player input: driving, aiming, firing |
| `Weapon`, `Bullet` | Shared shooting logic and projectile behavior |
| `Health` | Damage, armor multiplier, death |
| `EnemyAI` | Chasing, distance keeping, turret aiming, power-up seeking |
| `EnemySpawner` | Off-screen enemy spawning |
| `CameraFollow` | Smooth follow camera |
| `PowerUpReceiver` | Active effects and their multipliers on a tank |
| `PowerUpPickup`, `PowerUpSpawner` | Collectible boxes and their sparse spawning |
| `PowerUpAura`, `PickupBurst`, `GlowSprite`, `PowerUpStyle` | Power-up visuals |
| `GameManager` | Kill counter, death screen, restart |
| `HealthBarUI`, `PowerUpHUD`, `UIPunch` | HUD elements |
| `MainMenu`, `UIButtonEffects` | Menu navigation, transitions, button effects |
| `OneShotEffect`, `ExhaustSmoke`, `TrackAnimationControl` | Effects and animation control |
| `Sfx` | Sound playback with distance-based volume |
| Editor: `MapGenerator`, `UIBuilder`, `PowerUpBuilder` | Tools that build maps and UI screens |

### Concepts and techniques used

- Component-based design: small single-purpose scripts that are combined on prefabs, with `Weapon`, `Health` and `PowerUpReceiver` shared between player and enemies
- Prefabs and runtime `Instantiate` for bullets, enemies, boxes and effects
- `FixedUpdate` for physics movement, `Update` for input and visuals, `LateUpdate` for the camera
- Singleton (`GameManager`) and shared static lists (all power-up boxes in the world) for loose coupling between systems
- Coroutines for timed sequences (death screen, fades, scene transitions)
- Unscaled time for UI so effects keep running when the game is frozen
- Simple behavior logic with distance thresholds for the AI, angle-based turret control, and `Mathf.MoveTowardsAngle` for smooth rotation
- Viewport-space checks for off-screen spawning; `OverlapCircle` to avoid spawning inside walls
- Unity Events wired to buttons, 9-slice UI sprites, CanvasGroup fades
- Custom editor scripting (`MenuItem`, asset importers, prefab editing) to automate content creation

### Challenges solved

- **Sprites face a different direction than the code expects.** The tank art faces right while the movement code treats "up" as forward. The art lives on rotated child objects, so no gameplay code had to change.
- **Independent hull and turret for enemies.** Hull movement (physics, `FixedUpdate`) and turret aiming (visual, `Update`) are handled separately, which also lets enemies shoot while driving to a power-up.
- **Power-ups without duplicating code.** A multiplier-based receiver means damage, armor and speed bonuses work identically for the player and for enemies.
- **Switching to the new Input System.** All input code uses the current Input System API instead of the legacy `Input` class.
- **Pixel-art rendering.** Point filtering, full-rect sprite meshes and a fixed pixel scale keep tiled textures and UI borders crisp.

---

## Project structure

```
Assets/
├── Scripts/        Gameplay, AI, UI and effects scripts
├── Editor/         Editor tools (map generator, UI builders)
├── Scenes/         MainMenu, Map_01, ...
├── Prefabs/        Player, Enemy, Bullet, PowerUpBox, effects
├── Textures/       Tanks, map, UI and pickup textures
├── Animations/     Animator controllers and clips
└── Audio/          Sound effects
```

---

## Assets and credits

**Third-party assets**
- Tank sprites, animations and effect sprite sheets: *Tanks & Turrets* by *Lootzfir* — *(https://assetstore.unity.com/packages/2d/tanks-turrets-free-216890)* — *License: Standard Unity Asset Store EULA*
- Pixel font *Thaleah Fat* by *Rick Hoppmann* — *(https://assetstore.unity.com/packages/2d/fonts/free-pixel-font-thaleah-140059)* — *License: Standard Unity Asset Store EULA*

**Project assets**
- UI kit (panels, buttons, bars, icons), menu background, map textures (walls, crates, rocks, barrels, tank traps), power-up box artwork, and placeholder UI / pickup sounds were created for this project.

**Inspiration**
- *Tanki Online* — only gameplay ideas were borrowed. No assets, names or code from the original game are used.

---

## Getting started

**Requirements:** Unity 6 with the 2D module, plus the Input System and TextMeshPro packages (TextMeshPro essentials imported).

1. Clone the repository and open the folder with Unity Hub.
2. Open the scene `Assets/Scenes/MainMenu`.
3. Press **Play**, choose *Select Map* and then a map.

Make sure the scenes are listed under **File > Build Profiles > Scene List**, with `MainMenu` first.

---

## Roadmap

- [x] **Week 1** — Core gameplay, enemy AI, power-ups, HUD, menu, visuals
- [ ] **Garage** — choose hull, turret and color; save the selection between sessions
- [ ] **Weapon variety** — rapid fire, shotgun, rocket and other projectile types, each with its own bullet and effects
- [ ] **Enemy pathfinding** — navigate around walls instead of driving straight at the player
- [ ] **Game-over screen** — Restart and Main Menu buttons instead of the automatic reload
- [ ] **More maps** — new scenes with different layouts
- [ ] **Audio pass** — final sound effects and background music
- [ ] **Difficulty scaling** — enemy count and strength grow over time
- [ ] **Polish and release** — bug fixing, balancing, standalone build, gameplay video, itch.io page

---

## Known limitations

- Enemies steer straight toward the player and do not avoid walls, so long walls can trap them. The first map uses small, spread-out obstacles to work around this until pathfinding is added.
- The Garage screen is a placeholder.
- Sound effects are placeholders and will be replaced in the audio pass.

---

## Author

**Lăslău Andrei-Marius** — Fundamental concepts of programming languages — Facultatea de Automatică și Calculatoare, Timișoara, Year 3
Supervisor: Paiusan Razvan
