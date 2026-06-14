using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public enum RippleAxis
{
    X = 0,
    Y = 1,
    Z = 2
}

public enum RippleDirection
{
    Positive = 1,
    Negative = -1
}

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RippleEffectController : MonoBehaviour
{
    const int MaxRipples = 8;

    static readonly int RippleHeightId = Shader.PropertyToID("_RippleHeight");
    static readonly int RipplePulseCountId = Shader.PropertyToID("_RipplePulseCount");
    static readonly int RipplePulsesId = Shader.PropertyToID("_RipplePulses");
    static readonly int RippleAxisId = Shader.PropertyToID("_RippleAxis");
    static readonly int RippleDirectionSignId = Shader.PropertyToID("_RippleDirectionSign");
    static readonly int RippleAxisMinId = Shader.PropertyToID("_RippleAxisMin");
    static readonly int RippleAxisMaxId = Shader.PropertyToID("_RippleAxisMax");

    [Tooltip("Travel speed of each ripple front along the chosen local axis (object-space units per second).")]
    [SerializeField] float rippleSpeed = 2f;
    [Tooltip("Peak displacement amplitude of the first pulse in a sequence.")]
    [SerializeField] float rippleHeight = 0.08f;
    [Tooltip("Local object axis the ripple travels along.")]
    [SerializeField] RippleAxis rippleAxis = RippleAxis.Y;
    [Tooltip("Positive: ripple starts at the negative end of the axis and travels toward positive. Negative: opposite.")]
    [SerializeField] RippleDirection rippleDirection = RippleDirection.Positive;
    [Tooltip("Seconds between pulses in a sequence triggered by Space.")]
    [SerializeField] float reverbInterval = 0.5f;
    [Tooltip("Pulses fired per Space press. A new press cancels only pulses that have not started yet (max 8 active).")]
    [SerializeField] int reverbCount = 3;
    [Tooltip("Each subsequent pulse is this fraction of the previous pulse's height (0.5 = half, compounding every pulse).")]
    [FormerlySerializedAs("reverbHeightRatio")]
    [SerializeField, Range(0.01f, 1f)] float reverbFadeMultiplier = 0.5f;
    [FormerlySerializedAs("heightRampPortion")]
    [Tooltip("Fraction of pulse travel (0–0.5) to fade displacement in at the start.")]
    [SerializeField, Range(0f, 0.5f)] float heightRampIntroPortion = 0.1f;
    [Tooltip("Fraction of pulse travel (0–0.5) to fade displacement out at the end.")]
    [SerializeField, Range(0f, 0.5f)] float heightRampOutroPortion = 0.1f;
    [Tooltip("Log ripple trigger, cancellation, and completion messages to the Console.")]
    [SerializeField] bool logRippleEvents = true;

    public RippleAxis Axis => rippleAxis;
    public RippleDirection Direction => rippleDirection;
    public float AxisHalfExtent => _axisExtent * 0.5f;

    readonly List<RipplePulse> _pulses = new();
    readonly Vector4[] _pulseShaderData = new Vector4[MaxRipples];

    MeshRenderer _meshRenderer;
    MaterialPropertyBlock _propertyBlock;
    int _nextPulseId;
    float _axisMin;
    float _axisMax;
    float _axisExtent;

    void Awake()
    {
        CacheComponents();
        RegisterWithAnimController();
    }

    void Start()
    {
        LogReady();
        RunShaderUpdateAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    void OnDisable() => CancelAllPulses();

    void OnValidate()
    {
        reverbCount = Mathf.Clamp(reverbCount, 1, MaxRipples);
        reverbInterval = Mathf.Max(0f, reverbInterval);
        reverbFadeMultiplier = Mathf.Clamp(reverbFadeMultiplier, 0.01f, 1f);
        heightRampIntroPortion = Mathf.Clamp(heightRampIntroPortion, 0f, 0.5f);
        heightRampOutroPortion = Mathf.Clamp(heightRampOutroPortion, 0f, 0.5f);

        if (!isActiveAndEnabled)
            return;

        CacheComponents();
        PushShaderProperties();
    }

    async UniTaskVoid RunShaderUpdateAsync(CancellationToken destroyToken)
    {
        while (!destroyToken.IsCancellationRequested)
        {
            PushShaderProperties();
            await UniTask.Yield(destroyToken);
        }
    }

    void RegisterWithAnimController()
    {
        var animController = GetComponentInParent<CubeAnimController>();
        if (animController == null)
        {
            if (logRippleEvents)
            {
                Debug.LogWarning(
                    $"[RippleEffectController] No CubeAnimController found in parents of '{name}'. " +
                    "Space input and scale animation will not run until one is assigned above this object.",
                    this);
            }

            return;
        }

        animController.RegisterRipple(this);
    }

    public void Configure(RippleAxis axis, RippleDirection direction)
    {
        rippleAxis = axis;
        rippleDirection = direction;
        CacheAxisBounds();
        PushShaderProperties();
    }

    public void BeginRippleSequence()
    {
        var cancelledCount = CancelUnstartedPulses();
        PruneInactivePulses();

        var activeCount = CountActivePulses();
        if (activeCount + reverbCount > MaxRipples)
        {
            if (logRippleEvents)
            {
                Debug.LogWarning(
                    $"[RippleEffectController] Cannot start sequence: {activeCount} pulse(s) already running, " +
                    $"reverbCount {reverbCount} would exceed max {MaxRipples}.",
                    this);
            }

            return;
        }

        if (cancelledCount > 0 && logRippleEvents)
            Debug.Log($"[RippleEffectController] Cancelled {cancelledCount} scheduled pulse(s); in-progress ripples continue.", this);

        var startTime = Time.time;
        var heightScale = 1f;

        for (var i = 0; i < reverbCount; i++)
        {
            var pulseId = _nextPulseId++;
            _pulses.Add(new RipplePulse
            {
                Id = pulseId,
                HeightScale = heightScale,
                StartTime = startTime + i * reverbInterval,
                IsActive = true
            });

            heightScale *= reverbFadeMultiplier;
            RunPulseAsync(pulseId).Forget();
        }

        if (logRippleEvents)
        {
            Debug.Log(
                $"[RippleEffectController] Sequence started. pulses={reverbCount}, interval={reverbInterval:F2}s, " +
                $"axis={rippleAxis}, direction={rippleDirection}, speed={rippleSpeed}, height={rippleHeight}",
                this);
        }
    }

    async UniTaskVoid RunPulseAsync(int pulseId)
    {
        if (_axisExtent <= Mathf.Epsilon)
            return;

        while (Time.time < GetPulse(pulseId).StartTime)
        {
            if (!IsPulseLive(pulseId))
                return;

            await UniTask.Yield();
        }

        while (IsPulseLive(pulseId))
        {
            var index = FindPulseIndex(pulseId);
            if (index < 0)
                return;

            var pulse = _pulses[index];
            pulse.Progress += rippleSpeed * Time.deltaTime / _axisExtent;

            if (pulse.Progress >= 1f)
            {
                pulse.IsActive = false;
                _pulses[index] = pulse;

                if (logRippleEvents)
                    Debug.Log($"[RippleEffectController] Pulse complete (height scale {pulse.HeightScale:F3}).", this);

                return;
            }

            _pulses[index] = pulse;
            await UniTask.Yield();
        }
    }

    int CancelUnstartedPulses()
    {
        var cancelledCount = 0;
        var now = Time.time;

        for (var i = 0; i < _pulses.Count; i++)
        {
            var pulse = _pulses[i];
            if (!pulse.IsActive || now >= pulse.StartTime)
                continue;

            pulse.IsActive = false;
            _pulses[i] = pulse;
            cancelledCount++;
        }

        return cancelledCount;
    }

    void PruneInactivePulses() => _pulses.RemoveAll(p => !p.IsActive);

    void CancelAllPulses()
    {
        for (var i = 0; i < _pulses.Count; i++)
        {
            var pulse = _pulses[i];
            pulse.IsActive = false;
            _pulses[i] = pulse;
        }

        _pulses.Clear();
    }

    int CountActivePulses()
    {
        var count = 0;
        for (var i = 0; i < _pulses.Count; i++)
        {
            if (_pulses[i].IsActive)
                count++;
        }

        return count;
    }

    bool IsPulseLive(int pulseId)
    {
        var index = FindPulseIndex(pulseId);
        return index >= 0 && _pulses[index].IsActive;
    }

    RipplePulse GetPulse(int pulseId)
    {
        var index = FindPulseIndex(pulseId);
        return index >= 0 ? _pulses[index] : default;
    }

    int FindPulseIndex(int pulseId)
    {
        for (var i = 0; i < _pulses.Count; i++)
        {
            if (_pulses[i].Id == pulseId)
                return i;
        }

        return -1;
    }

    void LogReady()
    {
        if (!logRippleEvents)
            return;

        Debug.Log($"[RippleEffectController] Ready on '{name}'. Ripple sequences are driven by CubeAnimController.", this);

        if (_meshRenderer.sharedMaterial == null)
            Debug.LogWarning($"[RippleEffectController] No material on '{name}'.", this);
        else if (_meshRenderer.sharedMaterial.shader.name != "TessellationEffects/TessellationRipple")
            Debug.LogWarning($"[RippleEffectController] Material shader is '{_meshRenderer.sharedMaterial.shader.name}', expected TessellationEffects/TessellationRipple.", this);

        if (_axisExtent <= Mathf.Epsilon)
            Debug.LogWarning($"[RippleEffectController] Axis extent is zero on {rippleAxis}. Check MeshFilter.", this);
    }

    void CacheComponents()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _propertyBlock ??= new MaterialPropertyBlock();
        CacheAxisBounds();
    }

    void CacheAxisBounds()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
            return;

        var bounds = mesh.bounds;
        var axisIndex = (int)rippleAxis;
        _axisMin = bounds.min[axisIndex];
        _axisMax = bounds.max[axisIndex];
        _axisExtent = _axisMax - _axisMin;
    }

    void PushShaderProperties()
    {
        if (_meshRenderer == null)
            return;

        for (var i = 0; i < MaxRipples; i++)
        {
            if (i < _pulses.Count && _pulses[i].IsActive && Time.time >= _pulses[i].StartTime)
            {
                var pulse = _pulses[i];
                var rampedHeightScale = GetRampedHeightScale(pulse);
                _pulseShaderData[i] = new Vector4(pulse.Progress, rampedHeightScale, 1f, 0f);
            }
            else
                _pulseShaderData[i] = Vector4.zero;
        }

        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(RippleHeightId, rippleHeight);
        _propertyBlock.SetFloat(RipplePulseCountId, _pulses.Count);
        _propertyBlock.SetVectorArray(RipplePulsesId, _pulseShaderData);
        _propertyBlock.SetFloat(RippleAxisId, (float)rippleAxis);
        _propertyBlock.SetFloat(RippleDirectionSignId, (float)rippleDirection);
        _propertyBlock.SetFloat(RippleAxisMinId, _axisMin);
        _propertyBlock.SetFloat(RippleAxisMaxId, _axisMax);
        _meshRenderer.SetPropertyBlock(_propertyBlock);
    }

    float GetRampedHeightScale(RipplePulse pulse)
    {
        var rampUp = heightRampIntroPortion <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(pulse.Progress / heightRampIntroPortion);

        var rampDown = heightRampOutroPortion <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01((1f - pulse.Progress) / heightRampOutroPortion);

        return pulse.HeightScale * Mathf.Min(rampUp, rampDown);
    }

    struct RipplePulse
    {
        public int Id;
        public float Progress;
        public float HeightScale;
        public float StartTime;
        public bool IsActive;
    }
}
