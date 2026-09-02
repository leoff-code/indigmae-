using UnityEngine;

namespace CrystalSprint
{
    // Landmarks are in the supplied cabin's local coordinates, independent of placement/scale.
    public sealed class PondCabin : MonoBehaviour
    {
        public Vector3 Entrance => transform.TransformPoint(new Vector3(.966f, .254f, 3.85f));
        public Vector3 Interior => transform.TransformPoint(new Vector3(.966f, .254f, 1.8f));
        public Vector3 Porch => transform.TransformPoint(new Vector3(.966f, .254f, 5.25f));
        public Vector3 Approach => transform.TransformPoint(new Vector3(.966f, .15f, 8.2f));
        public Vector3 ExitDirection => transform.forward;
        public float DoorWidth => 1.10f * transform.lossyScale.x;
        public float DoorHeight => 2.10f * transform.lossyScale.y;
    }
}
