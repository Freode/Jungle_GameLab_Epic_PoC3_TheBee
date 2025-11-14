using UnityEngine;

public interface ICommand
{
    string Id { get; }
    string DisplayName { get; }
    Sprite Icon { get; }
    bool RequiresTarget { get; }

    bool IsAvailable(UnitAgent agent);
    void Execute(UnitAgent agent, CommandTarget target);
}
