# ReferenceClone — canonical claw source map

Status: **source-analysis reference only**. Do not implement from the old prototype until the behavior below is reproduced independently.

This map was reconstructed from the user-supplied Mono assemblies:

- `Assembly-CSharp(1).dll` SHA-256 `67d00745c78ee2dedd62d37e3c421c37fb5471703885d53c09dbe018a989f436`
- `Assembly-CSharp-firstpass(1).dll` SHA-256 `921f551ef2ff938e2a4d3f531f6980baeff8d1fbbd60804676c9933626248fa1`
- `Obi(1).dll` SHA-256 `1f53f31de36b2efbe7ae864707ff9605b753e33e0c813ed7e77990640709976f`

The assembly is obfuscated and contains many alias/decoy methods. The map below uses named methods and the real overrides/call sites that are reached by the canonical gameplay path. It intentionally records behavior rather than copying decompiled source text.

## 1. Canonical class ownership

### `Claw.Module`

Base MonoBehaviour for machine modules.

- one field: owning `ClawMachine`
- `Initialize(ClawMachine)` only stores the owner reference

### `Claw.ClawMachine`

Owns the high-level machine state and calls the modules.

Important fields:

- `curState`
- `settings : ClawMachineSettings`
- `inputModule`
- `clawMove : ClawMoveModule`
- `claw : ClawModule`
- camera / prize / animation references
- round and return events

Machine-state enum `HDOPLDFNJEI`:

| Value | Source name |
|---:|---|
| 0 | `off` |
| 1 | `idle` |
| 2 | `running` |
| 3 | `grabbing` |
| 4 | `returning` |
| 5 | `waitToOpen` |
| 6 | `openingClaw` |
| 7 | `overwriteReturning` |

### `Claw.ClawMoveModule`

Owns only trolley/mover translation and return-to-home behavior.

Important serialized fields:

- `meshBounds`
- `clawMoverTrans`
- `clawMoverRb`
- `speed`
- `velocityBasedMovement`
- `velocitySpeed`
- `returnAxisAtATime`
- `delayToOpen`

Verified code-reference result:

- `velocityBasedMovement`: **no gameplay references in this build**
- `velocitySpeed`: **no gameplay references in this build**

The named `MoveClaw(Vector2)` always uses direct position stepping and `Rigidbody.MovePosition`.

### `Claw.ClawModule`

Base physical claw controller.

Important fields:

- `List<ClawArm> claws`
- `ClawSettings clawSettings`
- lowering / distance-check fields
- `timeToClose`
- `timeToOpen`
- internal rope/grab state
- open/close direction integer
- `OnCloseClaw`, `OnOpenClaw`, `OnStartGrab`

Internal state enum `KJGKPEKMFFJ`:

| Value | Source name |
|---:|---|
| 0 | `none` |
| 1 | `lowering` |
| 2 | `closing` |
| 3 | `goingUp` |

### `Claw.ClawRope : ClawModule`

Adds exactly three fields:

- `ObiRopeCursor cursor`
- `ObiRope rope`
- cached initial rope rest length

It does not implement its own rope physics. It changes `ObiRope.restLength` through `ObiRopeCursor`.

### `ClawArm`

Serializable physical-arm holder:

- `Rigidbody rb`
- `Transform trans`
- `Transform rayCastTip`

Its canonical angular-drive operation is simply:

`rb.angularVelocity = command * trans.forward`

The arm also provides a downward raycast from `rayCastTip` and a material-application helper that assigns a `PhysicMaterial` to all colliders below its Rigidbody.

### `Claw.ClawSettings`

Fields:

- `PhysicMaterial clawPhysicMat`
- `float clawVelocity`
- `float angularDrag`
- `float drag`

Critical reference scan result:

- `clawVelocity` is read by `ClawModule.PhysicsUpdate`
- `clawPhysicMat` is read when the active settings are applied
- `angularDrag` has **no external gameplay read in this assembly**
- `drag` has **no external gameplay read in this assembly**

