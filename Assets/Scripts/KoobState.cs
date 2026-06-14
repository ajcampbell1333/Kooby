using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct KoobNode
{
	public bool occupied;
	public int playerID;
	public bool koobCount;
}

public class KoobState
{
	public List<List<List<KoobNode>>> matrix = new List<List<List<KoobNode>>>(3);

	public KoobState()
	{
		EnsureInitialized();
	}

	private void EnsureInitialized()
	{
		if (matrix == null) matrix = new List<List<List<KoobNode>>>(3);
		while (matrix.Count < 3) matrix.Add(new List<List<KoobNode>>(3));
		for (int x = 0; x < 3; x++)
		{
			var yzPlane = matrix[x];
			if (yzPlane == null)
			{
				yzPlane = new List<List<KoobNode>>(3);
				matrix[x] = yzPlane;
			}
			while (yzPlane.Count < 3) yzPlane.Add(new List<KoobNode>(3));
			for (int y = 0; y < 3; y++)
			{
				var zLine = yzPlane[y];
				if (zLine == null)
				{
					zLine = new List<KoobNode>(3);
					yzPlane[y] = zLine;
				}
				while (zLine.Count < 3) zLine.Add(new KoobNode { occupied = false, playerID = 0, koobCount = false });
			}
		}
	}

	public KoobNode GetNode(int x, int y, int z)
	{
		if (!IsInBounds(x, y, z))
		{
			KoobyLogManager.LogError(LogCategory.Matrix, $"GetNode out of bounds ({x},{y},{z})");
			return default;
		}
		return matrix[x][y][z];
	}

	public void SetNode(int x, int y, int z, KoobNode node)
	{
		if (!IsInBounds(x, y, z))
		{
			KoobyLogManager.LogError(LogCategory.Matrix, $"SetNode out of bounds ({x},{y},{z})");
			return;
		}
		matrix[x][y][z] = node;
	}

	public void SetNode(int x, int y, int z, bool occupied, int playerID = 0, bool koobCount = false)
	{
		SetNode(x, y, z, new KoobNode { occupied = occupied, playerID = playerID, koobCount = koobCount });
	}

	public void ResetBoard()
	{
		for (int x = 0; x < 3; x++)
			for (int y = 0; y < 3; y++)
				for (int z = 0; z < 3; z++)
					matrix[x][y][z] = new KoobNode { occupied = false, playerID = 0, koobCount = false };

		// Player 1: (0,0,0) and (2,2,2)
		SetNode(0, 0, 0, true, 1);
		SetNode(2, 2, 2, true, 1);

		// Player 2: (2,0,0) and (0,2,2)
		SetNode(2, 0, 0, true, 2);
		SetNode(0, 2, 2, true, 2);

		// Player 3: (0,2,0) and (2,0,2) - FIXED: now opposite corners
		SetNode(0, 2, 0, true, 3);
		SetNode(2, 0, 2, true, 3);

		// Player 4: (0,0,2) and (2,2,0)
		SetNode(0, 0, 2, true, 4);
		SetNode(2, 2, 0, true, 4);
	}

	public void PrintKoobState()
	{
		KoobyLogManager.Log(LogCategory.Matrix, "=== KoobState Debug ===");
		
		// Print each Y layer (Z=0, Z=1, Z=2)
		for (int z = 0; z < 3; z++)
		{
				KoobyLogManager.Log(LogCategory.Matrix, $"Z = {z}:");
			
			// Print each row in this Y layer
			for (int y = 2; y >= 0; y--) // Print from top to bottom (Y=2 to Y=0)
			{
				string row = "";
				for (int x = 0; x < 3; x++)
				{
					var node = GetNode(x, y, z);
					if (node.occupied)
					{
						row += $"[{node.playerID},{((node.koobCount) ? 1 : 0)}]";
					}
					else
					{
						row += "[-,-]";
					}
					
					// Add spacing between columns
					if (x < 2) row += " ";
				}
					KoobyLogManager.Log(LogCategory.Matrix, row);
			}
			
			// Add separator between layers
				if (z < 2) KoobyLogManager.Log(LogCategory.Matrix, "-------------------");
		}
		
		KoobyLogManager.Log(LogCategory.Matrix, "=== End KoobState ===");
	}

