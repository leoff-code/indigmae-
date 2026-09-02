namespace CrystalSprint
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        void Interact(PlayerInteractor user);
    }
}
