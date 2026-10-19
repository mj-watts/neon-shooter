# Localized glow

Bloom uses a four-level blur pyramid, 1.5 intensity, 0.9 threshold, 0.45 scatter,
and a 4.0 input clamp. Runtime settings and the saved volume profile agree.
Music and explosion pulses add at most 8% and 12% to bloom intensity.
Explosion light clouds are smaller and less opaque; sparks and their trails
retain their existing colours, geometry, and lifetimes.

The Play Mode regression renders six simultaneous, overlapping explosions.
It compares distant strips against the same scene without explosions and limits
the fraction of the frame that becomes nearly white. The existing emitter test
also requires visible bloom and a radial halo outside the solid object.

Measured with Unity 6000.6.6f1 / URP 17.6.0, Metal, 1280×800:

| Measurement | Before | After |
| --- | ---: | ---: |
| Added distant display brightness | 0.592 | 0.000 |
| Nearly white frame area | 7.62% | 1.00% |
| Bloom brightness added outside isolated emitter | — | 0.0205 |

All nine Play Mode and 69 Edit Mode tests passed in an isolated copy of the project, allowing the
main editor to remain open. Evidence is in `TestResults/LocalGlow` (ignored by Git).
These measurements describe the regression scene rather than every possible
encounter or display resolution.
