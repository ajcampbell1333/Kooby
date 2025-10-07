using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public enum PlayerPieceFace
{
	Top,
	Bottom,
	North,
	South,
	East,
	West
}

public class BumpAnimation : MonoBehaviour
{
	[SerializeField] private ScriptableCurve bumpCurve;
	[SerializeField] private float bumpDuration = 1f;

	public bool IsAnimating { get; private set; }
	private Coroutine runningCoroutine;
	private Transform savedParent;
	private Vector3 savedLocalPosition;
	private Quaternion savedLocalRotation;
	private Vector3 savedLocalScale;
	private GameObject pivotGO;
	private System.Action onComplete;
	private TaskCompletionSource<bool> bumpTcs;

	public void SetCurve(ScriptableCurve curve) => bumpCurve = curve;

	public Coroutine Bump(PlayerPieceFace face, System.Action onComplete = null)
	{
		if (IsAnimating) Stop();
		runningCoroutine = StartCoroutine(BumpRoutine(face, onComplete, null));
		return runningCoroutine;
	}

	public IEnumerator BumpEnumerator(PlayerPieceFace face, System.Action onComplete = null) => BumpRoutine(face, onComplete, null);

	public Task BumpAsync(PlayerPieceFace face)
	{
		if (IsAnimating) Stop();
		bumpTcs = new TaskCompletionSource<bool>();
		runningCoroutine = StartCoroutine(BumpRoutine(face, null, bumpTcs));
		return bumpTcs.Task;
	}

	public void Stop()
	{
		if (runningCoroutine != null)
		{
			StopCoroutine(runningCoroutine);
			runningCoroutine = null;
		}
		// Cancel async waiters if present
		if (bumpTcs != null && !bumpTcs.Task.IsCompleted) bumpTcs.TrySetCanceled();
		bumpTcs = null;
		onComplete = null;
		RestoreAndCleanup();
	}

	private IEnumerator BumpRoutine(PlayerPieceFace face, System.Action completionCallback, TaskCompletionSource<bool> tcs)
	{
		if (bumpCurve == null || bumpDuration <= 0f) yield break;

		IsAnimating = true;

		var piece = transform;
		savedParent = piece.parent;
		savedLocalPosition = piece.localPosition;
		savedLocalRotation = piece.localRotation;
		savedLocalScale = piece.localScale;
		onComplete = completionCallback;
		bumpTcs = tcs;

		// Create pivot
		pivotGO = new GameObject("BumpPivot");
		var pivot = pivotGO.transform;
		pivot.SetParent(savedParent, worldPositionStays: false);
		pivot.localPosition = transform.localPosition + GetFaceOffsetLocal(face, transform.localScale);
		pivot.localRotation = Quaternion.identity;
		pivot.localScale = Vector3.one;

		// Reparent piece under pivot without changing world transform
		piece.SetParent(pivot, worldPositionStays: true);

		// Determine scale axis
		int axis = GetAxisForFace(face);

		float t = 0f;
		while (t < bumpDuration)
		{
			float norm = Mathf.Clamp01(t / bumpDuration);
			float scaleAlongAxis = bumpCurve.Curve.Evaluate(norm);
			var current = Vector3.one;
			current[axis] = scaleAlongAxis;
			pivot.localScale = current;
			t += Time.deltaTime;
			yield return null;
		}
		pivot.localScale = Vector3.one;

		RestoreAndCleanup();
		runningCoroutine = null;
		IsAnimating = false;
		// Invoke completion pathways
		var cb = onComplete; onComplete = null;
		var tcsLocal = bumpTcs; bumpTcs = null;
		if (cb != null) cb();
		if (tcsLocal != null && !tcsLocal.Task.IsCompleted) tcsLocal.TrySetResult(true);
	}

	private void RestoreAndCleanup()
	{
		var piece = transform;
		// Always restore parent, even if null (root). This prevents destroying the piece
		// when we destroy the temporary pivot, since children are destroyed with parents.
		piece.SetParent(savedParent, worldPositionStays: false);
		piece.localPosition = savedLocalPosition;
		piece.localRotation = savedLocalRotation;
		piece.localScale = savedLocalScale;
		if (pivotGO != null) Object.Destroy(pivotGO);
		pivotGO = null;
	}

	private static int GetAxisForFace(PlayerPieceFace face)
	{
		switch (face)
		{
			case PlayerPieceFace.Top:
			case PlayerPieceFace.Bottom: return 1; // Y
			case PlayerPieceFace.North:
			case PlayerPieceFace.South: return 2; // Z
			case PlayerPieceFace.East:
			case PlayerPieceFace.West: return 0; // X
			default: return 1;
		}
	}

	private static Vector3 GetFaceOffsetLocal(PlayerPieceFace face, Vector3 localScale)
	{
		// Offset from the piece's local center to the center of the specified face,
		// accounting for the piece's current local scale (half-extents).
		float hx = 0.5f * localScale.x;
		float hy = 0.5f * localScale.y;
		float hz = 0.5f * localScale.z;
		switch (face)
		{
			case PlayerPieceFace.Top: return new Vector3(0f, hy, 0f);
			case PlayerPieceFace.Bottom: return new Vector3(0f, -hy, 0f);
			case PlayerPieceFace.North: return new Vector3(0f, 0f, hz);
			case PlayerPieceFace.South: return new Vector3(0f, 0f, -hz);
			case PlayerPieceFace.East: return new Vector3(hx, 0f, 0f);
			case PlayerPieceFace.West: return new Vector3(-hx, 0f, 0f);
			default: return Vector3.zero;
		}
	}
}


