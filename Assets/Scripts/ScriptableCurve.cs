using UnityEngine;

[CreateAssetMenu(fileName = "New Curve", menuName = "Koob/Scriptable Curve")]
public class ScriptableCurve : ScriptableObject
{
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public AnimationCurve Curve => curve;
    
    private void OnValidate()
    {
        // Ensure the curve is normalized (0,0 to 1,1)
        if (curve.keys.Length == 0)
        {
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
}

