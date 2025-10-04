using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class HUDManager : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private string styleSheetPath = "Assets/UI/HUD.uss"; // Fallback path
    
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    
    // UI Elements
    private Label turnStatusLabel;
    private Button leftButton;
    private Button moveButton;
    private Button rightButton;
    private VisualElement buttonRow;
    
    private void Start()
    {
        InitializeUI();
        
        // Auto-find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: Could not find GameManager in scene!");
                return;
            }
            KoobyLogManager.Log(LogCategory.UI_Output, "HUDManager: Auto-found GameManager");
        }
        
        SubscribeToGameEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: UIDocument not assigned!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        
        // Load the stylesheet
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }
        else
        {
            #if UNITY_EDITOR
            var loadedStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
            if (loadedStyleSheet != null)
            {
                root.styleSheets.Add(loadedStyleSheet);
                KoobyLogManager.Log(LogCategory.UI_Output, "Loaded HUD stylesheet from path: " + styleSheetPath);
            }
            else
            {
                KoobyLogManager.LogWarning(LogCategory.UI_Output, "Could not load HUD stylesheet from path: " + styleSheetPath);
            }
            #endif
        }
        
        // Get references to UI elements
        turnStatusLabel = root.Q<Label>("turnStatusLabel");
        leftButton = root.Q<Button>("leftButton");
        moveButton = root.Q<Button>("moveButton");
        rightButton = root.Q<Button>("rightButton");
        buttonRow = root.Q<VisualElement>("button-row");
        
        // Check for missing elements
        if (turnStatusLabel == null) KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: turnStatusLabel not found!");
        if (leftButton == null) KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: leftButton not found!");
        if (moveButton == null) KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: moveButton not found!");
        if (rightButton == null) KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: rightButton not found!");
        if (buttonRow == null) KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: button-row not found!");
        
        // Debug: Log which buttons were found
        KoobyLogManager.Log(LogCategory.UI_Output, $"Button status - Left: {(leftButton != null ? "Found" : "NULL")}, Move: {(moveButton != null ? "Found" : "NULL")}, Right: {(rightButton != null ? "Found" : "NULL")}");
        
        // Subscribe to button click events using UI Toolkit's built-in system
        if (leftButton != null)
            leftButton.clicked += OnLeftButtonClicked;
        if (moveButton != null)
            moveButton.clicked += OnMoveButtonClicked;
        if (rightButton != null)
            rightButton.clicked += OnRightButtonClicked;
        
        // Initially hide buttons
        SetButtonsVisible(false);
        
        KoobyLogManager.Log(LogCategory.UI_Output, "HUD UI initialized with button event subscriptions");
    }
    
    
    private void SubscribeToGameEvents()
    {
        if (gameManager != null)
        {
            // Subscribe to turn changes
            GameStateMachine.NewTurnBegan += OnNewTurnBegan;
            KoobyLogManager.Log(LogCategory.UI_Output, "HUDManager subscribed to NewTurnBegan event");
        }
        else
        {
            KoobyLogManager.LogError(LogCategory.UI_Output, "HUDManager: GameManager reference is null, cannot subscribe to events");
        }
    }
    
    private void UnsubscribeFromGameEvents()
    {
        if (gameManager != null)
        {
            GameStateMachine.NewTurnBegan -= OnNewTurnBegan;
        }
    }
    
    private void OnNewTurnBegan(KoobPlayer player)
    {
        UpdateTurnStatus(player);
        UpdateButtonVisibility(player);
    }
    
    private void UpdateTurnStatus(KoobPlayer player)
    {
        if (turnStatusLabel == null || player == null) return;
        
        turnStatusLabel.text = $"It's Player {player.id}'s turn.";
        
        // Get player color from first piece's material
        var pieces = player.GetPieces();
        if (pieces != null && pieces.Count > 0)
        {
            var renderer = pieces[0].GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color playerColor = renderer.material.color;
                turnStatusLabel.style.color = new StyleColor(playerColor);
                KoobyLogManager.Log(LogCategory.UI_Output, $"Updated turn status color to {playerColor} for Player {player.id}");
            }
        }
    }
    
    private void UpdateButtonVisibility(KoobPlayer player)
    {
        if (gameManager == null || player == null) return;
        
        // Check if current player is AI-controlled
        int playerIndex = gameManager.GetCurrentPlayerIndex(player);
        bool isAIPlayer = gameManager.IsPlayerAIControlled(playerIndex);
        
        KoobyLogManager.Log(LogCategory.UI_Output, $"UpdateButtonVisibility: Player {player.id}, Index: {playerIndex}, IsAI: {isAIPlayer}");
        
        SetButtonsVisible(!isAIPlayer);
        
        KoobyLogManager.Log(LogCategory.UI_Output, $"Buttons {(isAIPlayer ? "hidden" : "shown")} for Player {player.id} (AI: {isAIPlayer})");
    }
    
    private void SetButtonsVisible(bool visible)
    {
        if (buttonRow == null) return;
        
        buttonRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    // Button event handlers using UI Toolkit's built-in system
    private void OnLeftButtonClicked()
    {
        KoobyLogManager.Log(LogCategory.UI_Output, "Left button clicked");
        // TODO: Implement left button functionality
    }
    
    private void OnMoveButtonClicked()
    {
        KoobyLogManager.Log(LogCategory.UI_Output, "Move button clicked");
        // TODO: Implement move button functionality
    }
    
    private void OnRightButtonClicked()
    {
        KoobyLogManager.Log(LogCategory.UI_Output, "Right button clicked");
        // TODO: Implement right button functionality
    }
}
