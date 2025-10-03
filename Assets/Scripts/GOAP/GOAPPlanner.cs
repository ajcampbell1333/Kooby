using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GOAPPlanner
{
    public List<IGOAPAction> FindPlan(WorldState currentState, IGOAPGoal goal, List<IGOAPAction> availableActions)
    {
        var plan = new List<IGOAPAction>();
        var goalState = goal.GetGoalState();
        
        // Simple greedy approach: find the action that gets us closest to the goal
        var bestAction = FindBestAction(currentState, goal, availableActions);
        
        if (bestAction != null)
        {
            plan.Add(bestAction);
        }
        
        return plan;
    }
    
    private IGOAPAction FindBestAction(WorldState currentState, IGOAPGoal goal, List<IGOAPAction> availableActions)
    {
        IGOAPAction bestAction = null;
        float bestScore = float.MaxValue;
        
        foreach (var action in availableActions)
        {
            if (!action.IsValid(currentState) || !action.PreconditionsMet(currentState))
                continue;
                
            var newState = action.ApplyAction(new WorldState(currentState));
            float distance = goal.GetDistance(newState);
            float totalCost = action.Cost + distance;
            
            if (totalCost < bestScore)
            {
                bestScore = totalCost;
                bestAction = action;
            }
        }
        
        return bestAction;
    }
}
