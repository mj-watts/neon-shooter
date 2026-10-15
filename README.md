# Unity Neon Shooter

Native C# Unity port of the game in the sibling `browser-neon-shooter` folder.
The simulation, renderer, input, audio and HUD run in Unity. No browser,
JavaScript runtime, web view, Node server or paid Asset Store package is needed.

## Open and play

1. Install **Unity 6000.6.6f1** and activate a Unity license in Unity Hub.
2. In Hub, choose **Add > Add project from disk**, and select this folder.
   You can also launch the installed Editor directly with `-projectPath`.
3. Open **Assets/Scenes/NeonShooter.unity** (or **Neon Shooter > Open Game Scene**).
4. Press the Editor's **Play** button, then **Launch Flight** in the Game view.

The saved scene already contains the game component. Camera, procedural meshes,
materials and audio sources are constructed at runtime. A bootstrap also permits
playing from an empty scene. The project uses **Universal Render Pipeline 17.6.0**. Its renderer, pipeline
and post-processing assets are included and assigned to every quality level.
Legacy input is enabled in the project settings.

The briefing, HUD, pause and game-over screens follow the browser game's
purple/cyan palette, original fonts, lime controls and copy. The canyon, ship
palettes, layered exhaust, radial sparks and staggered block explosion rings
use the browser renderer as their reference. Native URP provides HDR bloom and
ACES output in linear colour space.

The menu plays the supplied **The Return of the 8 Bit Era** track, with a random
**2–4 second silent gap** between repeats. Launching switches to gameplay music;
returning to the briefing restarts the menu track. Sound Off mutes both players.
Reduced Motion keeps effects gentler. Local native records remain separate from
the browser's records.

| Action | Control |
| --- | --- |
| Move | Mouse, arrows, O/P, W/S; A/D also supported |
| Fire | Hold Space or left mouse button |
| Homing missile | B |
| Pause / resume | Escape or Pause button |
| Restart | Fly Again or Return after game over |
| Launch from briefing | Launch Flight or Return |
| Touch | Drag to move and auto-fire; on-screen missile button |
| Audio | Sound On/Off |

Losing application focus pauses flight. The briefing includes reduced-motion,
personal bests and the last ten battles. Records use Unity PlayerPrefs and are
separate from browser localStorage. Abandoning a started flight via **Flight
Briefing** also records it. Sound and reduced-motion preferences persist locally.

## Included gameplay and assets

- Original block silhouettes and browser palettes for the hero, three enemy classes,
  four fleet fighters, all twelve pickup icons, stars and bright red hearts.
- Formation waves, occasional heavy ships, ten-point perfect-wave stars,
  magnetic pickups, three lives, damage immunity and the crash sequence.
- Twin guns, machine guns, twin lasers, mega laser, homing missiles,
  shields, speed boost, orbital guards, wing orbs, spread shots, scatter bombs,
  four-ship support fleet and six-vehicle assault fleet.
- Civilians with hit penalties and safe-passage rewards; tanks, aimed turrets,
  paired laser gates, capital ships with six independently destructible turrets,
  and the following 100-ship invader formation.
- Scrolling pink canyon shelves, contour maps, buildings, trees and roads.
  The terrain shader is translated from the browser GLSL to Unity ShaderLab/HLSL.
- Shared voxel meshes with GPU instancing, radial glows, HDR bloom, exhaust, debris and explosion rings.
- All seven original MP3s copied byte-for-byte, including the music and voice
  clips. Browser-generated effects are recreated as synthesized Unity AudioClips.
- Three original TTF fonts imported; the original WOFF2 is preserved under
  `docs/SourceAssets` because Unity's font importer uses the TTF assets here.

This is a native recreation. Matched 1280×800 screenshots are saved under
`TestResults/VisualParity`; see [visual comparison](docs/visual-parity.md).
URP and WebGL use different rasterizers, bloom kernels and procedural noise
precision, so the screenshots demonstrate visual style rather than bit-identical pixels.
Generated sound envelopes, wrecks and some fine animation are approximations. The website's scroll-to-launch transition is replaced by a
native briefing. Refer to [port mapping](docs/port-map.md) for implementation detail.

## Add-on refinements

All twelve pickup types are checked against the sibling browser catalogue. Rotating
orbital guards leave a shrinking block ribbon and drifting particles, with shorter
trails in Reduced Motion. Ground cars render at 80% of their former size and have
individual speeds around 2.5 times their former pace. They steer gently around
other vehicles, ground defences and the shader's procedural trees and roofs, then
re-enter below the arena while assault support is active.

The briefing and HUD use the Neon Shooter name. In the Editor, **Neon Shooter >
Verify Add-ons** runs Edit Mode or Play Mode checks and saves XML under TestResults.

## Checks

Run the engine-independent checks using the .NET 8 SDK or newer:

```sh
cd /Users/mike.watts/Documents/Code/tmp/unity-neon-shooter
dotnet run --project Tests/Headless/NeonShooter.Checks.csproj
```

This requires no NuGet packages: it runs 59 gameplay checks plus 66 reference
scenarios derived from the browser's actual rules, canyon calculation and power
catalog, plus a five-minute simulated-flight check (126 checks total).
The gameplay, rendering, font/palette and menu-audio checks are exposed to
Unity's Edit Mode Test Runner.
To refresh the browser reference fixture after intentionally changing the source:

```sh
node Tools/browser-parity.mjs ../browser-neon-shooter
```

In Unity, choose **Window > General > Test Runner**, then **Run All** for EditMode
and PlayMode. The PlayMode smoke test launches a flight, exercises all powerups
and encounter renderers, checks shader support and seven audio imports, and
verifies pause. It is a smoke test, not a full manual playthrough. It also captures an offscreen
flight frame and checks for magenta shader errors and an empty render. A second Play Mode
check compares rendered bloom on/off and measures the radial glow outside an
emitter.

**Neon Shooter > Capture UI and Flight Previews** saves actual Game-view images
of the briefing, flight and pause screen to `TestResults`. Run it outside Play Mode.

**Neon Shooter > Validate Project** checks shader compiler errors, audio imports
and the scene component. To run the checks from the command line, close other
Editor instances using this project first:

```sh
UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod NeonShooter.Editor.ProjectTools.Validate -logFile Logs/validate.log
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" -runTests \
  -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" -runTests \
  -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log
```

The optional `Tools/CompileAgainstUnity.csproj` checks the production C# against
an installed Editor's real assemblies without starting it. Its `UnityManaged`
MSBuild property can be overridden for other installation locations. This check
cannot validate native initialization, shader compilation, import or rendering.

See [verification notes](docs/verification.md) for the evidence available on this
machine. No coverage percentage or complete gameplay/visual parity is claimed.

## Build

Choose **Neon Shooter > Build macOS** to create `Builds/macOS/Neon Shooter.app`.
For Windows, Linux, mobile or Web, install the relevant Editor build support in
Hub and use **File > Build Profiles** with the included scene. Those targets need
separate device/build validation; touch support alone does not prove a mobile build.

## License and source

The source project's MIT license is retained in `LICENSE`. Copied audio and fonts
retain their existing rights; their provenance and hashes are recorded in
[asset-manifest.json](docs/asset-manifest.json). The source checkout does not
provide separate redistribution licenses for those media files, so this port does
not invent or relicense them.

Unity reference: [migrating to URP](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp).
