using GlassesBar.Domain;

namespace GlassesBar;

public interface IInteractable
{
    GameplayActionDefinition GetActionDefinition(InteractionContext context);
    string GetPrompt(InteractionContext context);
    string GetUnavailablePrompt(InteractionContext context);
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}
