using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// Generic Godot interaction adapter. Station-specific rules live in registered action handlers.
/// </summary>
public partial class StationInteractable : StaticBody3D, IInteractable
{
    [Export] public StationKind Kind { get; set; }
    [Export] public string EntityId { get; set; } = string.Empty;
    [Export] public StationDefinition? Definition { get; set; }

    public string DisplayName => ResolveDefinition().DisplayName;

    public string GetPrompt(InteractionContext context)
    {
        var actionContext = CreateActionContext(context);
        return ResolveHandler(actionContext).GetPrompt(actionContext);
    }

    public GameplayActionDefinition GetActionDefinition(InteractionContext context)
    {
        var actionContext = CreateActionContext(context);
        return ResolveHandler(actionContext).GetActionDefinition(actionContext);
    }

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted)
            return string.Empty;
        if (GetStorageParent() is { IsOpen: false })
            return "先打开砧板右下方的上层抽屉，才能使用里面的冰桶。";

        var actionContext = CreateActionContext(context);
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
        {
            return actionContext.Definition.HideUnavailablePromptInGlassesWorld
                ? string.Empty
                : $"[G] 摘下眼镜后操作 · {actionContext.Definition.DisplayName}";
        }

        return ResolveHandler(actionContext).GetUnavailablePrompt(actionContext);
    }

    public bool CanInteract(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted ||
            GameSession.Instance.WorldMode == WorldMode.Glasses ||
            GetStorageParent() is { IsOpen: false })
        {
            return false;
        }

        var actionContext = CreateActionContext(context);
        return ResolveHandler(actionContext).CanInteract(actionContext);
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            GameSession.Instance.EmitSignal(
                GameSession.SignalName.StatusMessage,
                GetUnavailablePrompt(context));
            return;
        }

        var actionContext = CreateActionContext(context);
        ResolveHandler(actionContext).Execute(actionContext);
    }

    private StationActionContext CreateActionContext(InteractionContext interaction) =>
        new(this, ResolveDefinition(), interaction);

    private StationDefinition ResolveDefinition()
    {
        Definition ??= StationDefinitionCatalog.GetPrototype(EntityId, Kind);
        if (Definition.Id.ToString() != EntityId || Definition.Kind != Kind)
        {
            throw new InvalidOperationException(
                $"Station node '{EntityId}' does not match definition '{Definition.Id}' ({Definition.Kind}).");
        }

        return Definition;
    }

    private static IStationActionHandler ResolveHandler(StationActionContext context) =>
        StationActionHandlerRegistry.Resolve(context.Definition.HandlerId);

    private CabinetInteractable? GetStorageParent() => GetParent() as CabinetInteractable;
}
