using UnityEngine;

[CreateAssetMenu(menuName = "Commands/SOCommand", fileName = "SOCommand")]
public class SOCommand : ScriptableObject, ICommand
{
    public string id;
    public string displayName;
    public Sprite icon;
    public bool requiresTarget = true;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool RequiresTarget => requiresTarget;

    // Default availability checks ? can be overridden by subclassing or by external validator
    public virtual bool IsAvailable(UnitAgent agent)
    {
        return agent != null;
    }

    // Execute is abstract-ish: by default we just log; real behavior should be implemented in CommandHandlers
    public virtual void Execute(UnitAgent agent, CommandTarget target)
    {
        Debug.Log($"Executing SOCommand {displayName} for agent {agent?.name} target {target.type}");
    }
}