Therefore the old clone behavior that applied `drag/angularDrag` to the finger Rigidbodies was not source-equivalent.

### `Claw.ClawDifficultyManager`

Fields include:

- `overwriteType`
- `List<RealisticGrabSetting> settings`
- `pickRandomForDifficult`
- `claw`
- normal and normal-strong settings
- failed-try threshold / counter
- weighted grab-type list (the "hat")
- current grab type
- realistic-mode flag

Grab enum `BGIKCCCJDLL`:

| Value | Source name |
|---:|---|
| 0 | `none` |
| 1 | `deadGrab` |
| 2 | `dyingGrab` |
| 3 | `normalGrab` |
| 4 | `StrongGrab` |

### `Claw.RealisticGrabSetting`

Fields:

- `probability`
- `canRepeat`
- `grabType`
- `clawSettings`
- `changeSettingsAfterDelay`
- `delay`
- `delayedSetting`

### `Claw.ClawGrabAnimator`

This is a visual/tween animation component. It is **not** the physical finger actuator.

`OpenClaw()` / `CloseClaw()` animate configured tween data and handle the close sound. Physical fingers are driven separately by `ClawModule.PhysicsUpdate()`.

---

# 2. The three core behaviors to reproduce first

No prize logic, cabinet work, collection system, or extra polish should be added until these three behaviors match.

## A. Trolley movement

Canonical `MoveClaw(Vector2 input)` flow:

1. Optional input-axis swap if the module's inverted flag is set.
2. Multiply input by serialized `speed`.
3. Read current mover Transform position.
4. Test the proposed X movement against cached `meshBounds.bounds`.
5. Apply X only if the bounds check succeeds.
6. Test the proposed Z movement independently.
7. Apply Z only if its bounds check succeeds.
8. Add `Vector3.Distance(newPosition, oldPosition)` to the distance accumulator.
9. Call `clawMoverRb.MovePosition(newPosition)`.

There is no acceleration controller in the named source path.

Return-to-home behavior:

- if `returnAxisAtATime == true`, the source returns **Z first, then X**
- after the return finishes, machine state becomes `waitToOpen`
- a coroutine waits `delayToOpen`, then requests `openingClaw`

This corrects the old prototype, which returned X before Z.

## B. Rope lower / raise

`ClawRope` caches the original `rope.restLength` during initialization.

### Start grab

- base `FullGrab()` sets the claw internal state to `lowering`
- invokes `OnStartGrab`
- rope subclass starts loop sound `claw.rope.lower`

### Lowering, every physical update

Conceptual behavior:

1. requested length = current `rope.restLength + loweringSpeed`
2. `cursor.ChangeLength(requestedLength)`
3. when actual `rope.restLength >= initialRestLength + loweringDistance`:
   - call `CloseClaw()`
   - set internal state to `closing`
   - wait `timeToClose`
   - then change internal state to `goingUp`
   - stop lower sound / start raise sound

### Raising, every physical update

1. requested length = current `rope.restLength - loweringSpeed`
2. `cursor.ChangeLength(requestedLength)`
3. when rest length reaches or passes the initial rest length:
   - call `cursor.ChangeLength(initialRestLength)` again for an exact clamp
   - set internal claw state to `none`
   - tell owner machine to enter `returning`
   - stop raise sound

Important: `ClawRope` does **not** manually move the claw-head Transform/Rigidbody. Obi dynamic attachments are responsible for coupling rope constraints to the Rigidbody.

## C. Physical finger open / close

This is the most important correction from the failed prototype.

`ClawModule` does not calculate a target hinge angle.

There is no source-equivalent logic such as:

- interpolate 0° ↔ 45° target
- compare `hinge.angle` with target
- dead-zone controller
- stop when target is reached in script

Instead:

