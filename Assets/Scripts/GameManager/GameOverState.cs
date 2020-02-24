using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// state pushed on top of the GameManager when the player dies.
/// </summary>
public class GameOverState : AState, InputActions.IGameOverActions
{
    public TrackManager trackManager;
    public Canvas canvas;

	public AudioClip gameOverTheme;

	public Leaderboard miniLeaderboard;
	public Leaderboard fullLeaderboard;

    private EventSystem eventSystem;
    public override void Enter(AState from)
    {
        if(eventSystem == null) eventSystem = EventSystem.current;

        canvas.gameObject.SetActive(true);

        StartCoroutine(FocusPlayerInput());
		miniLeaderboard.playerEntry.inputName.text = PlayerData.instance.previousName;
		
		miniLeaderboard.playerEntry.score.text = trackManager.score.ToString();
		miniLeaderboard.Populate();

		CreditCoins();

		if (MusicPlayer.instance.GetStem(0) != gameOverTheme)
		{
            MusicPlayer.instance.SetStem(0, gameOverTheme);
			StartCoroutine(MusicPlayer.instance.RestartAllStems());
        }
    }

    IEnumerator<UnityEngine.WaitForEndOfFrame> FocusPlayerInput() {
        Debug.Log("FocusPlayerInput");
        eventSystem.SetSelectedGameObject(null);
 		yield return new WaitForEndOfFrame();
		eventSystem.SetSelectedGameObject(miniLeaderboard.playerEntry.inputName.gameObject);
		yield return new WaitForEndOfFrame();
    }

	public override void Exit(AState to)
    {
        canvas.gameObject.SetActive(false);
        FinishRun();
    }

    public override string GetName()
    {
        return "GameOver";
    }

    public override void Tick()
    {
        
    }

	public void OpenLeaderboard()
	{
		fullLeaderboard.forcePlayerDisplay = false;
		fullLeaderboard.displayPlayer = true;
		fullLeaderboard.playerEntry.playerName.text = miniLeaderboard.playerEntry.inputName.text;
		fullLeaderboard.playerEntry.score.text = trackManager.score.ToString();

		fullLeaderboard.Open();
    }

    public void GoToLoadout()
    {
        trackManager.isRerun = false;
		manager.SwitchState("Loadout");
    }

    public void RunAgain()
    {
        trackManager.isRerun = false;
        manager.SwitchState("Game");
    }

    protected void CreditCoins()
	{
		PlayerData.instance.Save();

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
        var transactionId = System.Guid.NewGuid().ToString();
        var transactionContext = "gameplay";
        var level = PlayerData.instance.rank.ToString();
        var itemType = "consumable";
        
        if (trackManager.characterController.coins > 0)
        {
            AnalyticsEvent.ItemAcquired(
                AcquisitionType.Soft, // Currency type
                transactionContext,
                trackManager.characterController.coins,
                "fishbone",
                PlayerData.instance.coins,
                itemType,
                level,
                transactionId
            );
        }

        if (trackManager.characterController.premium > 0)
        {
            AnalyticsEvent.ItemAcquired(
                AcquisitionType.Premium, // Currency type
                transactionContext,
                trackManager.characterController.premium,
                "anchovies",
                PlayerData.instance.premium,
                itemType,
                level,
                transactionId
            );
        }
#endif 
	}

	protected void FinishRun()
    {
		if(miniLeaderboard.playerEntry.inputName.text == "")
		{
			miniLeaderboard.playerEntry.inputName.text = "Trash Cat";
		}
		else
		{
			PlayerData.instance.previousName = miniLeaderboard.playerEntry.inputName.text;
		}

        PlayerData.instance.InsertScore(trackManager.score, miniLeaderboard.playerEntry.inputName.text );

        CharacterCollider.DeathEvent de = trackManager.characterController.characterCollider.deathData;
        //register data to analytics
#if UNITY_ANALYTICS
        AnalyticsEvent.GameOver(null, new Dictionary<string, object> {
            { "coins", de.coins },
            { "premium", de.premium },
            { "score", de.score },
            { "distance", de.worldDistance },
            { "obstacle",  de.obstacleType },
            { "theme", de.themeUsed },
            { "character", de.character },
        });
#endif

        PlayerData.instance.Save();

        trackManager.End();
    }

    InputActions controls;

    public void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputActions();
            controls.GameOver.SetCallbacks(this);
        }
        controls.GameOver.Enable();
    }

    public void OnDisable()
    {
        controls.GameOver.Disable();
    }

    public void OnDelete(InputAction.CallbackContext context) { }
    public void OnSpace(InputAction.CallbackContext context) { }
    public void OnShift(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }

    public void OnNavigate(InputAction.CallbackContext context) { 
        if(/*manager.ActionOf(context) && */ context.started) {

            var currentAxis = new AxisEventData(EventSystem.current);
            var currentButton = EventSystem.current.currentSelectedGameObject;

            Vector2 move = context.ReadValue<Vector2>();
            if(move.y > 0)  {
                currentAxis.moveDir = MoveDirection.Up;
                ExecuteEvents.Execute(currentButton, currentAxis, ExecuteEvents.moveHandler);
            } else if(move.y < 0) {
                currentAxis.moveDir = MoveDirection.Down;
                ExecuteEvents.Execute(currentButton, currentAxis, ExecuteEvents.moveHandler);
            } else if(move.x > 0) {
                currentAxis.moveDir = MoveDirection.Right;
                ExecuteEvents.Execute(currentButton, currentAxis, ExecuteEvents.moveHandler);
            } else if(move.x < 0) {
                currentAxis.moveDir = MoveDirection.Left;
                ExecuteEvents.Execute(currentButton, currentAxis, ExecuteEvents.moveHandler);
            }
        } 
    }

    //----------------
}
