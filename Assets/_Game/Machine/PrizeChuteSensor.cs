using Claw3D.Toys;
using UnityEngine;

namespace Claw3D.Machine
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PrizeChuteSensor : MonoBehaviour
    {
        [SerializeField] private MachineController machine;

        public void Configure(MachineController controller)
        {
            machine = controller;
        }

        private void OnTriggerEnter(Collider other)
        {
            ToyPhysics toy = other.GetComponentInParent<ToyPhysics>();
            if (toy == null || machine == null) return;
            machine.ReportPrize(toy);
        }
    }
}
