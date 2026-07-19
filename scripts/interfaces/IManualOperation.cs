namespace GlassesBar;

public interface IManualOperation
{
    bool IsRunning { get; }
    string OperationPrompt { get; }
    bool Begin(InteractionContext context);
    void UpdateOperation(double intensity, double deltaSeconds);
    OperationResult Complete();
    void Cancel();
}

