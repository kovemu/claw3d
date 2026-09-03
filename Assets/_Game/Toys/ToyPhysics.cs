using UnityEngine;

namespace Claw3D.Toys
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ToyPhysics : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float linearDamping = 0.9f;
        [SerializeField, Min(0f)] private float angularDamping = 1.6f;
        [SerializeField, Min(1)] private int solverIterations = 12;
        [SerializeField, Min(1)] private int solverVelocityIterations = 8;

        public void Configure(float linear, float angular, int iterations, int velocityIterations)
        {
            linearDamping = linear;
            angularDamping = angular;
            solverIterations = iterations;
            solverVelocityIterations = velocityIterations;
            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void Apply()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 12f;
            body.solverIterations = solverIterations;
            body.solverVelocityIterations = solverVelocityIterations;
        }
    }
}
