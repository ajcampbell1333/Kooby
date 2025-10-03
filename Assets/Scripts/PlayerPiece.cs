using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class PlayerPiece : MonoBehaviour
{
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private ScriptableCurve moveCurve;
    
    private bool isMoving = false;
    private Vector3 currentMatrixPosition;
    private Vector3 previousMatrixPosition;
    private List<Vector3> possibleMoves = new List<Vector3>();
    private KoobPlayer ownerPlayer;
    
    public UnityAction<PlayerPiece> BeganMoving;
    public UnityAction<PlayerPiece> FinishedMoving;
    
    public bool IsMoving => isMoving;
    public Vector3 GetPreviousMatrixPosition() => previousMatrixPosition;
    
    public void Move(Vector3 worldDestination, Vector3 logicalDestination)
    {
        if (isMoving)
        {
            Debug.LogWarning($"PlayerPiece {gameObject.name} is already moving!");
            return;
        }
        
        // Store previous position and update current matrix position
        previousMatrixPosition = currentMatrixPosition;
        currentMatrixPosition = logicalDestination;
        
        // Invoke the began moving event
        BeganMoving?.Invoke(this);
        
        StartCoroutine(MoveCoroutine(worldDestination));
    }
    
    private IEnumerator MoveCoroutine(Vector3 destination)
    {
        isMoving = true;
        
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        
        // Use the curve from ScriptableCurve, or default to linear if not assigned
        AnimationCurve curve = moveCurve != null ? moveCurve.Curve : AnimationCurve.Linear(0, 0, 1, 1);
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // Evaluate the animation curve
            float curveValue = curve.Evaluate(t);
            
            // Interpolate position using the curve value
            transform.position = Vector3.Lerp(startPosition, destination, curveValue);
            
            yield return null;
        }
        
        // Ensure we end exactly at the destination
        transform.position = destination;
        isMoving = false;
        
        // Invoke the finished moving event
        FinishedMoving?.Invoke(this);
        
        Debug.Log($"PlayerPiece {gameObject.name} finished moving to {destination}");
    }
    
    public void SetMoveCurve(ScriptableCurve curve)
    {
        moveCurve = curve;
    }
    
    public void SetMoveDuration(float duration)
    {
        moveDuration = duration;
    }
    
    public List<Vector3> GetPossibleMoves()
    {
        return new List<Vector3>(possibleMoves); // Return a copy
    }
    
    public void SetPossibleMoves(List<Vector3> moves)
    {
        possibleMoves.Clear();
        possibleMoves.AddRange(moves);
    }
    
    public void ClearPossibleMoves()
    {
        possibleMoves.Clear();
    }
    
    public Vector3 GetCurrentMatrixPosition()
    {
        return currentMatrixPosition;
    }
    
    public void SetMatrixPosition(Vector3 matrixPos)
    {
        currentMatrixPosition = matrixPos;
    }
    
    public void SetOwnerPlayer(KoobPlayer player) => ownerPlayer = player;
    public KoobPlayer GetOwnerPlayer() => ownerPlayer;
}