	public List<Vector3> GetPossibleMoves(Vector3 currentPosition)
	{
		List<Vector3> possibleMoves = new List<Vector3>();
		
		int x = Mathf.RoundToInt(currentPosition.x);
		int y = Mathf.RoundToInt(currentPosition.y);
		int z = Mathf.RoundToInt(currentPosition.z);
		
		// Count how many axes are at position 1 (center)
		int centerAxes = (x == 1 ? 1 : 0) + (y == 1 ? 1 : 0) + (z == 1 ? 1 : 0);
		
		switch (centerAxes)
		{
			case 0: // Corner case - all axes are 0 or 2 (3 possible moves)
				return GetCornerMoves(x, y, z);
				
			case 1: // Edge case - one axis is 1, two are 0 or 2 (4 possible moves)
				return GetEdgeMoves(x, y, z);
				
			case 2: // Outer center case - two axes are 1, one is 0 or 2 (5 possible moves)
				return GetOuterCenterMoves(x, y, z);
				
			case 3: // Middle center case - all axes are 1 (6 possible moves)
				return GetMiddleCenterMoves(x, y, z);
				
			default:
				return possibleMoves;
		}
	}
	
	private List<Vector3> GetCornerMoves(int x, int y, int z)
	{
		List<Vector3> moves = new List<Vector3>();
		// Corner: move one step along each axis (toward center)
		CheckAndAddMove(moves, 1, y, z); // X to center
		CheckAndAddMove(moves, x, 1, z); // Y to center
		CheckAndAddMove(moves, x, y, 1); // Z to center
		return moves;
	}
	
	private List<Vector3> GetEdgeMoves(int x, int y, int z)
	{
		List<Vector3> moves = new List<Vector3>();
		// Edge: move to adjacent positions (1 cube away)
		
		// Move along the center axis to both ends
		CheckAndAddMove(moves, x==1?0:x, y==1?0:y, z==1?0:z);
		CheckAndAddMove(moves, x==1?2:x, y==1?2:y, z==1?2:z);
		// Move along the first non-center axis to center
		CheckAndAddMove(moves, x, y==1?y:1, z);
		// Move along the second non-center axis to center
		CheckAndAddMove(moves, x, y, z==1?z:1);
		
		return moves;
	}
	
	private List<Vector3> GetOuterCenterMoves(int x, int y, int z)
	{
		List<Vector3> moves = new List<Vector3>();
		// Outer center: move to adjacent positions (1 cube away)
		
		// Move along the non-center axis to center
		CheckAndAddMove(moves, x==1 ? x : 1, y==1 ? y : 1, z==1 ? z : 1);
		// Move along the first center axis to both ends
		if (x == 1)
		{
			CheckAndAddMove(moves, 0, y, z);
			CheckAndAddMove(moves, 2, y, z);
		}
		// Move along the second center axis to both ends
		if (y == 1)
		{
			CheckAndAddMove(moves, x, 0, z);
			CheckAndAddMove(moves, x, 2, z);
		}
		// Move along the third center axis to both ends
		if (z == 1)
		{
			CheckAndAddMove(moves, x, y, 0);
			CheckAndAddMove(moves, x, y, 2);
		}
		
		return moves;
	}
	
	private List<Vector3> GetMiddleCenterMoves(int x, int y, int z)
	{
		List<Vector3> moves = new List<Vector3>();
		// Middle center: move to both ends of all three axes
		CheckAndAddMove(moves, 0, y, z); // X to 0
		CheckAndAddMove(moves, 2, y, z); // X to 2
		CheckAndAddMove(moves, x, 0, z); // Y to 0
		CheckAndAddMove(moves, x, 2, z); // Y to 2
		CheckAndAddMove(moves, x, y, 0); // Z to 0
		CheckAndAddMove(moves, x, y, 2); // Z to 2
		return moves;
	}
	
	private void CheckAndAddMove(List<Vector3> possibleMoves, int x, int y, int z)
	{
		// Check if position is unoccupied
		KoobNode node = GetNode(x, y, z);
		if (!node.occupied)
			possibleMoves.Add(new Vector3(x, y, z));
	}

