# ReferenceClone — core three wiring

This is the acceptance boundary for the first clean rebuild. Only three behaviors are in scope:

1. trolley movement
2. rope down/up
3. physical finger open/close

Everything below is tied to the supplied DLLs plus the previously extracted active `level2` objects. No legacy prototype controller behavior should be imported.

## Source build / active hierarchy

Unity source build: **2021.3.45f2**.

Active physical rig in `level2`:

- `ClawPhysics`
  - `Obi Solver`
    - `CLAW_Testing01 (1)` — active claw root
    - `ClawMain.002` — central dynamic claw Rigidbody
    - `single claw - pivot fixed 1`
    - `single claw - pivot fixed 2`
    - `single claw - pivot fixed 3`
    - `Obi Rope`
    - `MOVER (1)` — kinematic carriage body

Do not base the rebuild on the similarly named inactive claw copies.

---

# Gate A — trolley movement

## Component wiring

Active `ClawMoveModule`:

- mover Transform -> `MOVER (1)`
- mover Rigidbody -> `MOVER (1)` Rigidbody
- serialized `speed` -> **0.007**
- `velocityBasedMovement` -> false, and unused by the canonical named source path
- `velocitySpeed` -> 0, and unused by the canonical named source path
- `returnAxisAtATime` -> **true**
- `delayToOpen` -> **0.4 s**

`MOVER (1)` Rigidbody:

- mass **1.0 kg**
- gravity off
- kinematic on

## Required behavior

For each movement call:

- scale input by 0.007
- evaluate X and Z bounds independently
- call `Rigidbody.MovePosition` with the accepted new position
- no acceleration / deceleration controller

Return-to-home must be **Z first, then X** when `returnAxisAtATime` is true.

### Gate-A pass criteria

- no custom acceleration logic
- one fixed-step input produces the same fixed position step as source
- axis bounds reject only the blocked axis
- return order is Z -> X

---

# Gate B — rope down / up

## Component wiring

Active `ClawRope`:

- `cursor` -> active `ObiRopeCursor`
- `rope` -> active `ObiRope`
- `loweringSpeed` -> **0.004**
- `loweringDistance` -> **0.55**
- `timeToClose` -> **0.5 s**

Active rope initial structural state:

- initial active particles: **3**
- pooled capacity: **103 total**
- initial elements:
  - 0 -> 1: **0.012735528**
  - 1 -> 2: **0.014401228**
- initial rest length: **0.027136756**
- inter-particle distance: **0.021475287**
- bend constraints off on the actor
- self collisions off
- stretch compliance 0

Active updater:

- Unity fixed timestep **0.02 s**
- Obi fixed updater **4 substeps**

Attachments:

- source particle 2 -> `MOVER (1)` via dynamic particle attachment
- source particle 0 -> `ClawMain.002` via dynamic particle attachment
- both zero compliance

## Required source flow

Drop:

- on `FullGrab`, set claw rope state to lowering
- each physical update: `cursor.ChangeLength(rope.restLength + 0.004)`
- finish lowering when rest length reaches `initialRestLength + 0.55`
- call physical `CloseClaw`
- wait **0.5 s**
- switch to raising

Raise:

- each physical update: `cursor.ChangeLength(rope.restLength - 0.004)`
- when it reaches the original rest length, call `ChangeLength(originalRestLength)` once more to clamp exactly
- clear claw rope state
- machine enters returning

There must be **no custom Transform teleport or custom home-grown rope solver** in this gate. The source game delegates physical rope behavior to Obi.

### Gate-B pass criteria

- source-compatible Obi rope/cursor are used
- rest length changes by 0.004 per physical update
- 3 active particles grow through the pool as cursor pays rope out
- physical claw head is carried by the dynamic attachment, not manually positioned
- raise returns to the exact original rest length

---

# Gate C — physical finger open / close

## Component wiring

Three physical arms are represented in code as `ClawArm` objects with:

- arm Rigidbody
- arm Transform
- `RayCastTip`

Each active source HingeJoint:

- connected body -> central `ClawMain.002` Rigidbody
- local axis -> **(0,0,1)**
- useSpring false
- useMotor false
- useLimits true
- min **0°**
- max **45°**
- enableCollision false

Each arm Rigidbody:

- mass **0.25 kg**
- gravity on
- angular drag **0.05** from the Rigidbody serialization itself
- Continuous collision detection
- interpolation Interpolate

Central head Rigidbody:

- mass **0.25 kg**
- gravity on
- drag 0
- angular drag 0.05
- Discrete collision detection

## Required source actuation

The source does not command a target hinge angle.

Every `ClawModule.PhysicsUpdate()`:

- read current `ClawSettings.clawVelocity`
- multiply by the persistent open/close direction integer
- send the scalar to each `ClawArm`
- each arm sets its Rigidbody angular velocity along `armTransform.forward`

Direction state:

- `OpenClaw()` -> direction **-1**
- `CloseClaw()` -> direction **+1**

The HingeJoint limits are the mechanical stops.

There is no source-equivalent finger target servo or dead-zone angle controller.

## Difficulty setting effect

For the attached `Assembly-CSharp` build, `ClawSettings` contains:

- material
- claw velocity
- angularDrag
- drag

But external gameplay references show:

- material is applied to arm colliders
- claw velocity drives arm angular velocity
- `ClawSettings.angularDrag` is **not read by gameplay code**
- `ClawSettings.drag` is **not read by gameplay code**

Therefore these two `ClawSettings` fields must not be applied to Rigidbody damping in the first reference rebuild.

## Visual-event wiring

The active `level2` serialized data contains persistent UnityEvent calls from the physical claw events to `Claw.ClawGrabAnimator` methods:

- physical close event -> `ClawGrabAnimator.CloseClaw`
- physical open event -> `ClawGrabAnimator.OpenClaw`

`ClawGrabAnimator` is visual/tween behavior; it is not the physical actuator.

### Gate-C pass criteria

- idle/open command drives continuously toward the open mechanical Hinge limit
- close command drives continuously toward the closed mechanical Hinge limit
- fingers are not servoed to a scripted target angle
- arm collision can physically obstruct one finger while other fingers continue receiving their angular-velocity command
- no grab-setting damping is injected unless a later source trace proves such a call exists

---

# First ReferenceClone success definition

The rebuild does not proceed to toys, map, scoring, camera polish, collection, or difficulty tuning until all three are simultaneously true:

- mover behaves correctly
- rope completes one full down/up cycle through Obi without instability
- fingers stay open, close physically at the bottom, remain closed during raise/return, and open again at the machine's opening state

Only after these three gates match should the rest of `ClawMachine` be reconstructed.
