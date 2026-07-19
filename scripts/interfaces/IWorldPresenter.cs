namespace GlassesBar;

public interface IWorldPresenter
{
    void SetWorldMode(WorldMode mode);
    bool HasEntity(string entityId);
}

