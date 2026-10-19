# Browser visual reference — 15 October 2026

The browser app is the visual reference; subsequent Neon Shooter work targets
this Unity project unless instructed otherwise. Browser source files were left
unchanged.

## Fireworks upgrade

The later request for over-the-top fireworks supersedes the original effect
strength match. Unity now uses four staggered spark shells, roughly twice as
many burning sparks, tapered comet tails, and coloured blast light lasting
1.05 seconds instead of a 65 ms flash. Projectiles have larger, brighter halos.
Native URP bloom is 4.5 with a 0.65 threshold and 0.88 scatter.

Fresh native Unity screenshots are in `TestResults/Fireworks/bursts.png` and
`TestResults/Fireworks/trails.png`; these show the same three size-1.5 blasts
at about 0.4 and 0.95 seconds. The earlier browser comparison below is retained
as the style baseline. Rendering readback now measures 0.0819 additional
brightness outside the bloom emitter, versus 0.0388 before this upgrade.
The current native Play Mode suite passes all 5 tests, including persistent
blast light, ember lifetime, cleanup, and bounded glow material reuse.

## Matched screens

Captured the browser and Unity at **1280×800**, including actual UI, for:

- Launch / briefing
- Flight and HUD
- Three explosions (pink, cyan, gold; size 1.5; about 0.4 seconds after emission)
- Pause with blurred backdrop
- Game over with personal best and latest battle

`TestResults/VisualParity/comparison.png` contains all five side-by-side pairs.
The individual `compare-*.png` files retain the full 1280×800 image for each side.
Reference and Unity captures remain individually available as well.

Source mapping:

| Browser reference | Unity implementation |
| --- | --- |
| `TermsShooter.vue`, `terms-shooter.css` | `Runtime/NeonGame.UI.cs`, `Runtime/ArcadeButtons.cs` |
| `NeonShooter.js` palettes, player scale, particles | `Runtime/VoxelAssets.cs`, `Runtime/NeonGame.cs` |
| `FleetDesigns.js` | `Core/SpriteCatalog.cs` |
| `CanyonBackground.js`, `TerrainScenery.js` | `Resources/Shaders/Canyon.shader` |
| Exhaust and round-particle GLSL | `Resources/Shaders/Exhaust.shader`, `Spark.shader` |
| `BlockExplosionRing.js` | Four delayed, 48-fragment rings in `NeonGame.cs`, `ExplosionBlock.shader` |
| UnrealBloom / ACES | Native URP volume in `Runtime/NeonBloom.cs` |

The original OffBit, OffBit101 and Nohemi fonts are retained. Neue Power was
losslessly decompressed from the source WOFF2 into a Unity-importable TTF;
`browser-font-conversion.json` records both checksums.

## Verification

- **63 Edit Mode tests passed**: Metal shader compilation, pipeline configuration,
  font/palette/control style, simulation and menu audio.
- **5 Play Mode tests passed**: native frame readback, all encounter renderers,
  launch/pause, actual bloom and radial glow, explosion expiry, bounded material
  reuse and soundtrack lifecycle.
- **121 simulation checks passed**, including browser-reference scenarios and a
  five-minute simulated flight.
- Media hashes, scene references, assembly definitions and converted font hashes
  pass `Tools/verify-assets.py`.
- Both authored launch surfaces match the reference RGB values: background
  **(8, 3, 24)** and button background **(20, 1, 40)**. Saved checks:
  `TestResults/VisualParity/colour-checks.json`.
- In the normal-motion bloom fixture, display-space brightness outside a small
  emitter increases by **0.0388** with bloom; a radial halo adds **0.7669**.
  The test explicitly selects normal motion so saved user preferences do not
  alter the fixture, and converts linear HDR readback to display space before
  comparison. Diagnostic results: `TestResults/visual-parity-glow.txt`.

Native test results: `TestResults/visual-parity-editmode.xml` and
`TestResults/visual-parity-playmode.xml`. Tests and screenshots ran in an
isolated project copy, preserving the open project's flight and saved records.

## Repeat the screenshots

Start the sibling browser dev server on port 5173, then run:

```sh
node Tools/capture-browser-reference.mjs
```

For an isolated Unity checkout, start the Editor with:

```sh
/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /path/to/isolated/unity-neon-shooter \
  -executeMethod NeonShooter.Editor.FlightPreview.Start \
  -neonPreviewExit -neonVisualParity -logFile /tmp/neon-visual-reference.log
```

The explicit `-neonVisualParity` mode sets up temporary in-memory reference
scenes and records; ordinary play and the regular preview menu do not use these
fixtures. Screenshots wait for file completion, use a fixed Game-view render
resolution on Retina displays, and capture all five states.

The rendering engines use different bloom kernels, font rasterizers and GPU
noise precision. Terrain patterns and individual random explosion fragments
therefore differ. The Unity briefing retains native Sound / Past Battles controls, and does not reproduce the surrounding website's long
scrolling article. Screenshot comparisons support the matched visual style,
not a claim of bit-identical rendering or complete animation parity.
