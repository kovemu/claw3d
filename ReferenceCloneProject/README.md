# ReferenceCloneProject

This is a separate Unity project for the Claw Machine Sim study/reconstruction path.

## Editor

Open **this subfolder** in Unity Hub:

`claw3d/ReferenceCloneProject`

Target editor: **Unity 2021.3.45f2**.

Do not open this reference clone using the repository root's Unity 6 project.

## Local-only source material

The repository ignores:

`ReferenceCloneProject/Assets/ReferenceOriginal/`

Keep extracted/reference-only third-party material there. Do not commit it.

For the Obi gate, the clean-room `ClawRope.cs` is compiled only when the scripting define symbol below is present:

`CLAW_REFERENCE_OBI`

Before enabling that symbol, a compatible local Obi assembly/package must already be available in the Unity project. The supplied/extracted Obi binary is reference material and is intentionally not stored in GitHub.

## Current canonical layer

Implemented from the DLL/source map without using the failed Unity 6 prototype architecture:

- `Claw.Module`
- `Claw.ClawSettings`
- global `ClawArm`
- `Claw.ClawModule`
- `Claw.ClawMoveModule`
- `Claw.ClawRope` (only with `CLAW_REFERENCE_OBI`)
- `Claw.ClawMachine` state shell

Temporary non-canonical test code lives under `Assets/Scripts/Debug` and must not be treated as source behavior.

## Gate 1 scope

Only these are allowed before the next phase:

1. mover: direct 0.007 fixed-step `Rigidbody.MovePosition`
2. rope: `ObiRopeCursor.ChangeLength(restLength +/- 0.004)`
3. arms: `rb.angularVelocity = command * armTransform.forward`

No custom rope solver, target-angle finger servo, damping injection, prize logic, map polish, or toy logic belongs in this gate.
