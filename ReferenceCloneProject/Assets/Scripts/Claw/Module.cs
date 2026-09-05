using UnityEngine;

namespace Claw
{
    /// <summary>
    /// Clean-room reconstruction of the source Module boundary.
    /// Verified source behavior: Initialize only stores the owning ClawMachine reference.
    /// </summary>
    public abstract class Module : MonoBehaviour
    {
        protected ClawMachine clawMachine;

        public virtual void Initialize(ClawMachine owner)
        {
            clawMachine = owner;
        }
    }
}
