using UnityEngine;

public interface IGOAPGoal
{
    string Name { get; }
    float Priority { get; }
    bool IsValid(WorldState worldState);
    WorldState GetGoalState();
    float GetDistance(WorldState worldState);
}
