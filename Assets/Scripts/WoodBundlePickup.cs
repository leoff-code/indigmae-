using UnityEngine;

namespace CrystalSprint
{
    public sealed class WoodBundlePickup : MonoBehaviour, IInteractable
    {
        public bool CanInteract => FindAnyObjectByType<LumberjackEquipment>() != null;
        public void Interact(PlayerInteractor user)
        {
            var inventory = user != null ? user.GetComponent<LumberjackEquipment>() : FindAnyObjectByType<LumberjackEquipment>();
            if (inventory != null && inventory.TryAddWoodBundle()) Destroy(gameObject);
        }
    }
}
