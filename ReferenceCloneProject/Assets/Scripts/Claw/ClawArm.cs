using System;
using UnityEngine;

[Serializable]
public sealed class ClawArm
{
    public Rigidbody rb;
    public Transform trans;
    public Transform rayCastTip;

    public void SetAngularVelocity(float command)
    {
        if (rb == null || trans == null) return;
        rb.angularVelocity = command * trans.forward;
    }

    public void ApplyPhysicMaterial(PhysicMaterial material)
    {
        if (rb == null) return;

        Collider[] colliders = rb.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; ++i)
            colliders[i].material = material;
    }
}
