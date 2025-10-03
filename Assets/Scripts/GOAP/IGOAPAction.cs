using UnityEngine;

public interface IGOAPAction
{
    string Name { get; }
    float Cost { get; }
    bool IsValid(WorldState worldState);
    WorldState ApplyAction(WorldState worldState);
    bool PreconditionsMet(WorldState worldState);
}
