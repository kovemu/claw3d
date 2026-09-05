using System;
using UnityEngine;

namespace Claw
{
    [Serializable]
    public sealed class ClawSettings
    {
        public PhysicMaterial clawPhysicMat;
        public float clawVelocity;
        public float angularDrag;
        public float drag;

        public ClawSettings(PhysicMaterial material, float velocity, float angularDragValue, float dragValue)
        {
            clawPhysicMat = material;
            clawVelocity = velocity;
            angularDrag = angularDragValue;
            drag = dragValue;
        }
    }
}
