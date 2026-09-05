using UnityEngine;

namespace Claw3D.Claw
{
    public sealed class ClawCableVisual : MonoBehaviour
    {
        [SerializeField] private Transform trolley;
        [SerializeField] private Transform hub;
        [SerializeField] private Transform cableMesh;
        [SerializeField] private ClawRopeConstraint rope;
        [SerializeField] private float radius = 0.006f;

        private LineRenderer line;

        public void Configure(Transform trolleyTransform, Transform hubTransform, Transform mesh, float cableRadius = 0.006f)
        {
            trolley = trolleyTransform;
            hub = hubTransform;
            cableMesh = mesh;
            radius = cableRadius;
            Refresh();
        }

        public void SetRope(ClawRopeConstraint ropeConstraint)
        {
            rope = ropeConstraint;
            EnsureLineRenderer();
            Refresh();
        }

        public void SetRadius(float cableRadius)
        {
            radius = Mathf.Max(0.001f, cableRadius);
            if (line != null)
            {
                line.startWidth = radius * 2f;
                line.endWidth = radius * 2f;
            }
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (rope != null && rope.HasSimulationPoints)
            {
                EnsureLineRenderer();
                if (line != null)
                {
                    line.positionCount = rope.ParticleCount;
                    for (int i = 0; i < rope.ParticleCount; i++)
                        line.SetPosition(i, rope.GetParticlePosition(i));
                    line.startWidth = radius * 2f;
                    line.endWidth = radius * 2f;
                }

                if (cableMesh != null)
                {
                    Renderer meshRenderer = cableMesh.GetComponent<Renderer>();
                    if (meshRenderer != null) meshRenderer.enabled = false;
                }
                return;
            }

            if (line != null) line.positionCount = 0;
            if (trolley == null || hub == null || cableMesh == null) return;

            Renderer renderer = cableMesh.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            Vector3 a = trolley.position;
            Vector3 b = hub.position;
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.0001f) return;

            cableMesh.position = (a + b) * 0.5f;
            cableMesh.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            cableMesh.localScale = new Vector3(radius, length * 0.5f, radius);
        }

        private void EnsureLineRenderer()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.loop = false;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startWidth = radius * 2f;
            line.endWidth = radius * 2f;

            if (line.sharedMaterial == null && cableMesh != null)
            {
                Renderer source = cableMesh.GetComponent<Renderer>();
                if (source != null) line.sharedMaterial = source.sharedMaterial;
            }
        }
    }
}
