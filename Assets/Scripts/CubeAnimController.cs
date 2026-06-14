using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CubeAnimController : MonoBehaviour
{
    [Tooltip("How much to compress the collision axis at peak impact (0 = none, 1 = flat).")]
    [SerializeField, Range(0f, 0.9f)] float squashAmount = 0.35f;
    [Tooltip("How much perpendicular axes bulge outward during the squash.")]
    [SerializeField, Range(0f, 1f)] float bulgeAmount = 0.2f;
    [Tooltip("Seconds to reach peak squash after Space is pressed.")]
    [SerializeField] float impactDuration = 0.08f;
    [Tooltip("Seconds to spring back to rest scale after peak squash.")]
    [SerializeField] float recoverDuration = 0.45f;
    [Tooltip("Extra bounce past rest scale during recovery (cartoon overshoot).")]
    [SerializeField, Range(0f, 0.5f)] float recoverOvershoot = 0.08f;
    [Tooltip("Log Space triggers and scale animation events to the Console.")]
    [SerializeField] bool logAnimEvents = true;
    [Tooltip("When enabled, pressing Space triggers collision ripple + squash for look-dev.")]
    [SerializeField] bool enableSpaceDebugInput = false;

    public float CollisionAnimationDuration => impactDuration + recoverDuration;

    RippleEffectController _ripple;
    Transform _scaleRig;
    Vector3 _restScale = Vector3.one;
    Vector3 _restPosition = Vector3.zero;
    CancellationTokenSource _scaleAnimCts;

    public void RegisterRipple(RippleEffectController ripple)
    {
        _ripple = ripple;

        if (logAnimEvents)
            Debug.Log($"[CubeAnimController] Registered ripple controller on '{ripple.name}'.", this);
    }

    public void CaptureRestState()
    {
        if (_scaleRig == null)
            _scaleRig = transform.childCount > 0 ? transform.GetChild(0) : transform;

        _restScale = _scaleRig.localScale;
        _restPosition = _scaleRig.localPosition;
    }

    void Awake()
    {
        _scaleRig = transform.childCount > 0 ? transform.GetChild(0) : transform;
        CaptureRestState();
    }

    void Start()
    {
        if (_ripple == null && logAnimEvents)
            Debug.LogWarning($"[CubeAnimController] No RippleEffectController registered on '{name}'.", this);
        else if (logAnimEvents && enableSpaceDebugInput)
            Debug.Log($"[CubeAnimController] Ready on '{name}'. Press Space to trigger collision ripple + squash.", this);

        if (enableSpaceDebugInput)
            RunDriverAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    void OnDisable()
    {
        _scaleAnimCts?.Cancel();
        _scaleAnimCts?.Dispose();
        _scaleAnimCts = null;
        if (_scaleRig != null)
        {
            _scaleRig.localScale = _restScale;
            _scaleRig.localPosition = _restPosition;
        }
    }

    async UniTaskVoid RunDriverAsync(CancellationToken destroyToken)
    {
        while (!destroyToken.IsCancellationRequested)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                TriggerCollisionEffect();

            await UniTask.Yield(destroyToken);
        }
    }

    public void PlayCollisionEffect() => TriggerCollisionEffect();

    void TriggerCollisionEffect()
    {
        if (_ripple == null)
        {
            if (logAnimEvents)
                Debug.LogWarning("[CubeAnimController] Space ignored — no ripple controller registered.", this);

            return;
        }

        CaptureRestState();

        if (logAnimEvents)
            Debug.Log("[CubeAnimController] Triggering collision ripple + squash.", this);

        _ripple.BeginRippleSequence();
        PlaySquashAnimationAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    async UniTaskVoid PlaySquashAnimationAsync(CancellationToken destroyToken)
    {
        _scaleAnimCts?.Cancel();
        _scaleAnimCts?.Dispose();
        _scaleAnimCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
        var animToken = _scaleAnimCts.Token;

        var squashScale = BuildSquashScale(_ripple.Axis);
        var overshootScale = BuildOvershootScale(_restScale, recoverOvershoot);

        if (!await AnimateScaleAsync(_restScale, squashScale, impactDuration, animToken))
            return;

        if (!await AnimateScaleAsync(squashScale, overshootScale, recoverDuration * 0.55f, animToken))
            return;

        if (!await AnimateScaleAsync(overshootScale, _restScale, recoverDuration * 0.45f, animToken))
            return;

        ApplyTransform(_restScale);
    }

    Vector3 BuildSquashScale(RippleAxis axis)
    {
        var scale = _restScale;
        var axisIndex = (int)axis;
        scale[axisIndex] = _restScale[axisIndex] * (1f - squashAmount);

        for (var i = 0; i < 3; i++)
        {
            if (i == axisIndex)
                continue;

            scale[i] = _restScale[i] * (1f + bulgeAmount);
        }

        return scale;
    }

    static Vector3 BuildOvershootScale(Vector3 restScale, float overshoot)
    {
        var overshootScale = restScale;
        for (var i = 0; i < 3; i++)
            overshootScale[i] = restScale[i] * (1f + overshoot);

        return overshootScale;
    }

    async UniTask<bool> AnimateScaleAsync(Vector3 fromScale, Vector3 toScale, float duration, CancellationToken token)
    {
        if (duration <= Mathf.Epsilon)
        {
            ApplyTransform(toScale);
            return !token.IsCancellationRequested;
        }

        var fromPosition = PositionForScale(fromScale);
        var toPosition = PositionForScale(toScale);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            if (token.IsCancellationRequested)
                return false;

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = EaseOutQuad(t);
            _scaleRig.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            _scaleRig.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            await UniTask.Yield(token);
        }

        ApplyTransform(toScale);
        return true;
    }

    void ApplyTransform(Vector3 scale)
    {
        _scaleRig.localScale = scale;
        _scaleRig.localPosition = PositionForScale(scale);
    }

    Vector3 PositionForScale(Vector3 scale)
    {
        var pos = _restPosition;
        var axisIndex = (int)_ripple.Axis;
        var scaleFactor = scale[axisIndex] / _restScale[axisIndex];
        var compression = 1f - scaleFactor;
        var halfExtent = _ripple.AxisHalfExtent;

        // Pin the travel-target face so compression accumulates on the ripple source side.
        if (_ripple.Direction == RippleDirection.Positive)
            pos[axisIndex] = _restPosition[axisIndex] - compression * halfExtent;
        else
            pos[axisIndex] = _restPosition[axisIndex] + compression * halfExtent;

        return pos;
    }

    static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
