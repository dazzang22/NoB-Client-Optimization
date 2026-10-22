# Target Selection Reproduction

This scene preserves the current production `OutlineSelection` algorithm and demonstrates its target-selection behavior.

## Open and run

1. Open `Assets/Tests/TargetSelection/TargetSelectionRepro.unity`.
2. Enter Play Mode.
3. Press `1`–`4`, or use the four Game View buttons, to change cases.
4. Read the Game View panel. Red is Candidate A, green is Candidate B.

The controller changes only the candidates' positions. The production `OutlineSelection` component calculates `ClosestObject`.

## Scene contract

- Camera position: `(0, 0, 0)`, FOV `60`
- Production max interaction distance: `1.5`
- Candidate tag: `SelectableDrawing`
- Both candidates are active cubes with the production `Outline` component.
- Candidate positions are serialized for CASE 2 so that Play Mode opens on the primary counterexample.

## Current HEAD results

| Case | Candidate A | Candidate B | Expected | Current actual |
|---|---|---|---|---|
| 1 | `(0, 0, 0.8)`, on-screen | `(0, 0, 1.4)`, on-screen | A | A |
| 2 | `(1, 0, 0.2)`, off-screen | `(0, 0, 1.4)`, on-screen | B | `null` |
| 3 | `(0, 0, -0.8)`, behind camera | `(0, 0, 1.4)`, on-screen | B | A |
| 4 | `(1, 0, 0.2)`, off-screen | `(-1.2, 0, 0.5)`, off-screen | `null` | `null` |

The precise viewport values depend on Game View aspect ratio. The supplied positions leave a wide margin for the off-screen cases.

## Regenerate the scene

Use `Tools > Target Selection Repro > Generate Scene` if the generated scene needs to be rebuilt. This replaces only the reproduction scene.
