using UnityEngine;

public enum UnitRole
{ 
    None, 
    Worker, 
    Scout, 
    Guard,
    Queen,
}

public class RoleAssigner : MonoBehaviour
{
    public UnitAgent agent;
    public UnitRole currentRole = UnitRole.None;

    void Start()
    {
        if (agent == null) agent = GetComponent<UnitAgent>();
    }

    public void AssignRole(UnitRole role)
    {
        currentRole = role;
        // placeholder: you can add behavior changes here based on role
        switch (role)
        {
            case UnitRole.Worker:
                agent.visionRange = 0;
                FogOfWarManager.Instance?.RecalculateVisibility();
                // e.g., set gather behavior
                break;
            case UnitRole.Scout:
                // increase vision
                agent.visionRange = 1;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.Guard:
                // defensive behavior
                break;
            case UnitRole.Queen:
                agent.visionRange = 2;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
            case UnitRole.None:
            default:
                agent.visionRange = 1;
                FogOfWarManager.Instance?.RecalculateVisibility();
                break;
        }
    }
}
