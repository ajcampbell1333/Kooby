using UnityEngine;

    public enum LogCategory
    {
        Manager,
        Player,
        Matrix,
        UI_Input,
        UI_Output
    }

public class KoobyLogManager : MonoBehaviour
{
    public static KoobyLogManager Instance { get; private set; }
    
    [SerializeField] private bool enableManagerLogs = true;
    [SerializeField] private bool enablePlayerLogs = true;
    [SerializeField] private bool enableMatrixLogs = true;
    [SerializeField] private bool enableUIInputLogs = true;
    [SerializeField] private bool enableUIOutputLogs = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void Log(LogCategory category, string message)
    {
        if (Instance == null) return;
        
        bool shouldLog = false;
        string categoryName = "";
        
        switch (category)
        {
            case LogCategory.Manager:
                shouldLog = Instance.enableManagerLogs;
                categoryName = "MANAGER";
                break;
            case LogCategory.Player:
                shouldLog = Instance.enablePlayerLogs;
                categoryName = "PLAYER";
                break;
            case LogCategory.Matrix:
                shouldLog = Instance.enableMatrixLogs;
                categoryName = "MATRIX";
                break;
            case LogCategory.UI_Input:
                shouldLog = Instance.enableUIInputLogs;
                categoryName = "UI_Input";
                break;
            case LogCategory.UI_Output:
                shouldLog = Instance.enableUIOutputLogs;
                categoryName = "UI_Output";
                break;
        }
        
        if (shouldLog)
        {
            Debug.Log($"[{categoryName}] {message}");
        }
    }
    
    public static void LogWarning(LogCategory category, string message)
    {
        if (Instance == null) return;
        
        bool shouldLog = false;
        string categoryName = "";
        
        switch (category)
        {
            case LogCategory.Manager:
                shouldLog = Instance.enableManagerLogs;
                categoryName = "MANAGER";
                break;
            case LogCategory.Player:
                shouldLog = Instance.enablePlayerLogs;
                categoryName = "PLAYER";
                break;
            case LogCategory.Matrix:
                shouldLog = Instance.enableMatrixLogs;
                categoryName = "MATRIX";
                break;
            case LogCategory.UI_Input:
                shouldLog = Instance.enableUIInputLogs;
                categoryName = "UI_Input";
                break;
            case LogCategory.UI_Output:
                shouldLog = Instance.enableUIOutputLogs;
                categoryName = "UI_Output";
                break;
        }
        
        if (shouldLog)
        {
            Debug.LogWarning($"[{categoryName}] {message}");
        }
    }
    
    public static void LogError(LogCategory category, string message)
    {
        if (Instance == null) return;
        
        bool shouldLog = false;
        string categoryName = "";
        
        switch (category)
        {
            case LogCategory.Manager:
                shouldLog = Instance.enableManagerLogs;
                categoryName = "MANAGER";
                break;
            case LogCategory.Player:
                shouldLog = Instance.enablePlayerLogs;
                categoryName = "PLAYER";
                break;
            case LogCategory.Matrix:
                shouldLog = Instance.enableMatrixLogs;
                categoryName = "MATRIX";
                break;
            case LogCategory.UI_Input:
                shouldLog = Instance.enableUIInputLogs;
                categoryName = "UI_Input";
                break;
            case LogCategory.UI_Output:
                shouldLog = Instance.enableUIOutputLogs;
                categoryName = "UI_Output";
                break;
        }
        
        if (shouldLog)
        {
            Debug.LogError($"[{categoryName}] {message}");
        }
    }
}
