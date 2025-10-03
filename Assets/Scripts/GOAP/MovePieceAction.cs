using UnityEngine;

public class MovePieceAction : IGOAPAction
{
    public string Name => $"Move {pieceName} to {targetPosition}";
    public float Cost => 1.0f;
    
    private PlayerPiece piece;
    private Vector3 targetPosition;
    private string pieceName;
    
    public MovePieceAction(PlayerPiece piece, Vector3 targetPosition)
    {
        this.piece = piece;
        this.targetPosition = targetPosition;
        this.pieceName = piece.gameObject.name;
    }
    
    public bool IsValid(WorldState worldState)
    {
        if (piece == null) return false;
        
        // Check if the target position is in the piece's possible moves
        var possibleMoves = piece.GetPossibleMoves();
        return possibleMoves.Contains(targetPosition);
    }
    
    public WorldState ApplyAction(WorldState worldState)
    {
        var newState = new WorldState(worldState);
        
        // Update the piece's position in the world state
        string pieceKey = $"piece_{pieceName}_position";
        newState.Set(pieceKey, targetPosition);
        
        // Update the board state
        string boardKey = $"board_{targetPosition.x}_{targetPosition.y}_{targetPosition.z}";
        newState.Set(boardKey, piece.GetOwnerPlayer().id);
        
        // Clear the old position
        Vector3 oldPos = piece.GetCurrentMatrixPosition();
        string oldBoardKey = $"board_{oldPos.x}_{oldPos.y}_{oldPos.z}";
        newState.Set(oldBoardKey, 0);
        
        return newState;
    }
    
    public bool PreconditionsMet(WorldState worldState)
    {
        // Check if the target position is unoccupied
        string boardKey = $"board_{targetPosition.x}_{targetPosition.y}_{targetPosition.z}";
        int occupant = worldState.Get<int>(boardKey);
        return occupant == 0;
    }
}
