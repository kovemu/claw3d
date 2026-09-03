using UnityEngine;

namespace Claw3D.Toys
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ToyPhysics : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float mass = 0.7f;

        private void Awake()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.mass = mass;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}
