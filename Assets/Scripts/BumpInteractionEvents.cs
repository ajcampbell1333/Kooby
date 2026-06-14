using UnityEngine;

public struct BumpInteraction
{
	public PlayerPiece Bumper;
	public PlayerPiece Bumpee;
	public Vector3Int AxisDirection;
}

public static class BumpInteractionEvents
{
	public static event System.Action<BumpInteraction> Requested;

	public static void Raise(BumpInteraction interaction) => Requested?.Invoke(interaction);
}
