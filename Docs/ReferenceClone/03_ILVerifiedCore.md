# ReferenceClone — IL-verified core behavior

Basis: user-supplied `Assembly-CSharp(1).dll`. This note records behavior reconstructed from the actual Mono IL and metadata rather than from the failed prototype.

## Exact field types now confirmed

`Claw.ClawMoveModule`:

- `MeshRenderer meshBounds`
- cached `Bounds`
- `Transform clawMoverTrans`
- `Rigidbody clawMoverRb`
- `float speed`
- `bool velocityBasedMovement`
- `float velocitySpeed`
- `bool returnAxisAtATime`
- `float delayToOpen`

`ClawArm`:

- `Rigidbody rb`
- `Transform trans`
- `Transform rayCastTip`

`Claw.ClawRope` adds only:

- `ObiRopeCursor cursor`
- `ObiRope rope`
- cached initial rest length

## ClawModule IL

`CloseClaw()`:

- writes direction integer `+1`
- invokes `OnCloseClaw` if non-null

`OpenClaw()`:

- writes direction integer `-1`
- invokes `OnOpenClaw` if non-null

`FullGrab()`:

- writes internal claw state `lowering`
- invokes `OnStartGrab`

`PhysicsUpdate()`:

1. iterate `List<ClawArm>`
2. for every arm call its angular-drive method with `clawSettings.clawVelocity * directionInteger`
3. if internal state is `none`, return
4. if `lowering`, dispatch the virtual lowering slot
5. if `goingUp`, dispatch the virtual raising slot
6. `closing` does no per-step lower/raise work

The source `ClawArm` angular-drive method is exactly the mechanical command shape:

`rb.angularVelocity = scalar * trans.forward`

Material application gets the colliders below the arm Rigidbody and assigns the supplied `PhysicMaterial` to each collider. No `ClawSettings.drag` or `ClawSettings.angularDrag` application is present in this path.

## ClawRope IL

The rope does not replace `PhysicsUpdate()`. It overrides the lower/raise virtual slots called by `ClawModule.PhysicsUpdate()`.

Lowering slot:

- requested length = `rope.restLength + loweringSpeed`
- `cursor.ChangeLength(requested)`
- when `rope.restLength >= cachedInitial + loweringDistance`:
  - `CloseClaw()`
  - state = `closing`
  - start coroutine `(timeToClose, goingUp)`
  - stop `claw.rope.lower` loop sound

The delay coroutine:

- waits `WaitForSeconds(delay)`
- assigns the requested claw internal state
- starts `claw.rope.raise` loop sound

Raising slot:

- requested length = `rope.restLength - loweringSpeed`
- `cursor.ChangeLength(requested)`
- when `rope.restLength <= cachedInitial`:
  - `cursor.ChangeLength(cachedInitial)` for exact clamp
  - state = `none`
  - `ClawMachine.SetMachineState(returning)`
  - stop `claw.rope.raise` loop sound

## ClawMoveModule IL

`Initialize(ClawMachine)`:

1. cache `clawMoverTrans.position` as return origin
2. cache `meshBounds.bounds`
3. call base `Module.Initialize`

`MoveClaw(Vector2)`:

1. if inverted flag is true, swap input X/Y
2. multiply input by `speed`
3. cache old mover position
4. X candidate = old + `Vector3.right * input.x`
5. use `cachedBounds.Contains(candidate)`; only then apply the X delta
6. Z candidate = old + `Vector3.forward * input.y`
7. use `cachedBounds.Contains(candidate)`; only then apply the Z delta
8. accumulate `Vector3.Distance(new, old)`
9. `clawMoverRb.MovePosition(new)`

The named path contains no acceleration controller. The serialized `velocityBasedMovement` / `velocitySpeed` fields are not used by this method.

With `returnAxisAtATime == true`, `UpdateReturning()` dispatches an axis state machine:

- first return Z by repeatedly calling `MoveClaw(0, signToOriginZ)`
- detect crossing the original Z coordinate by a sign change
- then return X with `MoveClaw(signToOriginX, 0)`
- detect crossing the original X coordinate by a sign change
- enter `waitToOpen`
- start a delay coroutine using serialized `delayToOpen`
- delayed target state is `openingClaw`

This is more specific than the earlier approximation that used `MoveTowards`.

## ClawMachine IL

`CalledFixedUpdate()` ordering:

1. input module fixed update
2. `claw.PhysicsUpdate()`
3. fixed animation modules when enabled
4. if state == `returning`, `clawMove.UpdateReturning()`

`SetMachineState(state)` side effects relevant to Gate 1:

- `idle` -> `claw.OpenClaw()`
- `running` -> invoke `OnStartRound`
- `grabbing` -> `claw.FullGrab()` and enable action-camera path
- `returning` -> optional settings shortcuts; otherwise no immediate claw command
- `waitToOpen` -> get/reset moved distance and invoke returned-position event
- `openingClaw` -> `claw.OpenClaw()`, start hard-coded `0.6 s` completion coroutine, disable action-camera path
- `overwriteReturning` -> rewrite current state to `returning`

The opening completion coroutine waits, calls `SetMachineState(idle)`, tells the prize spawner the grab ended, then invokes `OnEndRound`.

## Implementation consequence

The new `ReferenceCloneProject` is being corrected against these IL facts. The legacy Unity 6 prototype is not the source of truth and should not be patched further.
