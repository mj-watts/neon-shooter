# Browser to Unity mapping

| Browser source | Unity implementation |
| --- | --- |
| `ShooterGame.js` | `Assets/NeonShooter/Core/ShooterGame.cs`, native simulation data in `GameTypes.cs` |
| `Powerups.js` | `PowerCatalog` in `GameTypes.cs`; original durations, labels, colors and weights |
| `FleetDesigns.js`, `BlockIcons.js`, `NeonShooter.makeShip` | `Core/SpriteCatalog.cs`, silhouettes extracted into C# with themed fleet palettes |
| `NeonShooter.js` | `Runtime/NeonGame.cs` orchestrates rendering, input, effects and HUD |
| `CanyonTerrain.js` | `CanyonTerrain` in `Core/GameTypes.cs` |
| `CanyonBackground.js`, `TerrainScenery.js` | `Resources/Shaders/Canyon.shader` |
| `CapitalShip.js`, `CivilianShip.js`, `GroundDefenses.js` | `Runtime/VoxelAssets.cs` shared generated meshes |
| `UnrealBloomPass`, custom halos | `Runtime/NeonBloom.cs`, native URP Bloom/Tonemapping volumes, `Glow.shader` |
| Web Audio generated effects | `Runtime/FlightAudio.cs` PCM synthesis and event cooldowns |
| `src/assets/sound/*.mp3` | `Resources/Audio/*.mp3`, unchanged bytes |
| `public/fonts/*.ttf` | `Resources/Fonts/*.ttf`, unchanged bytes |
| `public/fonts/neue-power.woff2` | `docs/SourceAssets/neue-power.woff2`, unchanged bytes, not a Unity runtime font |
| Vue launch UI, HUD, keyboard/mouse/touch input | `Runtime/NeonGame.cs` native IMGUI and Unity input |
| `PersonalBests.js`, `BattleHistory.js` | `Runtime/RunHistory.cs` PlayerPrefs, local to the Unity application |

## Architecture

The core assembly has no Unity references and accepts an input value for each
step. `Advance` clamps long frame deltas to 0.1 seconds and subdivides to 120 Hz,
matching the browser. Movement keeps the browser's world units and aspect rules:
height is `max(32, 24 / aspect)` so portrait screens retain a 24-unit-wide arena.

The runtime owns a camera looking down the positive Z axis. Browser depth is
inverted: the foreground player and shots use negative Z, ground installations
use positive Z, the capital deck sits below them and the canyon is furthest away.
Voxel geometry uses vertex colors and shared meshes, avoiding a GameObject and
material per pixel. Meshes and glow quads are submitted in GPU-instanced batches
of up to 1,023 instances. URP's forward renderer and native Render Graph apply bloom and ACES tone
mapping through the saved `FlightPostProcessing` volume profile.

Effects are bounded to 1,600 particles. The core tracks projectiles, pickups,
falling ships and encounters; rendering reads that state without owning combat.
Capital turrets remain targetable independently of the large hull. Gate beams
exist only while both emitters remain, and use a swept vertical player check.
Missiles home on enemies and turrets, and patrol when no target is visible.

Ground units use the same surface coordinates as the canyon shader. The deep
shelf travels at 0.36 times scenery speed, the middle at 0.67 and the near shelf
at 1.08. Turrets, tanks, wrecks and gate emitters share the deep shelf's scroll
and lateral offset; assault vehicles use the near shelf's offset. Offsets are
applied in simulation, so weapons, targeting and collisions follow the visible
positions. `CanyonTerrain` supplies the shared rates and lateral offsets to the
simulation and renderer.

Native URP bloom uses HDR highlights, a 0.65 threshold and an intensity of 4.5. Radial halos retain emissive color and rotate with
beams. Button styles explicitly set white text for all eight IMGUI states and
use 56-unit square targets with neon borders. UI sizing is recalculated during
OnGUI for the active viewport, avoiding stale sizing between Editor Game views.

## Deliberate adaptations and remaining parity limits

- Browser DOM/CSS and page scrolling become a native briefing and HUD.
- Browser localStorage is not imported. Unity maintains its own recent flights
  and independent personal-best score/wave. The browser's separate lexicographic
  best-battle record is not exposed as a separate Unity screen.
- Web Audio filters/reverb and Three.js tone mapping/bloom are not identical to
  Unity's PCM effects and native URP bloom. Voice clips/music are unchanged files.
- Ground/ship silhouettes are retained and use the pink and yellow theme. Fine civilian gestures,
  warning highlights, smoke trails, muzzle flashes and cinematic transitions are
  simplified. Collision and powerup checks do not establish pixel-level parity.
- The source's lives, scoring, power timers, upgrade levels, supply weights,
  civilian penalties/rewards and main encounter timings are retained. This is a
  hand-written C# port, not an automatic translation with complete state parity.

## Verification boundaries

The .NET checks execute the actual C# core. The installed-assembly compilation
checks actual Unity API signatures. Unity Editor import/test runs additionally
check the native project and shader toolchain. A rendered flight frame is
captured in Play Mode; real-device/audio checks are separate. See `verification.md`.
