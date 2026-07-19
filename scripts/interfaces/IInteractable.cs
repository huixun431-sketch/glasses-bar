namespace GlassesBar;

public interface IInteractable
{
    string GetPrompt(InteractionContext context);
    string GetUnavailablePrompt(InteractionContext context);
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}
