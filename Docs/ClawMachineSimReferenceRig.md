# Claw Machine Sim reference rig — verified extraction notes

Source build: Unity **2021.3.45f2**. Values below were read from the uploaded `level2`, `sharedassets*.assets`, `globalgamemanagers.assets`, `Assembly-CSharp.dll`, and `Obi.dll` files. The goal of this document is to keep **verified serialized values** separate from prototype approximations.

## Active gameplay rig in `level2`

The active physics set is under:

- `ClawPhysics` — GameObject 818
  - `Obi Solver` — GameObject 858
    - `CLAW_Testing01 (1)` — GameObject 5021
    - `ClawMain.002` — GameObject 4557, central claw Rigidbody
    - `single claw - pivot fixed 1` — GameObject 4207
    - `single claw - pivot fixed 2` — GameObject 4530
    - `single claw - pivot fixed 3` — GameObject 4533
    - `Obi Rope` — GameObject 208
    - `MOVER (1)` — GameObject 518

Do not use the similarly named inactive `CLAW_Testing01` / `CLAW_Testing01 (2)` template copies as the gameplay reference. Their serialized values differ.

## Unity / Obi timing

- Unity fixed timestep: **0.02 s = 50 Hz**
- Active `ObiFixedUpdater` MonoBehaviour path: **18390**
- updater solver list: active `Obi Solver` path **18391**
- `substeps`: **4**
- The uploaded target `Obi.dll` version has no serialized `substepUnityPhysics` field. Its `ObiFixedUpdater` serializes only the solver list plus `substeps`.

Therefore the rope solver is advanced four times per Unity fixed step.

## Active ObiSolver — exact serialized parameters

Active `ObiSolver` MonoBehaviour path **18391**:

### General

- `simulateWhenInvisible`: **true**
- backend enum: **1 = Burst**
- mode: **3D**
- interpolation: **None**
- gravity: **(0, -9.81, 0)**
- gravity space: **Self**
- damping: **0**
- max anisotropy: **3**
- sleep threshold: **0.0005**
- collision margin: **0.02**
- max depenetration: **10**
- continuous collision detection factor: **1**
- shock propagation: **0**
- surface collision iterations: **8**
- surface collision tolerance: **0.005**
- world linear inertia scale: **0**
- world angular inertia scale: **0**

### Constraint groups

All active groups use **SOR = 1** and **1 iteration**. Evaluation order:

- Distance: **Sequential**, enabled
- Bending: **Parallel**, enabled at solver level
- Particle collision: **Sequential**, enabled
- Particle friction: **Parallel**, enabled
- Collider collision: **Sequential**, enabled
- Collider friction: **Parallel**, enabled
- Skin: **Sequential**, enabled
- Volume: **Parallel**, enabled
- Shape matching: **Parallel**, enabled
- Tether: **Parallel**, enabled
- Pin: **Parallel**, enabled
- Stitch: **Parallel**, enabled
- Density: **Parallel**, enabled
- Stretch/shear: **Sequential**, enabled
- Bend/twist: **Sequential**, enabled
- Chain: **Sequential**, **disabled**

Important: the solver-level Bending group being enabled does **not** mean the active rope actor uses bend constraints. The active `ObiRope` actor disables its own bend constraints, described below.

## Carriage / `ClawMoveModule`

Active `ClawMoveModule` MonoBehaviour path **18546** on `clawMachine rope claw america`:

- `clawMoverTrans`: Transform path **5389** = `MOVER (1)`
- `clawMoverRb`: Rigidbody path **13492**
- `speed`: **0.007** per FixedUpdate
- `velocityBasedMovement`: **false**
- `velocitySpeed`: **0**
- `returnAxisAtATime`: **true**
- `delayToOpen`: **0.4 s**

`MOVER (1)` Rigidbody:

- mass: **1.0 kg**
- drag: **0**
- angular drag: **0.05**
- gravity: **off**
- kinematic: **on**

