using UnityEngine;
using UnityEngine.Events;

public enum PlayState
{
    MainMenu,
    Pause,
    Play,
    GameOver
}

public enum TurnState
{
    Beginning,
    InProgress,
    Ending
}

public class GameStateMachine
{
    public static UnityAction<KoobPlayer> NewTurnBegan;
    
    private KoobPlayer currentPlayer;
    private PlayState playState;
    private TurnState turnState;
    
    public KoobPlayer CurrentPlayer => currentPlayer;
    public PlayState PlayState => playState;
    public TurnState TurnState => turnState;
    
    public void SetCurrentPlayer(KoobPlayer player)
    {
        currentPlayer = player;
        turnState = TurnState.Beginning;
        NewTurnBegan?.Invoke(player);
    }
    
    public void OnPlayerPieceBeganMoving(PlayerPiece piece)
    {
        turnState = TurnState.Ending;
    }
}
