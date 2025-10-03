using UnityEngine;
using System.Collections.Generic;

public class KoobPlayer : MonoBehaviour
{
    public int id;
    private List<PlayerPiece> pieces = new List<PlayerPiece>();
    
    public void AssignPiece(PlayerPiece piece)
    {
        if (pieces.Count >= 2)
        {
            Debug.LogError($"KoobPlayer {id}: Cannot assign more than 2 pieces. Current count: {pieces.Count}");
            return;
        }
        
        pieces.Add(piece);
        Debug.Log($"KoobPlayer {id}: Assigned piece {piece.gameObject.name}. Total pieces: {pieces.Count}");
    }
    
    public List<PlayerPiece> GetPieces() => new List<PlayerPiece>(pieces);
    
    // Placeholder class for future NGO multiplayer integration
    // No additional properties or methods needed yet
}
