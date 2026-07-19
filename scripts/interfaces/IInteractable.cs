namespace GlassesBar;

public interface IInteractable
{
    string GetPrompt(InteractionContext context);
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}

