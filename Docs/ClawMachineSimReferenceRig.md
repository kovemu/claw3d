# Claw Machine Sim reference rig — verified extraction notes

Source build: Unity 2021.3.45f2. Values below were read from the uploaded `level2`, `sharedassets*.assets`, `globalgamemanagers.assets`, `Assembly-CSharp.dll`, and `Obi.dll` files. This document intentionally separates **verified serialized values** from our prototype-only approximations.

## Active gameplay rig in `level2`

The active physics set is under:

- `ClawPhysics` (GameObject path 818)
  - `Obi Solver` (858)
    - `CLAW_Testing01 (1)` (5021)
    - `ClawMain.002` (4557) — central claw rigidbody
    - `single claw - pivot fixed 1` (4207)
    - `single claw - pivot fixed 2` (4530)
    - `single claw - pivot fixed 3` (4533)
    - `Obi Rope` (208)
    - `MOVER (1)` (518)

Do not use the similarly named inactive template copies under `clawMachine rope claw america/CLAW_Testing01`; their serialized Rigidbody values differ.

## Unity timing

- Project fixed timestep: **0.02 s (50 Hz)**
- Active `ObiFixedUpdater`: **4 substeps** per FixedUpdate

## Carriage / `ClawMoveModule`

Serialized active `ClawMoveModule` on the machine root:

- `speed`: **0.007** per FixedUpdate
- `velocityBasedMovement`: **false**
- `returnAxisAtATime`: **true**
- `delayToOpen`: **0.4 s**

`MOVER (1)` Rigidbody:

- mass: **1.0**
- useGravity: **false**
- isKinematic: **true**

## Central claw rigidbody

Active `ClawMain.002` (path 4557 / Rigidbody path 13694):

- mass: **0.25**
- drag: **0**
- angularDrag: **0.05**
- useGravity: **true**
- isKinematic: **false**
- collisionDetection: **Discrete**

SphereCollider on the head:

- radius: **0.1913362145** in the source object's local scale
- center: **(0, 0.0970459357, 0)**

Source transform:

- local position: **(-0.0466144383, 0.8998636007, -4.2447080612)**
- local scale: **(0.2227894217, 0.2227894217, 0.2227894217)**

Mesh reference:

- `sharedassets2.assets`, Mesh path **928**
- mesh name: **ClawMain.002**

## Three physical claw arms

Each active arm Rigidbody has:

- mass: **0.25**
- drag: **0**
- angularDrag: **0.05**
- useGravity: **true**
- isKinematic: **false**
- interpolation: **Interpolate**
- collisionDetection: **Continuous**

Each HingeJoint has:

- connectedBody: central `ClawMain.002` Rigidbody
- anchor: **(0,0,0)**
- axis: **(0,0,1)** in the source arm transform
- autoConfigureConnectedAnchor: **true**
- useSpring: **false**
- useMotor: **false**
- useLimits: **true**
- min: **0°**
- max: **45°**
- bounciness: **0**
- bounceMinVelocity: **0.2**
- contactDistance: **0**
- breakForce / breakTorque: **Infinity**
- enableCollision: **false**
- enablePreprocessing: **true**
- massScale / connectedMassScale: **1**

Arm 1 (`single claw - pivot fixed 1`):

- position: **(-0.0843545794, 0.8091999888, -4.2666306496)**
- yaw: approximately **-30°**
- connectedAnchor: **(-0.1694001108, -0.1807000637, -0.0983999595)**

Arm 2 (`single claw - pivot fixed 2`):

- position: **(-0.0468846560, 0.8091710806, -4.2010211945)**
- yaw: approximately **90.6212°**
- connectedAnchor: **(-0.0012125362, -0.1808356196, 0.1960922629)**

Arm 3 (`single claw - pivot fixed 3`):

- position: **(-0.0088104010, 0.8091711998, -4.2662758827)**
- yaw: approximately **-150°**
- connectedAnchor: **(0.1696868688, -0.1808356196, -0.0968077555)**

Each arm mesh reference:

- `sharedassets2.assets`, Mesh path **920**
- mesh name: **ClawMain.004**

### Arm collision geometry

Each arm uses the same child collision layout. Source primitives use default Unity CapsuleCollider/BoxCollider dimensions, with shape produced by Transform scale/position.

