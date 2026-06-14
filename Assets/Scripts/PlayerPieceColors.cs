using UnityEngine;

public static class PlayerPieceColors
{
	static readonly Color[] Colors =
	{
		new Color(1f, 0f, 0f, 1f),              // Player 1 — from Player1Piece.mat
		new Color(0f, 1f, 0.09687209f, 1f),     // Player 2 — from Player2Piece.mat
		new Color(0f, 0.11869669f, 1f, 1f),     // Player 3 — from Player3Piece.mat
		new Color(1f, 0.66827226f, 0f, 1f),     // Player 4 — from Player4Piece.mat
	};

	public static Color Get(int playerIndex) => Colors[playerIndex];

	public static Color GetByPlayerId(int playerId) => Colors[playerId - 1];
}
