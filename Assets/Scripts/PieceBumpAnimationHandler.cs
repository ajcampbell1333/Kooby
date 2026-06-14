using UnityEngine;

[RequireComponent(typeof(CubeAnimController))]
public class PieceBumpAnimationHandler : MonoBehaviour
{
	PlayerPiece _piece;
	CubeAnimController _animController;
	RippleEffectController _rippleController;

	void Awake()
	{
		_piece = GetComponent<PlayerPiece>();
		_animController = GetComponent<CubeAnimController>();
		_rippleController = GetComponentInChildren<RippleEffectController>();
	}

	void OnEnable() => BumpInteractionEvents.Requested += OnBumpRequested;
	void OnDisable() => BumpInteractionEvents.Requested -= OnBumpRequested;

	void OnBumpRequested(BumpInteraction interaction)
	{
		if (_piece == null || _animController == null || _rippleController == null)
			return;

		bool isBumper = interaction.Bumper == _piece;
		bool isBumpee = interaction.Bumpee == _piece;
		if (!isBumper && !isBumpee)
			return;

		var (axis, direction) = ResolveRippleSettings(interaction.AxisDirection, isBumper, _rippleController.transform);
		_rippleController.Configure(axis, direction);
		_animController.PlayCollisionEffect();
	}

	static (RippleAxis axis, RippleDirection direction) ResolveRippleSettings(
		Vector3Int axisDirection, bool isBumper, Transform meshTransform)
	{
		Vector3 matrixDirection = new Vector3(axisDirection.x, axisDirection.y, axisDirection.z);
		Vector3 localDirection = meshTransform.InverseTransformDirection(matrixDirection);

		float absX = Mathf.Abs(localDirection.x);
		float absY = Mathf.Abs(localDirection.y);
		float absZ = Mathf.Abs(localDirection.z);

		int axisIndex;
		if (absX >= absY && absX >= absZ)
			axisIndex = 0;
		else if (absY >= absZ)
			axisIndex = 1;
		else
			axisIndex = 2;

		float dominantSign = Mathf.Sign(localDirection[axisIndex]);
		if (dominantSign == 0f)
			dominantSign = 1f;

		// Ripples flow away from the impact face: bumper opposes bump vector, bumpee follows it.
		RippleDirection rippleDirection = isBumper
			? (dominantSign > 0f ? RippleDirection.Negative : RippleDirection.Positive)
			: (dominantSign > 0f ? RippleDirection.Positive : RippleDirection.Negative);

		return ((RippleAxis)axisIndex, rippleDirection);
	}
}
