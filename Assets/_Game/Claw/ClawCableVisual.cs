using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawCableVisual : MonoBehaviour
    {
        [SerializeField] private Transform trolley;
        [SerializeField] private Transform hub;
        [SerializeField] private Transform cableMesh;
        [SerializeField] private float radius = 0.006f;

        public void Configure(Transform trolleyTransform, Transform hubTransform, Transform mesh, float cableRadius = 0.006f)
        {
            trolley = trolleyTransform;
            hub = hubTransform;
            cableMesh = mesh;
            radius = cableRadius;
            Refresh();
        }

        public void SetRadius(float cableRadius)
        {
            radius = Mathf.Max(0.001f, cableRadius);
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (trolley == null || hub == null || cableMesh == null) return;
            Vector3 a = trolley.position;
            Vector3 b = hub.position;
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.0001f) return;

            cableMesh.position = (a + b) * 0.5f;
            cableMesh.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            cableMesh.localScale = new Vector3(radius, length * 0.5f, radius);
        }
    }
}
