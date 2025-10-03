using UnityEngine;

public class BringPiecesTogetherGoal : IGOAPGoal
{
    public string Name => "Bring Pieces Together";
    public float Priority => 1.0f;
    
    private KoobPlayer player;
    
    public BringPiecesTogetherGoal(KoobPlayer player)
    {
        this.player = player;
    }
    
    public bool IsValid(WorldState worldState)
    {
        return player != null && player.GetPieces().Count == 2;
    }
    
    public WorldState GetGoalState()
    {
        var goalState = new WorldState();
        
        // The goal is to have pieces adjacent (distance = 1)
        goalState.Set("pieces_adjacent", true);
        
        return goalState;
    }
    
    public float GetDistance(WorldState worldState)
    {
        if (player == null || player.GetPieces().Count != 2)
            return float.MaxValue;
        
        var pieces = player.GetPieces();
        
        // Prefer simulated positions from worldState if available; fall back to live positions
        Vector3 GetPos(PlayerPiece piece)
        {
            string key = $"piece_{piece.gameObject.name}_position";
            return (worldState != null && worldState.Has(key)) ? worldState.Get<Vector3>(key) : piece.GetCurrentMatrixPosition();
        }
        
        Vector3 pos1 = GetPos(pieces[0]);
        Vector3 pos2 = GetPos(pieces[1]);
        
        // Calculate Manhattan distance
        float distance = Mathf.Abs(pos1.x - pos2.x) +
                        Mathf.Abs(pos1.y - pos2.y) +
                        Mathf.Abs(pos1.z - pos2.z);
        
        // If pieces are already adjacent, distance is 0
        if (distance <= 1.0f)
            return 0.0f;
        
        // Return the distance as the "cost" to reach the goal
        return distance;
    }
}
