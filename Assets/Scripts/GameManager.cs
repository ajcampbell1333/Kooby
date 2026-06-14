using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
	#region vars
		#region public vars
			public KoobState koobState;
			public KoobNodeSet koobNodeSet;
		public KoobPlayer GetCurrentPlayer() => gameStateMachine.CurrentPlayer;
		public UnityAction<KoobPlayer> GameWon;
		public UnityAction<Vector3> CurrentPlayerWillWinSoon;
		
		// Move choice cycling
		public int currentMoveChoice { get; private set; } = 0;
		public List<Vector3> currentPossibleMoves { get; private set; } = new List<Vector3>();
		public List<Vector3> currentPossibleBumpMoves { get; private set; } = new List<Vector3>();
		public UnityAction MoveChoiceChanged;
		
		public int TotalChoiceCount => currentPossibleMoves.Count + currentPossibleBumpMoves.Count;
		public bool IsCurrentChoiceBumpMove => currentMoveChoice >= currentPossibleMoves.Count;
		public string GetCurrentActionButtonText() => IsCurrentChoiceBumpMove ? "BUMP" : "MOVE";
		
		public int GetCurrentPlayerIndex(KoobPlayer player) => koobPlayers.IndexOf(player);
		public bool IsPlayerAIControlled(int playerIndex) => aiControlledPlayers != null && aiControlledPlayers.Length > playerIndex && aiControlledPlayers[playerIndex];
		
		public void CycleMoveChoice(bool forward)
		{
			if (TotalChoiceCount == 0) return;
			
			if (forward)
			{
				currentMoveChoice = (currentMoveChoice + 1) % TotalChoiceCount;
			}
			else
			{
				currentMoveChoice = (currentMoveChoice - 1 + TotalChoiceCount) % TotalChoiceCount;
			}
			
			KoobyLogManager.Log(LogCategory.Manager, $"Move choice cycled to index {currentMoveChoice} of {TotalChoiceCount} ({currentPossibleMoves.Count} moves, {currentPossibleBumpMoves.Count} bumps)");
			
			UpdateHighlightForCurrentMove();
			MoveChoiceChanged?.Invoke();
		}
		
		private void UpdateHighlightForCurrentMove()
		{
			if (highlightsManager == null || koobNodeSet == null) return;
			if (TotalChoiceCount == 0 || currentMoveChoice >= TotalChoiceCount) return;
			
			Vector3 selectedPosition = GetCurrentChoicePosition();
			Vector3 worldPosition = koobNodeSet.GetPos(selectedPosition);
			highlightsManager.PlaceSingleHighlight(worldPosition);
			
			string choiceType = IsCurrentChoiceBumpMove ? "bump" : "move";
			KoobyLogManager.Log(LogCategory.Manager, $"Updated highlight to show {choiceType} choice {currentMoveChoice} at {worldPosition}");
		}
		
		private Vector3 GetCurrentChoicePosition()
		{
			if (IsCurrentChoiceBumpMove)
				return currentPossibleBumpMoves[currentMoveChoice - currentPossibleMoves.Count];
			return currentPossibleMoves[currentMoveChoice];
		}
		#endregion public vars
		
		#region private vars
			[SerializeField] private GameObject koobMatrixPrefab;
			[SerializeField] private GameObject koobPlayerPrefab;
			[SerializeField] private GameObject playerPiecePrefab;
			[SerializeField] private ScriptableCurve playerMoveCurve; // Animation curve template for player piece movement
			[SerializeField] private bool _debugMotion = false;
			[SerializeField] private bool _enableAI = false;
			[SerializeField] private bool[] aiControlledPlayers = new bool[4] { true, true, true, true };
			[SerializeField] private float aiMoveDelaySeconds = 5f;
			private GameObject koobMatrixInstance;
			private GameStateMachine gameStateMachine;
			private List<KoobPlayer> koobPlayers = new List<KoobPlayer>();
			private List<PlayerPiece> allPlayerPieces = new List<PlayerPiece>();
			private List<NPCPlayer> npcPlayers = new List<NPCPlayer>();
			private Coroutine aiTurnCoroutine;
			private Coroutine bumpCoroutine;
			private HighlightsManager highlightsManager;
			private CameraOrbit cameraOrbit;
			
			private const int NUM_PLAYERS = 4;
			private const int PIECES_PER_PLAYER = 2;
		#endregion private vars
	#endregion vars

	private void Start()
	{
		InitManagersAndHUD();
		InitMatrixAndNodeSet();
		InitStateAndPlayers();
		StartGame();
	}
	
	public void StartGame()
	{
		if (!ValidateConfig()) return;
		SetupPiecesAndState();
		SetupAIIfEnabled();
		SetInitialPlayer();
		EnableDebugMotionIfRequested();
	}

	private void OnDestroy()
	{
		// Unsubscribe turn-begin handler
		GameStateMachine.NewTurnBegan -= OnNewTurnBegan;
	}
	
	private void OnPlayerPieceFinishedMoving(PlayerPiece piece)
	{
		// Clear the old position in KoobState
		Vector3 oldPosition = piece.GetPreviousMatrixPosition();
		int oldX = Mathf.RoundToInt(oldPosition.x);
		int oldY = Mathf.RoundToInt(oldPosition.y);
		int oldZ = Mathf.RoundToInt(oldPosition.z);
		koobState.SetNode(oldX, oldY, oldZ, false, 0);
		
		// Update KoobState with the piece's new position
		Vector3 newPosition = piece.GetCurrentMatrixPosition();
		int x = Mathf.RoundToInt(newPosition.x);
		int y = Mathf.RoundToInt(newPosition.y);
		int z = Mathf.RoundToInt(newPosition.z);
		
		// Use stored owner player id instead of parsing from name
		string pieceName = piece.gameObject.name;
		int playerID = piece.GetOwnerPlayerId();
		
		// Debug: Print the move details BEFORE updating KoobState
		KoobyLogManager.Log(LogCategory.Matrix, $"Move: {pieceName} from {oldPosition} to {newPosition}");
		KoobyLogManager.Log(LogCategory.Matrix, $"KoobState BEFORE update - Old pos ({oldX},{oldY},{oldZ}): {koobState.GetNode(oldX, oldY, oldZ).occupied}, New pos ({x},{y},{z}): {koobState.GetNode(x, y, z).occupied}");
		
		// Update the KoobState
		koobState.SetNode(x, y, z, true, playerID);
		
		// Debug: Print the move details AFTER updating KoobState
		KoobyLogManager.Log(LogCategory.Matrix, $"KoobState AFTER update - Old pos ({oldX},{oldY},{oldZ}): {koobState.GetNode(oldX, oldY, oldZ).occupied}, New pos ({x},{y},{z}): {koobState.GetNode(x, y, z).occupied}");
		
		// Efficiently update possible moves for the position change
		RefreshPossibleMovesForPositionChange(oldPosition, newPosition);

		CubeAnimController animController = piece.GetComponent<CubeAnimController>();
		if (animController != null)
			animController.CaptureRestState();

		// Check for win before advancing the turn
		KoobPlayer activePlayer = gameStateMachine.CurrentPlayer;
		if (CheckForWin(activePlayer))
		{
			// Winner found; do not advance turn
			return;
		}
		
		// Cycle to next player from whoever took the turn (move or bump)
		int currentPlayerIndex = koobPlayers.IndexOf(activePlayer);
		int nextPlayerIndex = (currentPlayerIndex + 1) % NUM_PLAYERS;
		gameStateMachine.SetCurrentPlayer(koobPlayers[nextPlayerIndex]);
		
		KoobyLogManager.Log(LogCategory.Manager, $"Updated KoobState for {pieceName} at position ({x},{y},{z}). Next player: Player{nextPlayerIndex + 1}");
	}

	#region Helper Methods
	private void InitManagersAndHUD()
	{
		highlightsManager = GetComponent<HighlightsManager>();
		if (highlightsManager == null)
			KoobyLogManager.LogWarning(LogCategory.Manager, "HighlightsManager component not found on same GameObject.");
		cameraOrbit = Camera.main.GetComponent<CameraOrbit>();
		if (cameraOrbit == null)
			KoobyLogManager.LogWarning(LogCategory.Manager, "CameraOrbit component not found on Main Camera.");
		var hudManager = FindObjectOfType<HUDManager>();
		if (hudManager != null)
		{
			hudManager.LeftButtonClicked += () => CycleMoveChoice(false);
			hudManager.RightButtonClicked += () => CycleMoveChoice(true);
			KoobyLogManager.Log(LogCategory.Manager, "Subscribed to HUD button events");
		}
		else
			KoobyLogManager.LogWarning(LogCategory.Manager, "HUDManager not found in scene");
	}

	private void InitMatrixAndNodeSet()
	{
		if (!koobMatrixPrefab)
		{
			KoobyLogManager.LogWarning(LogCategory.Manager, "Koob Matrix Prefab not assigned.");
			return;
		}
		koobMatrixInstance = Instantiate(koobMatrixPrefab);
		koobNodeSet = koobMatrixInstance.GetComponent<KoobNodeSet>();
		if (koobNodeSet == null)
			KoobyLogManager.LogWarning(LogCategory.Manager, "KoobNodeSet component not found on koobMatrixInstance.");
		if (cameraOrbit != null)
			cameraOrbit.SetOrbitTarget(koobMatrixInstance.transform);
	}

	private void InitStateAndPlayers()
	{
		koobState = new KoobState();
		koobState.ResetBoard();
		gameStateMachine = new GameStateMachine();
		CreatePlayers();
	}

	private bool ValidateConfig()
	{
		if (playerPiecePrefab == null)
		{
			KoobyLogManager.LogError(LogCategory.Manager, "Player Piece Prefab not assigned. Assign Tessellation_Cube_Rig.");
			return false;
		}
		if (koobNodeSet == null)
		{
			KoobyLogManager.LogError(LogCategory.Manager, "KoobNodeSet not available. Make sure koobMatrixPrefab has KoobNodeSet component.");
			return false;
		}
		return true;
	}

	private void SetupPiecesAndState()
	{
		CreatePlayerPieces();
		koobState.ResetBoard();
		InitializeKoobStateWithStartingPositions();
		koobState.PrintKoobState();
	}

	private void SetupAIIfEnabled()
	{
		if (!_enableAI) return;
		InitializeAIPlayers();
		GameStateMachine.NewTurnBegan += OnNewTurnBegan;
	}

	private void SetInitialPlayer()
	{
		gameStateMachine.SetCurrentPlayer(koobPlayers[0]);
		KoobyLogManager.Log(LogCategory.Manager, $"Game started! Created {NUM_PLAYERS * PIECES_PER_PLAYER} player pieces total.");
	}

	private void EnableDebugMotionIfRequested()
	{
		if (!_debugMotion) return;
		var randomMoves = gameObject.AddComponent<RandomContinuousMoves>();
		randomMoves.Initialize(this, allPlayerPieces);
	}
	#endregion Helper Methods

	public void ExecuteCurrentMoveChoice()
	{
		var player = gameStateMachine.CurrentPlayer;
		if (player == null) return;
		if (TotalChoiceCount == 0) return;
		if (currentMoveChoice < 0 || currentMoveChoice >= TotalChoiceCount) return;
		
		if (IsCurrentChoiceBumpMove)
		{
			Vector3 targetCell = GetCurrentChoicePosition();
			if (!TryResolveBumpInteraction(player, targetCell, out BumpInteraction interaction))
			{
				KoobyLogManager.LogWarning(LogCategory.Player, $"Human Player {player.id} bump failed — could not resolve bumper/bumpee for {targetCell}");
				return;
			}

			if (bumpCoroutine != null)
				StopCoroutine(bumpCoroutine);
			bumpCoroutine = StartCoroutine(ExecuteBumpInteraction(interaction));
			return;
		}
		
		Vector3 logicalDestination = currentPossibleMoves[currentMoveChoice];
		var playerPieces = player.GetPieces();
		if (playerPieces == null || playerPieces.Count == 0) return;
		PlayerPiece mover = null;
		foreach (var p in playerPieces)
		{
			var moves = p.GetPossibleMoves();
			if (moves.Contains(logicalDestination))
			{
				mover = p;
				break;
			}
		}
		if (mover == null) return;
		Vector3 worldDestination = koobNodeSet.GetPos(logicalDestination);
		mover.Move(worldDestination, logicalDestination);
		KoobyLogManager.Log(LogCategory.Player, $"Human Player {player.id} moved {mover.gameObject.name} to {logicalDestination}");
	}

	private bool TryResolveBumpInteraction(KoobPlayer player, Vector3 targetCell, out BumpInteraction interaction)
	{
		interaction = default;
		if (player == null) return false;

		PlayerPiece bumper = FindBumperPiece(player, targetCell);
		PlayerPiece bumpee = FindPieceAtCell(targetCell);
		if (bumper == null || bumpee == null) return false;

		Vector3 bumperPos = bumper.GetCurrentMatrixPosition();
		Vector3 delta = targetCell - bumperPos;
		var axisDirection = new Vector3Int(
			Mathf.RoundToInt(delta.x),
			Mathf.RoundToInt(delta.y),
			Mathf.RoundToInt(delta.z));

		if (Mathf.Abs(axisDirection.x) + Mathf.Abs(axisDirection.y) + Mathf.Abs(axisDirection.z) != 1)
			return false;

		interaction = new BumpInteraction
		{
			Bumper = bumper,
			Bumpee = bumpee,
			AxisDirection = axisDirection
		};
		return true;
	}

	private PlayerPiece FindBumperPiece(KoobPlayer player, Vector3 targetCell)
	{
		foreach (var piece in player.GetPieces())
		{
			var bumpMoves = koobState.GetPossibleBumpMoves(piece.GetCurrentMatrixPosition(), player.id);
			if (bumpMoves.Contains(targetCell))
				return piece;
		}
		return null;
	}

	private PlayerPiece FindPieceAtCell(Vector3 cell)
	{
		foreach (var piece in allPlayerPieces)
		{
			if (piece.GetCurrentMatrixPosition() == cell)
				return piece;
		}
		return null;
	}

	private IEnumerator ExecuteBumpInteraction(BumpInteraction interaction)
	{
		BumpInteractionEvents.Raise(interaction);

		int bumperPlayerId = interaction.Bumper.GetOwnerPlayerId();
		KoobyLogManager.Log(LogCategory.Player,
			$"Human Player {bumperPlayerId} bumping {interaction.Bumpee.gameObject.name} with {interaction.Bumper.gameObject.name} along {interaction.AxisDirection}");

		float animDuration = GetCollisionAnimationDuration(interaction.Bumpee);
		if (animDuration > 0f)
			yield return new WaitForSeconds(animDuration);

		Vector3 pushDestination = GetBumpPushDestination(interaction);
		if (!IsValidBumpPushDestination(interaction.Bumpee, pushDestination))
		{
			KoobyLogManager.LogWarning(LogCategory.Player,
				$"Bump push aborted — destination {pushDestination} is not valid for {interaction.Bumpee.gameObject.name}");
			bumpCoroutine = null;
			yield break;
		}

		if (interaction.Bumpee.IsMoving)
		{
			KoobyLogManager.LogWarning(LogCategory.Player, $"Bump push aborted — {interaction.Bumpee.gameObject.name} is already moving");
			bumpCoroutine = null;
			yield break;
		}

		Vector3 worldDestination = koobNodeSet.GetPos(pushDestination);
		interaction.Bumpee.Move(worldDestination, pushDestination, PlayerPiece.BumpSpeedMultiplier);
		KoobyLogManager.Log(LogCategory.Player,
			$"Bump pushed {interaction.Bumpee.gameObject.name} to {pushDestination}");

		bumpCoroutine = null;
	}

	private static Vector3 GetBumpPushDestination(BumpInteraction interaction)
	{
		Vector3 direction = new Vector3(
			interaction.AxisDirection.x,
			interaction.AxisDirection.y,
			interaction.AxisDirection.z);
		return interaction.Bumpee.GetCurrentMatrixPosition() + direction;
	}

	private bool IsValidBumpPushDestination(PlayerPiece bumpee, Vector3 destination)
	{
		int x = Mathf.RoundToInt(destination.x);
		int y = Mathf.RoundToInt(destination.y);
		int z = Mathf.RoundToInt(destination.z);
		if (x < 0 || x > 2 || y < 0 || y > 2 || z < 0 || z > 2)
			return false;

		var node = koobState.GetNode(x, y, z);
		return !node.occupied;
	}

	private static float GetCollisionAnimationDuration(PlayerPiece piece)
	{
		if (piece == null) return 0f;
		var animController = piece.GetComponent<CubeAnimController>();
		return animController != null ? animController.CollisionAnimationDuration : 0f;
	}

	private bool CheckForWin(KoobPlayer player)
	{
		if (player == null) return false;

		if (!koobState.IsPlayerWinning(player.id))
		{
			var pieces = player.GetPieces();
			if (pieces != null && pieces.Count == 2)
			{
				Vector3 a = pieces[0].GetCurrentMatrixPosition();
				Vector3 b = pieces[1].GetCurrentMatrixPosition();
				KoobyLogManager.Log(LogCategory.Player,
					$"Win check failed for Player {player.id}: piece positions {a} and {b}");
			}
			return false;
		}

		Debug.Log($"PLAYER {player.id} WON!!!");
		KoobyLogManager.Log(LogCategory.Player, $"PLAYER {player.id} WON!!!");
		GameWon?.Invoke(player);
		return true;
	}
	
	private Vector3 GetStartingPosition(int player, int piece)
	{
		// Starting positions: each player gets opposite corners of the cube
		switch (player)
		{
			case 0: // Player 1: (0,0,0) and (2,2,2)
				return piece == 0 ? new Vector3(0, 0, 0) : new Vector3(2, 2, 2);
			case 1: // Player 2: (2,0,0) and (0,2,2)
				return piece == 0 ? new Vector3(2, 0, 0) : new Vector3(0, 2, 2);
			case 2: // Player 3: (0,2,0) and (2,0,2) - FIXED: now opposite corners
				return piece == 0 ? new Vector3(0, 2, 0) : new Vector3(2, 0, 2);
			case 3: // Player 4: (0,0,2) and (2,2,0)
				return piece == 0 ? new Vector3(0, 0, 2) : new Vector3(2, 2, 0);
			default:
				return Vector3.zero;
		}
	}
	
	private void CreatePlayers()
	{
		if (koobPlayerPrefab == null)
		{
			KoobyLogManager.LogError(LogCategory.Manager, "KoobPlayer Prefab not assigned.");
			return;
		}
		
		koobPlayers.Clear();
		
		for (int i = 0; i < NUM_PLAYERS; i++)
		{
			GameObject playerInstance = Instantiate(koobPlayerPrefab);
			playerInstance.name = $"Player{i + 1}";
			
			KoobPlayer koobPlayer = playerInstance.GetComponent<KoobPlayer>();
			if (koobPlayer != null)
			{
				koobPlayer.id = i + 1; // Player IDs start from 1
				koobPlayers.Add(koobPlayer);
			}
			else
			{
				KoobyLogManager.LogError(LogCategory.Manager, $"KoobPlayer component not found on prefab for Player {i + 1}.");
				Destroy(playerInstance);
			}
		}
		
		// Final verification
		if (koobPlayers.Count != NUM_PLAYERS)
			KoobyLogManager.LogError(LogCategory.Manager, $"Expected {NUM_PLAYERS} players but only created {koobPlayers.Count}.");
		
		KoobyLogManager.Log(LogCategory.Manager, $"Created {koobPlayers.Count} players.");
	}
	
	private void CreatePlayerPieces()
	{
		for (int player = 0; player < NUM_PLAYERS; player++)
		{
			for (int piece = 0; piece < PIECES_PER_PLAYER; piece++)
			{
				GameObject playerPiece = Instantiate(playerPiecePrefab);
				playerPiece.name = $"Player{player + 1}_Piece{piece + 1}";

				RemoveCollidersInHierarchy(playerPiece);

				PlayerPiece playerPieceComponent = playerPiece.GetComponent<PlayerPiece>();
				if (playerPieceComponent == null)
					playerPieceComponent = playerPiece.AddComponent<PlayerPiece>();

				if (playerPiece.GetComponent<PieceBumpAnimationHandler>() == null)
					playerPiece.AddComponent<PieceBumpAnimationHandler>();

				playerPieceComponent.BeganMoving += gameStateMachine.OnPlayerPieceBeganMoving;
				playerPieceComponent.FinishedMoving += OnPlayerPieceFinishedMoving;

				if (playerMoveCurve != null)
					playerPieceComponent.SetMoveCurve(playerMoveCurve);

				ApplyPlayerPieceColor(playerPiece, player);

				playerPiece.transform.localScale = Vector3.one * 0.9f;

				Vector3 logicalPosition = GetStartingPosition(player, piece);
				koobNodeSet.SetPos(playerPiece.transform, logicalPosition);

				CubeAnimController animController = playerPiece.GetComponent<CubeAnimController>();
				if (animController != null)
					animController.CaptureRestState();

				playerPieceComponent.SetMatrixPosition(logicalPosition);

				List<Vector3> initialPossibleMoves = koobState.GetPossibleMoves(logicalPosition);
				playerPieceComponent.SetPossibleMoves(initialPossibleMoves);

				playerPieceComponent.SetOwnerPlayer(koobPlayers[player]);
				koobPlayers[player].AssignPiece(playerPieceComponent);
				allPlayerPieces.Add(playerPieceComponent);

				KoobyLogManager.Log(LogCategory.Player, $"Created {playerPiece.name} with color {PlayerPieceColors.Get(player)} at logical position {logicalPosition} with {initialPossibleMoves.Count} possible moves");
			}
		}
	}

	private static void RemoveCollidersInHierarchy(GameObject root)
	{
		foreach (Collider collider in root.GetComponentsInChildren<Collider>())
			Destroy(collider);
	}

	private static void ApplyPlayerPieceColor(GameObject pieceRoot, int playerIndex)
	{
		MeshRenderer meshRenderer = pieceRoot.GetComponentInChildren<MeshRenderer>();
		if (meshRenderer == null)
		{
			KoobyLogManager.LogWarning(LogCategory.Player, $"No MeshRenderer found under {pieceRoot.name}; player color not applied.");
			return;
		}

		Material instance = new Material(meshRenderer.sharedMaterial);
		instance.SetColor("_Color", PlayerPieceColors.Get(playerIndex));
		meshRenderer.material = instance;
	}
	
	private void InitializeKoobStateWithStartingPositions()
	{
		// Set all starting positions in KoobState
		for (int player = 0; player < NUM_PLAYERS; player++)
		{
			for (int piece = 0; piece < PIECES_PER_PLAYER; piece++)
			{
				Vector3 logicalPosition = GetStartingPosition(player, piece);
				int x = Mathf.RoundToInt(logicalPosition.x);
				int y = Mathf.RoundToInt(logicalPosition.y);
				int z = Mathf.RoundToInt(logicalPosition.z);
				koobState.SetNode(x, y, z, true, player + 1); // Player IDs start from 1
			}
		}
	}
	
	private void RefreshAllPossibleMoves()
	{
		// Update possible moves for all pieces since the board state changed
		foreach (var piece in allPlayerPieces)
		{
			Vector3 currentPos = piece.GetCurrentMatrixPosition();
			List<Vector3> newPossibleMoves = koobState.GetPossibleMoves(currentPos);
			piece.SetPossibleMoves(newPossibleMoves);
		}
	}
	
	private void RefreshPossibleMovesForPositionChange(Vector3 oldPosition, Vector3 newPosition)
	{
		KoobyLogManager.Log(LogCategory.Matrix, $"Refreshing moves: removing {newPosition} from all pieces, adding {oldPosition} to adjacent pieces");
		
		// Remove newly occupied position from all pieces' possible moves
		foreach (var piece in allPlayerPieces)
		{
			var currentMoves = piece.GetPossibleMoves();
			if (currentMoves.Contains(newPosition))
			{
				KoobyLogManager.Log(LogCategory.Matrix, $"Removing {newPosition} from {piece.gameObject.name}'s possible moves");
				currentMoves.Remove(newPosition);
				piece.SetPossibleMoves(currentMoves);
			}
		}
		
		// Add newly unoccupied position to adjacent pieces' possible moves
		foreach (var piece in allPlayerPieces)
		{
			Vector3 currentPos = piece.GetCurrentMatrixPosition();
			List<Vector3> adjacentPositions = koobState.GetPossibleMoves(currentPos);
			if (adjacentPositions.Contains(oldPosition))
			{
				KoobyLogManager.Log(LogCategory.Matrix, $"Adding {oldPosition} to {piece.gameObject.name}'s possible moves (was adjacent)");
				// This piece is adjacent to the newly unoccupied position, refresh its moves
				piece.SetPossibleMoves(adjacentPositions);
			}
		}
	}
	
	private void InitializeAIPlayers()
	{
		npcPlayers.Clear();
		
		// Create AI for players 2, 3, and 4 (keep player 1 human)
		for (int i = 0; i < NUM_PLAYERS; i++)
		{
			bool shouldCreateAI = aiControlledPlayers != null && aiControlledPlayers.Length > i && aiControlledPlayers[i];
			if (!shouldCreateAI) continue;
			var npcPlayer = gameObject.AddComponent<NPCPlayer>();
			npcPlayer.Initialize(koobPlayers[i]);
			npcPlayers.Add(npcPlayer);
		}
		
		KoobyLogManager.Log(LogCategory.Manager, $"Initialized {npcPlayers.Count} AI players");
	}
	
	private void OnNewTurnBegan(KoobPlayer player)
	{
		// Reset highlights from previous turn
		if (highlightsManager != null)
			highlightsManager.ResetHighlights();
		
		// Check for win opportunity (duplicate moves)
		Vector3 winPosition;
		var uniqueMoves = koobState.GetPossibleMovesForPlayer(player, out winPosition);
		if (winPosition != Vector3.zero)
		{
			CurrentPlayerWillWinSoon?.Invoke(winPosition);
			KoobyLogManager.Log(LogCategory.Player, $"Player {player.id} can win by moving to {winPosition}!");
		}
		
		// Initialize move choice for human players BEFORE showing highlights
		int playerIndex = koobPlayers.IndexOf(player);
		bool isAI = playerIndex >= 0 && aiControlledPlayers != null && aiControlledPlayers.Length > playerIndex && aiControlledPlayers[playerIndex];
		
		if (!isAI)
		{
			// Human player - initialize move choice and bump moves
			currentPossibleMoves = uniqueMoves;
			currentPossibleBumpMoves = koobState.GetPossibleBumpMovesForPlayer(player);
			currentMoveChoice = 0;
			KoobyLogManager.Log(LogCategory.Manager, $"Human player {player.id} turn - initialized with {currentPossibleMoves.Count} possible moves and {currentPossibleBumpMoves.Count} possible bump moves");
		}
		else
		{
			// AI player - clear move choice and bump moves
			currentPossibleMoves.Clear();
			currentPossibleBumpMoves.Clear();
			currentMoveChoice = 0;
			KoobyLogManager.Log(LogCategory.Manager, $"AI player {player.id} turn - cleared move choice");
		}
		
		// Show highlights for possible moves (now that move choice is initialized)
		ShowHighlightsForPlayer(player);
		
		if (!isAI)
			MoveChoiceChanged?.Invoke();
		
		// Handle AI turn
		if (!_enableAI || player == null) return;
		if (playerIndex < 0) return;
		if (!isAI) return;
		if (aiTurnCoroutine != null) StopCoroutine(aiTurnCoroutine);
		aiTurnCoroutine = StartCoroutine(ExecuteAIMoveAfterDelay(player, aiMoveDelaySeconds));
	}

	private void ShowHighlightsForPlayer(KoobPlayer player)
	{
		if (highlightsManager == null || koobNodeSet == null) return;
		
		// Get unique possible moves for this player
		Vector3 winPosition;
		var uniqueMoves = koobState.GetPossibleMovesForPlayer(player, out winPosition);
		
		// Check if player is AI
		int playerIndex = koobPlayers.IndexOf(player);
		bool isAI = playerIndex >= 0 && aiControlledPlayers != null && aiControlledPlayers.Length > playerIndex && aiControlledPlayers[playerIndex];
		
		if (isAI)
		{
			// AI player - show all possible moves
			KoobyLogManager.Log(LogCategory.Manager, $"Showing {uniqueMoves.Count} highlights for AI Player {player.id}");
			foreach (var move in uniqueMoves)
			{
				Vector3 worldPosition = koobNodeSet.GetPos(move);
				highlightsManager.PlaceHighlight(worldPosition);
			}
		}
		else
		{
			UpdateHighlightForCurrentMove();
		}
	}

	private IEnumerator ExecuteAIMoveAfterDelay(KoobPlayer player, float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		var npcPlayer = npcPlayers.Find(npc => npc != null && npc.player == player);
		if (npcPlayer == null) yield break;
		var pieceToMove = npcPlayer.ChooseBestMove();
		if (pieceToMove == null) yield break;
		var bestMove = npcPlayer.ChooseBestMoveForPiece(pieceToMove);
		if (bestMove == Vector3.zero) yield break;
		var worldMove = koobNodeSet.GetPos(bestMove);
		pieceToMove.Move(worldMove, bestMove);
		KoobyLogManager.Log(LogCategory.Player, $"AI Player {player.id} moved {pieceToMove.gameObject.name} to {bestMove}");
	}
} 