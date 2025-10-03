using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCPlayer : MonoBehaviour
{
    public KoobPlayer player;
    private GOAPPlanner planner;
    private BringPiecesTogetherGoal goal;
    private List<IGOAPAction> availableActions;
    private WorldState currentWorldState;
    
    public void Initialize(KoobPlayer player)
    {
        this.player = player;
        this.planner = new GOAPPlanner();
        this.goal = new BringPiecesTogetherGoal(player);
        this.availableActions = new List<IGOAPAction>();
        this.currentWorldState = new WorldState();
    }
    
    public PlayerPiece ChooseBestMove()
    {
        if (player == null || player.GetPieces().Count != 2)
            return null;
            
        UpdateWorldState();
        GenerateAvailableActions();
        
        var plan = planner.FindPlan(currentWorldState, goal, availableActions);
        
        if (plan.Count > 0 && plan[0] is MovePieceAction moveAction)
        {
            // Find the actual piece that corresponds to this action
            var pieces = player.GetPieces();
            foreach (var piece in pieces)
            {
                if (moveAction.Name.Contains(piece.gameObject.name))
                {
                    return piece;
                }
            }
        }
        
        // Fallback: return a random piece with valid moves
        var piecesWithMoves = player.GetPieces().Where(p => p.GetPossibleMoves().Count > 0).ToList();
        return piecesWithMoves.Count > 0 ? piecesWithMoves[Random.Range(0, piecesWithMoves.Count)] : null;
    }
    
    public Vector3 ChooseBestMoveForPiece(PlayerPiece piece)
    {
        if (piece == null)
            return Vector3.zero;
            
        UpdateWorldState();
        
        var possibleMoves = piece.GetPossibleMoves();
        if (possibleMoves.Count == 0)
            return Vector3.zero;
            
        Vector3 bestMove = possibleMoves[0];
        float bestScore = float.MaxValue;
        
        foreach (var move in possibleMoves)
        {
            var tempAction = new MovePieceAction(piece, move);
            if (!tempAction.IsValid(currentWorldState) || !tempAction.PreconditionsMet(currentWorldState))
                continue;
                
            var newState = tempAction.ApplyAction(new WorldState(currentWorldState));
            float distance = goal.GetDistance(newState);
            
            if (distance < bestScore)
            {
                bestScore = distance;
                bestMove = move;
            }
        }
        
        return bestMove;
    }
    
    private void UpdateWorldState()
    {
        currentWorldState = new WorldState();
        
        // Update piece positions
        var pieces = player.GetPieces();
        foreach (var piece in pieces)
        {
            Vector3 pos = piece.GetCurrentMatrixPosition();
            string pieceKey = $"piece_{piece.gameObject.name}_position";
            currentWorldState.Set(pieceKey, pos);
            
            // Update board state
            string boardKey = $"board_{pos.x}_{pos.y}_{pos.z}";
            currentWorldState.Set(boardKey, player.id);
        }
    }
    
    private void GenerateAvailableActions()
    {
        availableActions.Clear();
        
        var pieces = player.GetPieces();
        foreach (var piece in pieces)
        {
            var possibleMoves = piece.GetPossibleMoves();
            foreach (var move in possibleMoves)
            {
                availableActions.Add(new MovePieceAction(piece, move));
            }
        }
    }
}