## Active `ClawRope` gameplay values

Active gameplay `ClawRope` MonoBehaviour is path **17939** on active `CLAW_Testing01 (1)`.

Its inherited `ClawModule` data serializes:

- base grab setting: claw velocity **10**
- base grab angular drag: **30**
- base grab drag: **10**
- distance check method enum: **2**
- `loweringSpeed`: **0.004**
- `loweringDistance`: **0.55**
- `timeToClose`: **0.5 s**
- `timeToOpen`: **1.5 s**

This confirms that the `.004` and `.55` values are not guesses from the prototype; they are the active game's serialized values.

## Active ObiRope actor — exact runtime serialization

Active `ObiRope` MonoBehaviour path: **17911**, on GameObject `Obi Rope` 208.

### Blueprint reference

It references:

- fileID **2** = `sharedassets2.assets`
- PathID **2543**
- asset name: **`Claw Rope Small`**

So the active claw does **not** use the other `Claw Rope` blueprint PathID 2544.

### Actor state

The saved actor contains a solver-index pool of **103** particles. Initial structural elements are only two:

1. particle **0 → 1**, rest length **0.012735528**
2. particle **1 → 2**, rest length **0.014401228**

Initial active rest length:

**0.012735528 + 0.014401228 = 0.027136756**

### Actor collision/constraint settings

- Obi collision material: `HighFriction`, PathID **2513**
- surface collisions: **false**
- self collisions: **false**
- tearing: **false**
- tear resistance multiplier: **1000**
- tear rate: **1**
- distance constraints: **enabled**
- stretching scale: **1**
- stretch compliance: **0**
- max compression: **0**
- bend constraints: **disabled**
- bend compliance: **0**
- serialized max bending: **0.275** — inactive because bend constraints are disabled
- plastic yield: **0**
- plastic creep: **0**

This corrects the old prototype assumption that the active rope had self-collision and soft bend stiffness. It does not. For this claw rope, the important solver behavior is primarily **distance constraints + dynamic pin attachments + substeps**.

## `Claw Rope Small` blueprint — exact particle data

`sharedassets2.assets`, PathID **2543**, name `Claw Rope Small`.

### Counts

- active particle count: **3**
- initial active particle count: **3**
- pooled particles: **100**
- total capacity: **103**

The initial three particles are:

1. `(0.000000805, 0.898205221, 0.000000477)`
2. `(0.000122267, 0.910933971, -0.000396789)`
3. `(0.000000954, 0.925329208, 0.000000477)`

### Mass/filter/radius

For the first three active particles:

- inverse mass: **10** → mass **0.1 kg** each
- filter: **-65534**
- principal radius: **(0.003, 0.003, 0.003)**
- initial velocity: **0**

The old note calling `-65534` a "phase" was based on an older Obi field name. In the uploaded target `Obi.dll`, this serialized array is named **`filters`**.

### Rope blueprint geometry values

Verified trailing blueprint fields:

- thickness: **0.003**
- resolution: **0.1**
- inter-particle distance: **0.021475287**
- total particles: **103**
- pooled particles: **100**

The `restLengths` pool begins with the two active element lengths above. Pooled segments use the blueprint inter-particle distance when `ObiRopeCursor` activates new particles.

This is a critical correction to the current prototype: the real game starts with a very short flexible rope containing three particles, then **adds pooled particles as the rope is paid out**. A fixed three-particle rope stretched over the entire 0.55 m drop is structurally wrong.

## Active ObiRopeCursor — exact values and behavior

Active `ObiRopeCursor` MonoBehaviour path **17912**.

Serialized fields:

- `m_CursorMu`: **0**
- `m_SourceMu`: **0**
- `direction`: **true**

This corrects the earlier guessed values `0.531 / 0.741`; those values were wrong and have been removed from the project config.

At runtime, `ChangeLength()` works by modifying the structural element at the cursor. When extending:

1. it first grows the current cursor element up to `interParticleDistance`;
2. if more length is required, it activates a pooled particle;
3. it inserts a new structural element at the cursor;
4. it repeats until the requested rest length is reached;
5. constraints/rest positions/rest length are rebuilt.

When shortening, it reverses the process and deactivates particles as whole elements are consumed.

This dynamic activation/deactivation behavior is the next rope implementation target. Our current fixed-count custom PBD rope does not reproduce it.

## Particle attachment topology — exact

Two active `ObiParticleAttachment` components are on the `Obi Rope` GameObject.

### Path 17907 — top/start

- actor: active ObiRope path **17911**
- target Transform: **5389 = MOVER (1)**
- particle group: fileID 2 / PathID **2491**, name `start`
- particle group contains source particle: **2**
- attachment type: **Dynamic**
- constrain orientation: **false**
- compliance: **0**
- break threshold: **Infinity**

### Path 17906 — bottom/end

- actor: active ObiRope path **17911**
- target Transform: **9006 = ClawMain.002**
- particle group: fileID 2 / PathID **2503**, name `end`
- particle group contains source particle: **0**
- attachment type: **Dynamic**
- constrain orientation: **false**
- compliance: **0**
- break threshold: **Infinity**

Therefore particle order is effectively:

`claw head ← particle 0 — particle 1 — particle 2 → MOVER`

The cursor starts at `mu=0`, on the head-side structural element. New pooled rope is inserted from that side while paying out.

### Source-space attachment offsets

From the saved blueprint positions and active transforms, the initial source-solver-space relationship is approximately:

- particle 2 relative to `MOVER (1)`: **(-0.00910, -0.10477, -0.00210)**
- particle 0 relative to the claw-head transform position: approximately **(0.000115, -0.001658, 0.000109)** before accounting for the head Transform's local scale in target-local coordinates

This explains another flaw in the prototype: the flexible Obi rope is **not attached to the MOVER center**. It starts roughly 0.105 units below it.

## Rope ObiCollisionMaterial

The active rope references Obi collision material `HighFriction`, PathID **2513**:

- dynamic friction: **1.0**
- static friction: **0.0**
- stickiness: **0**
- stick distance: **0**
- friction combine: **Maximum**
- stickiness combine: **Maximum**
- rolling contacts: **false**
- rolling friction: **0**

Do not confuse this Obi particle collision material with the Unity `PhysicMaterial` named `highFriction Claw` used by the claw fingers.

## Central claw Rigidbody

Active `ClawMain.002` GameObject 4557 / Rigidbody 13694:

- mass: **0.25 kg**
- drag: **0**
- angular drag: **0.05**
- gravity: **on**
- kinematic: **off**
- collision detection: **Discrete**

Active head collider is **CapsuleCollider** PathID **14605**, not the SphereCollider previously recorded from a different clone:

- radius: **0.11**
- height: **0.513159454**
- direction: **Y axis**
- center: **(0, -0.195496231, 0)**

Source head Transform:

- local position: **(-0.0466144383, 0.8998636007, -4.2447080612)**
- local scale: **(0.2227894217, 0.2227894217, 0.2227894217)**

Mesh reference:

- `sharedassets2.assets`, Mesh PathID **928**
- mesh name: **ClawMain.002**

## Three physical claw arms

Each active arm Rigidbody:

- mass: **0.25 kg**
- drag: **0**
- angular drag: **0.05**
- gravity: **on**
- kinematic: **off**
- interpolation: **Interpolate**
- collision detection: **Continuous**

Each active HingeJoint:

- connected body: central `ClawMain.002` Rigidbody
- anchor: `(0,0,0)`
- axis: `(0,0,1)` in the source arm transform
- autoConfigureConnectedAnchor: true
- spring: off
- motor: off
- limits: on
- min: **0°**
- max: **45°**
- bounciness: **0**
- bounceMinVelocity: **0.2**
- contactDistance: **0**
- breakForce / breakTorque: Infinity
- enableCollision: false
- enablePreprocessing: true
- massScale / connectedMassScale: 1

