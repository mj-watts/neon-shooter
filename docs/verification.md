# Verification on 2026-10-15

## Current project: Unity 6000.6.6f1, URP 17.6.0

The project was upgraded in Unity Hub from the original 6000.3 version to
6000.6.6f1. The current configuration uses URP rather than the deprecated
Built-in Render Pipeline. The saved pipeline is assigned in Graphics settings
and all six quality levels. The forward renderer includes native post-process
data; the saved flight volume profile includes Bloom and ACES Tonemapping.

### Rendering regression and fix

- Reproduced the reported magenta background in the native Metal shader compiler:
  `Canyon.shader` used HLSL's reserved identifier `point`, causing a syntax error
  and a subsequent undeclared-identifier error.
- Added and executed two Unity rendering regression tests before fixing the
  production code. Both failed for the expected reasons: canyon compilation and
  the absence of a URP pipeline. Evidence: `TestResults/rendering-red.xml`.
- Renamed the reserved identifier, converted all three game shaders to URP HLSL,
  and replaced the Built-in-only `OnRenderImage` bloom with native URP volumes.
- **57 Unity Edit Mode tests passed, 0 failed**, including shader compilation
  on Metal, shader support and the pipeline assignment at every quality level.
- **2 Unity Play Mode tests passed, 0 failed**, exercising launch, all
  powerups and encounter renderers, one audio listener, pause, shader support
  and all seven audio imports.
- The Play Mode check captures a 1280x800 offscreen frame through an explicit URP
  render request. It rejects blank frames and magenta error-shader coverage.
  The rendered frame was visually inspected: pink canyon shelves with yellow rims,
  scenery, ships and a red laser gate render successfully.
- Captured image: `TestResults/flight-urp.png`. Unity results are saved as
  `TestResults/editmode-urp.xml` and `TestResults/playmode-urp.xml`.
- Native tests ran in an isolated temporary copy at
  `/tmp/neon-shooter-render-validation` to avoid interrupting the open Editor.
  The tested source and generated pipeline assets are included in this project.

### Gameplay, compilation and assets

- **121 headless checks passed, 0 failed**: 54 gameplay behaviors, 66 browser
  reference scenarios and one five-minute simulated flight.
- Reference fixtures cover 18 power-state sequences, 36 canyon-wall samples and
  all 12 power definitions derived from the browser source.
- The five-minute simulated flight reached wave 67 and 1,772 kills, with a peak
  of 170 projectiles and 100 enemies. Positions stayed finite and lists bounded.
- Production C# compiles against the installed Unity 6000.6.6f1 engine/editor and
  imported URP assemblies with zero warnings and zero errors.
- Seven MP3s and four fonts match the browser source byte-for-byte; SHA-256 hashes
  are recorded in `asset-manifest.json`. Three TTF fonts are runtime imports;
  WOFF2 is preserved as a source archive.
- Scene/component GUID references and all five assembly definitions pass the
  asset verification script.

The initial license-activation blocker has been resolved. A complete manual
playthrough, audible playback, standalone builds and real-device touch testing
remain separate checks. No measured coverage percentage or pixel-perfect
browser-rendering parity is claimed.

## Bloom, ground registration and button regressions

- Four new simulation checks reproduced missing lateral movement for turrets,
  tanks, wrecks/gates and the assault fleet, plus the incorrect deep-shelf scroll
  rate. They pass with the shared terrain surface transform.
- A native bloom regression reproduced an average brightness contribution of
  only 0.00174 outside a small cyan emitter. Updated bloom contributes about
  0.0376, and the emissive radial halo adds about 0.418 in that test. The check
  toggles the actual Volume setting and compares GPU-rendered pixels.
- The button regression checks white text in all eight GUIStyle states, a
  20-point block font and at least 56-unit button targets.
- Captured and visually inspected actual Game-view screenshots of the briefing,
  flight and pause screen. They show bloom/glow, neon button borders, large
  targets and white labels. The preview also reproduced stale UI sizing between
  multiple Game views; recalculating the UI viewport during OnGUI fixes it.
- Previews: `TestResults/briefing-ui.png`, `TestResults/flight-ui.png`, and
  `TestResults/paused-ui.png`. Effects measurements: `TestResults/glow-readback.txt`.

## Pink and yellow theme

- Removed the previous game branding from the authored project and runtime UI.
  A case-insensitive scan of project source and asset bytes found no remaining
  references.
- Recolored the briefing, HUD, buttons, ship/fleet palettes, civilian and ground
  assets, projectiles, pickup glows and procedural terrain. Pinks and roses form
  the base, with lemon-yellow and gold highlights; text remains white.
- All 57 Unity Edit Mode tests and both Play Mode tests pass after recoloring.
- Visually inspected fresh briefing and flight captures in the Unity Game view;
  the updated previews are stored in `TestResults`.

## Menu soundtrack and audio reaction

- User-supplied MP3 copied byte-for-byte to `Resources/MenuAudio/menu-soundtrack.mp3`;
  its SHA-256 provenance is recorded in `docs/menu-soundtrack.json` and checked by
  `Tools/verify-assets.py`. The seven gameplay audio assets remain separate.
- Playback uses the audio DSP clock, a non-looping AudioSource and a random 2–4
  second gap measured from the clip end. Launch/briefing transitions stop the
  previous soundtrack before starting the next.
- Real output samples drive RMS energy and transient detection. Menu title, button
  borders, canyon tint, bloom, exhaust and ship dance respond to this signal.
  Sound Off silences both players.
- 62 Edit Mode tests and 4 Play Mode tests passed, including actual natural clip
  completion/restart and menu-to-flight transitions. 121 headless checks passed.
- Production C# compilation: zero warnings and errors. Asset verification passed.
- Actual Unity menu rendering was inspected with the supplied track playing.
  Measured reactive pulses changed from 0.933 to 0.794 between captures; screenshot
  saved as `TestResults/menu-music-ui.png`.

## Browser visual matching

The later browser-reference pass supersedes the earlier pink/yellow theme,
framed buttons and dancing menu preview. See [visual-parity.md](visual-parity.md)
for the source mapping, matched screenshots, colour readbacks and current
63 Edit Mode / 5 Play Mode results. The supplied menu soundtrack and native
record storage remain in use.

## Orb trails and independent ground drivers

- Removed the old brand from the two remaining runtime UI labels. A byte scan of
  170 authored files found zero matches, including text and imported media.
- All twelve pickup definitions match freshly generated browser fixtures: labels,
  colours, durations, weights and collection outcomes. Each has a native icon;
  the rendering smoke test activates the complete catalogue.
- Orbital guards leave bounded block ribbons and drifting particles. Trails
  follow the hero's position, freeze on pause, expire
  after the guard disappears and clear on relaunch or return to briefing.
- Ground cars use 80% scale, individual speeds around 2.5 times the previous
  pace, modest heading changes and local avoidance. The terrain sampler follows
  the canyon shader's parcel hashes and tree/building rules. Camera parallax
  applies immediately without suppressing independent steering. Cars re-enter
  below the arena while assault support remains active.
- 126 headless checks passed, including 66 browser-reference scenarios and the
  five-minute flight. Production compilation against the installed Unity
  assemblies completed with zero warnings and errors. Original media checksums,
  assembly references and scene GUID checks passed.
- Unity Test Runner: 68 Edit Mode and 5 Play Mode tests passed. XML results are
  `TestResults/addons-editmode.xml` and `TestResults/addons-playmode.xml`.
  Inspected the fresh native render `TestResults/addons-trails-and-cars.png`;
  this verifies the rendered appearance, rather than pixel parity with WebGL.