Four capsule children:

1. local position **(-0.0741, -0.0054, 0)**, scale **(0.0381, 0.0758, 0.0381)**
2. local position **(-0.1877, -0.0608, 0)**, scale **(0.0381, 0.0758, 0.0381)**
3. local position **(-0.2605, -0.1643, 0)**, scale **(0.0381, 0.0758, 0.0381)**
4. local position **(-0.2923, -0.2789, 0)**, scale **(0.0381, 0.0758, 0.0381)**

Two box children:

1. local position **(-0.2426, -0.4408, 0)**, scale **(0.0131, 0.0896, 0.0609)**
2. local position **(-0.2774, -0.3633, 0)**, scale **(0.0131, 0.0896, 0.0609)**

The capsule/box colliders use the `maxFriction` PhysicMaterial in the base configuration.

## Claw PhysicMaterials

Verified serialized PhysicMaterial assets:

### `maxFriction`

- dynamicFriction: **10**
- staticFriction: **10**
- bounciness: **0**
- frictionCombine: **Maximum**
- bounceCombine: **Average**

### `highFriction Claw`

- dynamicFriction: **0.75**
- staticFriction: **0.75**
- bounciness: **0**
- frictionCombine: **Maximum**
- bounceCombine: **Average**

### `icey`

- dynamicFriction: **0.30**
- staticFriction: **0.30**
- bounciness: **0**
- frictionCombine: **Minimum**
- bounceCombine: **Maximum**

## Active `ClawRope`

The active `ClawRope` MonoBehaviour on `CLAW_Testing01 (1)` serializes:

- loweringSpeed: **0.004**
- loweringDistance: **0.55**
- timeToClose: **0.5 s**
- timeToOpen: **1.5 s**

It references:

- active `Obi Rope` actor (path 17911)
- active `ObiRopeCursor`
- `ClawGrabAnimator`

## Obi rope asset structure

Active rope actor uses the `Claw Rope Small` ObiRopeBlueprint (`sharedassets2.assets`, path 2543).

Important distinction discovered during extraction:

- blueprint `activeParticleCount`: **3**
- blueprint `initialActiveParticleCount`: **3**
- serialized positions/restPositions array capacity: **103**

So **103 is the reserved particle pool capacity, not the initial active rope particle count**. `ObiRopeCursor` can activate/deactivate pooled particles while changing rest length. Earlier prototype assumptions that the rope permanently contained 5 or 103 active particles were incorrect.

The first three blueprint positions are approximately:

1. `(0.000000805, 0.898205221, 0.000000477)`
2. `(0.000122267, 0.910933971, -0.000396789)`
3. `(0.000000954, 0.925329208, 0.000000477)`

The remainder of the 103-position pool is initially unused/zero-filled.

Two `ObiParticleAttachment`s bind the rope dynamically to:

- top: `MOVER (1)` transform, particle group `start`
- bottom: active `ClawMain.002` transform, particle group `end`

## Claw arm control

`ClawReferences` contains exactly three `ClawArm` entries, each holding:

- the arm Rigidbody
- the arm Transform
- its `RayCastTip` Transform

The gameplay code drives the arm Rigidbody angular velocity directly. The HingeJoint is used as a physical constraint/limit only; it is not used as a spring or motor.

## Difficulty values already verified

Realistic mode values recovered from the active `ClawDifficultyManager` include:

- normal velocity: **10**
- strong velocity: **10**
- dead velocity: **5**
- dying initial velocity: **15**
- dying delayed velocity: **7**
- dying delay: **7 s**
- grab drag: **10**
- grab angular drag: **30**

Normal mode:

- normal velocity: **11**
- strengthened velocity: **12**
- failed tries before strong mode: **3**

## Next extraction target

Before replacing the rope implementation again, finish mapping these source objects:

1. `ObiRopeBlueprint` inverse-mass / radii / phase arrays
2. `ObiSolver` constraint iteration parameters
3. `ObiRopeCursor` exact rest-length insertion/removal behavior
4. active head/finger mesh materials and renderer references
5. original machine visual hierarchy and local transforms

Only after these are mapped should the current custom PBD rope be treated as replaceable rather than tuned further.