- `OpenClaw()` sets the command multiplier to **-1** and invokes `OnOpenClaw`
- `CloseClaw()` sets the command multiplier to **+1** and invokes `OnCloseClaw`
- every `PhysicsUpdate()` loops all physical arms and commands:
  - `clawSettings.clawVelocity * directionMultiplier`
  - `ClawArm` turns that scalar directly into `Rigidbody.angularVelocity` along `arm.trans.forward`
- the physical HingeJoint limits stop the arm at its mechanical limit

So for the reference clone the HingeJoint is the mechanical stop; code should not act as a servo-to-angle controller.

---

# 3. High-level source state flow

The canonical machine path is:

`idle -> running -> grabbing -> returning -> waitToOpen -> openingClaw -> idle`

During `grabbing`, `ClawRope` itself has its own sub-state:

`lowering -> closing -> goingUp -> none`

`ClawMachine.CalledFixedUpdate()` ordering is important:

1. input module fixed update
2. `claw.PhysicsUpdate()`
3. fixed animation modules, when present
4. if machine state is `returning`, `clawMove.UpdateReturning()`

`openingClaw`:

- calls `claw.OpenClaw()`
- starts a **hard-coded 0.6 second** coroutine
- disables the action camera
- after the coroutine: returns machine to idle, ends the prize-spawner grab, invokes end-round event

`ClawModule.timeToOpen` exists as a serialized field but the attached `ClawRope` gameplay path does not read it. The machine's canonical opening-completion delay is the hard-coded 0.6 s above.

---

# 4. Difficulty behavior relevant to the physical claw

`ClawDifficultyManager.Start()` subscribes its `OnStartGrab` handler to `ClawModule.OnStartGrab`.

Normal mode:

- failed counter is incremented at the start of each grab
- if it is below the threshold, `settingsNormal` is applied
- if it reaches/exceeds the threshold, `settingsNormalStrong` is applied
- winning a prize resets the failed counter to zero

Realistic mode:

- all pending delayed-setting coroutines are stopped
- choose a grab type from the configured weighted settings or an overwrite type
- if a selected type is not repeatable and equals the current type, selection retries, with a ten-attempt safety limit in the canonical path
- apply the selected setting's `clawSettings`
- optionally wait its configured delay and apply `delayedSetting`

Applying a `ClawSettings` changes the active `ClawSettings` reference and reapplies the finger PhysicMaterial. In this assembly it does not directly apply the `drag` or `angularDrag` fields to Rigidbody properties.

---

# 5. Obi behavior verified from the supplied `Obi.dll`

`ObiRopeCursor.ChangeLength(float)` is responsible for changing structural rope length.

Verified behavior:

- clamps requested length to rope capacity
- shrinking consumes/removes cursor-side structural elements and deactivates particles
- growing first expands the cursor element toward `interParticleDistance`, then activates/copies pooled particles and inserts new structural elements as needed
- new particles are copied from the configured source particle and placed on the cursor edge
- after topology/length changes it calls:
  - recalculate rest positions
  - recalculate rest length
  - rebuild constraints from structural elements

`ObiFixedUpdater.FixedUpdate()` performs:

1. prepare frame
2. begin fixed step
3. split the fixed step into configured substeps
4. simulate all substeps
5. end step and write results back

The active source scene's updater was previously extracted as four substeps.

---

# 6. Rules for the new ReferenceClone

1. Freeze the legacy `ClawController`, `ClawFinger`, `MachineController`, and custom `ClawRopeConstraint`; do not use them as the new architecture.
2. Recreate source class boundaries instead of wrapping all behavior into a new controller.
3. No guessed physics values or helper behavior are allowed in the canonical layer. Unknowns stay explicitly unknown until extracted.
4. First acceptance gate contains only:
   - trolley movement
   - rope down/up cycle
   - physical open/close
5. Each gate must be compared against source behavior before moving on.
6. Original proprietary binaries/assets remain local reference material and are not added to the public repository.