Arm positions / connected anchors:

- Arm 1: position `(-0.0843545794, 0.8091999888, -4.2666306496)`, connectedAnchor `(-0.1694001108, -0.1807000637, -0.0983999595)`
- Arm 2: position `(-0.0468846560, 0.8091710806, -4.2010211945)`, connectedAnchor `(-0.0012125362, -0.1808356196, 0.1960922629)`
- Arm 3: position `(-0.0088104010, 0.8091711998, -4.2662758827)`, connectedAnchor `(0.1696868688, -0.1808356196, -0.0968077555)`

Finger mesh:

- `sharedassets2.assets`, Mesh PathID **920**
- mesh name: **ClawMain.004**

### Arm collision geometry

Each arm uses four CapsuleCollider children and two BoxCollider children, plus a `RayCastTip` transform.

Capsules:

1. position `(-0.0741, -0.0054, 0)`, scale `(0.0381, 0.0758, 0.0381)`
2. position `(-0.1877, -0.0608, 0)`, scale `(0.0381, 0.0758, 0.0381)`
3. position `(-0.2605, -0.1643, 0)`, scale `(0.0381, 0.0758, 0.0381)`
4. position `(-0.2923, -0.2789, 0)`, scale `(0.0381, 0.0758, 0.0381)`

Boxes:

1. position `(-0.2426, -0.4408, 0)`, scale `(0.0131, 0.0896, 0.0609)`
2. position `(-0.2774, -0.3633, 0)`, scale `(0.0131, 0.0896, 0.0609)`

## Unity claw PhysicMaterials

### `maxFriction`

- dynamic friction: **10**
- static friction: **10**
- bounce: 0
- friction combine: Maximum
- bounce combine: Average

### `highFriction Claw`

- dynamic friction: **0.75**
- static friction: **0.75**
- bounce: 0
- friction combine: Maximum
- bounce combine: Average

### `icey`

- dynamic friction: **0.30**
- static friction: **0.30**
- bounce: 0
- friction combine: Minimum
- bounce combine: Maximum

## Claw arm control

`ClawArm` in `Assembly-CSharp.dll` is a serializable holder containing:

- Rigidbody
- Transform
- RayCastTip Transform

The gameplay code drives the arm Rigidbody angular velocity directly. HingeJoint supplies the mechanical limits; it is not a spring/motor actuator.

## Difficulty values already verified

Realistic mode:

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

## Consequence for our current prototype

The current custom PBD rope is now known to differ from the source in several decisive ways:

1. source flexible rope starts at only **0.027136756** rest length, not the old prototype's 0.24 center-to-center cable length;
2. source top attachment is offset below the MOVER center;
3. source starts with **3 particles**, but activates pooled particles while extending;
4. source inter-particle distance is **0.021475287**, so a full 0.55 payout uses many more than three particles;
5. source rope actor has **bend constraints disabled**;
6. source rope actor has **self collisions disabled**;
7. source distance and pin constraints each use only **1 solver iteration**, compensated by **4 substeps**.

Therefore the existing fixed-count custom rope should not be tuned further. Its structure needs replacement.

## Next implementation target

Rebuild the learning/reference rope around the now-verified contract:

- head-side particle 0 ↔ dynamic head attachment
- top particle 2 ↔ dynamic MOVER attachment with source offset
- 3 active particles initially
- 100-particle reserve pool
- cursor at mu 0, source mu 0, positive direction
- element-level rest lengths, not uniform stretching of a fixed particle set
- particle activation/deactivation as rest length changes
- distance-only rope constraints for this actor
- 4 substeps / 1 distance iteration / 1 pin iteration

After that, compare the resulting motion before doing prize/toy tuning.
