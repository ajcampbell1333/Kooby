using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomContinuousMoves : MonoBehaviour
{
    private GameManager gameManager;
    private List<PlayerPiece> allPlayerPieces = new List<PlayerPiece>();
    private Coroutine currentMoveCoroutine;
    
    public void Initialize(GameManager gm, List<PlayerPiece> pieces)
    {
        gameManager = gm;
        allPlayerPieces = pieces;
        GameStateMachine.NewTurnBegan += OnNewTurnBegan;
    }
    
    private void Start() => OnNewTurnBegan(gameManager.GetCurrentPlayer());

    private void OnDestroy() => GameStateMachine.NewTurnBegan -= OnNewTurnBegan;
    
    
    private void OnNewTurnBegan(KoobPlayer player)
    {
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
        currentMoveCoroutine = StartCoroutine(MakeRandomMove(player));
    }
    
    private IEnumerator MakeRandomMove(KoobPlayer player)
    {
        yield return new WaitForSeconds(5f);
        List<PlayerPiece> playerPieces = player.GetPieces();
        if (playerPieces.Count == 0) yield break;
        PlayerPiece randomPiece = playerPieces[Random.Range(0, playerPieces.Count)];
        List<Vector3> possibleMoves = randomPiece.GetPossibleMoves();
        if (possibleMoves.Count == 0) yield break;
        Vector3 randomLogicalMove = possibleMoves[Random.Range(0, possibleMoves.Count)];
        Vector3 randomWorldMove = gameManager.koobNodeSet.GetPos(randomLogicalMove);
        randomPiece.Move(randomWorldMove, randomLogicalMove);
    }
}