	public List<Vector3> GetPossibleMovesForPlayer(KoobPlayer player, out Vector3 winPosition)
	{
		winPosition = Vector3.zero;
		List<Vector3> allMoves = new List<Vector3>();
		List<Vector3> uniqueMoves = new List<Vector3>();
		
		var pieces = player.GetPieces();
		foreach (var piece in pieces)
		{
			var pieceMoves = GetPossibleMoves(piece.GetCurrentMatrixPosition());
			foreach (var move in pieceMoves)
			{
				// Check if this move position already exists (duplicate)
				if (allMoves.Contains(move))
				{
					// Duplicate found - this is a win position
					winPosition = move;
					// Don't add duplicate to unique moves
					continue;
				}
				allMoves.Add(move);
				uniqueMoves.Add(move);
			}
		}
		
		return uniqueMoves;
	}

	public List<Vector3> GetPossibleBumpMoves(Vector3 currentPosition, int currentPlayerID)
	{
		List<Vector3> bumpMoves = new List<Vector3>();
		int x = Mathf.RoundToInt(currentPosition.x);
		int y = Mathf.RoundToInt(currentPosition.y);
		int z = Mathf.RoundToInt(currentPosition.z);
		Vector3[] directions = new Vector3[]
		{
			new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
			new Vector3(0, 1, 0), new Vector3(0, -1, 0),
			new Vector3(0, 0, 1), new Vector3(0, 0, -1)
		};
		foreach (var dir in directions)
		{
			int adjX = x + Mathf.RoundToInt(dir.x);
			int adjY = y + Mathf.RoundToInt(dir.y);
			int adjZ = z + Mathf.RoundToInt(dir.z);
			if (!IsInBounds(adjX, adjY, adjZ)) continue;
			var adjNode = GetNode(adjX, adjY, adjZ);
			if (!adjNode.occupied || adjNode.playerID == currentPlayerID) continue;
			int pushX = adjX + Mathf.RoundToInt(dir.x);
			int pushY = adjY + Mathf.RoundToInt(dir.y);
			int pushZ = adjZ + Mathf.RoundToInt(dir.z);
			if (!IsInBounds(pushX, pushY, pushZ)) continue;
			var pushNode = GetNode(pushX, pushY, pushZ);
			if (!pushNode.occupied) bumpMoves.Add(new Vector3(adjX, adjY, adjZ));
		}
		return bumpMoves;
	}

	public List<Vector3> GetPossibleBumpMovesForPlayer(KoobPlayer player)
	{
		List<Vector3> allBumpMoves = new List<Vector3>();
		List<Vector3> uniqueBumpMoves = new List<Vector3>();
		var pieces = player.GetPieces();
		foreach (var piece in pieces)
		{
			var pieceBumpMoves = GetPossibleBumpMoves(piece.GetCurrentMatrixPosition(), player.id);
			foreach (var bumpMove in pieceBumpMoves)
			{
				if (!allBumpMoves.Contains(bumpMove))
				{
					allBumpMoves.Add(bumpMove);
					uniqueBumpMoves.Add(bumpMove);
				}
			}
		}
		return uniqueBumpMoves;
	}

	public bool IsInBounds(int x, int y, int z) => x >= 0 && x < 3 && y >= 0 && y < 3 && z >= 0 && z < 3;

	public bool TryGetPlayerOccupiedCells(int playerId, out Vector3Int cellA, out Vector3Int cellB)
	{
		cellA = default;
		cellB = default;
		var cells = new List<Vector3Int>();

		for (int x = 0; x < 3; x++)
			for (int y = 0; y < 3; y++)
				for (int z = 0; z < 3; z++)
				{
					var node = GetNode(x, y, z);
					if (node.occupied && node.playerID == playerId)
						cells.Add(new Vector3Int(x, y, z));
				}

		if (cells.Count != 2)
			return false;

		cellA = cells[0];
		cellB = cells[1];
		return true;
	}

	public static bool AreCellsAdjacent(Vector3Int a, Vector3Int b) =>
		Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z) == 1;

	public bool IsPlayerWinning(int playerId)
	{
		if (!TryGetPlayerOccupiedCells(playerId, out var cellA, out var cellB))
			return false;
		return AreCellsAdjacent(cellA, cellB);
	}
} 