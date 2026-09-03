using UnityEngine;

namespace Claw3D.Toys
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ToyPhysics : MonoBehaviour
    {
        private void Awake()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 12f;
        }
    }
}
