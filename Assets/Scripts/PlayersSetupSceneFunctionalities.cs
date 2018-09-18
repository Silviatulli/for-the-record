﻿using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class PlayersSetupSceneFunctionalities : MonoBehaviour {

    private GameObject customizeLabel;

    private InputField UINameSelectionInputBox;
    private Button UIStartGameButton;
    private Button UIAddPlayerButton;
    private Button UIResetButton;

    private GameObject UIAIPlayerSelectionButtonsObject;
    private GameObject configSelectionButtonsObject;

    
    public GameObject poppupPrefab;
    public GameObject playerUIPrefab;
    public GameObject playerCanvas;
    private PoppupScreenFunctionalities playerWarningPoppupRef;



    void ConfigureAllHumanPlayers()
    {
        GameGlobals.players.Add(new UIPlayer(playerUIPrefab,playerCanvas, playerWarningPoppupRef, 0,"Player1"));
        GameGlobals.players.Add(new UIPlayer(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 1, "Player2"));
        GameGlobals.players.Add(new UIPlayer(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 2,"Player3"));
        GameGlobals.gameDiceNG = new RandomDiceNG();
    }
    void ConfigureRandomTestWithRobots()
    {
        GameGlobals.numberOfSpeakingPlayers = 2;
        GameGlobals.players.Add(new AIPlayerBalancedStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 0, "Emys", GameProperties.isSpeechAllowed));
        GameGlobals.players.Add(new AIPlayerBalancedStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 1, "Glin", GameProperties.isSpeechAllowed));
        GameGlobals.players.Add(new UIPlayer(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 2, "Player"));
        GameGlobals.gameDiceNG = new RandomDiceNG();
    }
    void ConfigureConditionA()
    {
        GameGlobals.numberOfSpeakingPlayers = 2;
        AIPlayerGreedyStrategy emys = new AIPlayerGreedyStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 0, "Emys", GameProperties.isSpeechAllowed);
        GameGlobals.players.Add(emys);
        AIPlayerCoopStrategy glin = new AIPlayerCoopStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 1, "Glin", GameProperties.isSpeechAllowed);
        GameGlobals.players.Add(glin);
        GameGlobals.players.Add(new UIPlayer(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 2,"Player"));
        GameGlobals.gameDiceNG = new VictoryDiceNG();
        emys.FlushRobotUtterance("<gaze(Player)> Eu sou o émys!");
        Thread.Sleep(1000);
        glin.FlushRobotUtterance("<gaze(Player)> E eu sou o Glin! Vamos lá formar uma banda e ver se conseguimos triunfar!");
    }
    void ConfigureConditionB()
    {
        GameGlobals.numberOfSpeakingPlayers = 2;
        AIPlayerGreedyStrategy emys = new AIPlayerGreedyStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 0, "Emys", GameProperties.isSpeechAllowed);
        GameGlobals.players.Add(emys);
        AIPlayerCoopStrategy glin = new AIPlayerCoopStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, 1, "Glin", GameProperties.isSpeechAllowed);
        GameGlobals.players.Add(glin);
        GameGlobals.players.Add(new UIPlayer(2,"Player"));
        GameGlobals.gameDiceNG = new LossDiceNG();
        emys.FlushRobotUtterance("<gaze(Player)> Eu sou o émys!");
        Thread.Sleep(1000);
        glin.FlushRobotUtterance("<gaze(Player)> E eu sou o Glin! Vamos lá formar uma banda e ver se conseguimos triunfar!");
    }

    void Start ()
    {
        if (!GameProperties.isSimulation)
        {
            Object.DontDestroyOnLoad(playerCanvas);
            this.playerWarningPoppupRef = new PoppupScreenFunctionalities(null,null, poppupPrefab, playerCanvas, this.GetComponent<PlayerMonoBehaviourFunctionalities>(), Resources.Load<Sprite>("Textures/UI/Icons/Warning"), new Color(0.9f, 0.8f, 0.8f), "Audio/snap");

            if (GameProperties.isAutomaticalBriefing)
            {
                if (GameGlobals.currGameId == 1) //gameId starts in 1, 1 is the first game (tutorial)
                {
                    ConfigureRandomTestWithRobots();
                }
                else if (GameGlobals.currGameId == 2)
                {
                    string gameCode = GameGlobals.currSessionId;
                    int lastGameCodeLetterASCII = gameCode[gameCode.Length - 1];
                    int middleOfletters = 'A' + 13;
                    if (lastGameCodeLetterASCII > middleOfletters)
                    {
                        ConfigureConditionA();
                    }
                    else
                    {
                        ConfigureConditionB();
                    }
                }
                StartGame();
                return;
            }

            this.customizeLabel = GameObject.Find("Canvas/SetupScreen/customizeLabel").gameObject;

            this.UIResetButton = GameObject.Find("Canvas/SetupScreen/resetButton").gameObject.GetComponent<Button>();
            this.UINameSelectionInputBox = GameObject.Find("Canvas/SetupScreen/nameSelectionInputBox").gameObject.GetComponent<InputField>();
            this.UIStartGameButton = GameObject.Find("Canvas/SetupScreen/startGameButton").gameObject.GetComponent<Button>();
            this.UIAddPlayerButton = GameObject.Find("Canvas/SetupScreen/addPlayerGameButton").gameObject.GetComponent<Button>();

            this.UIAIPlayerSelectionButtonsObject = GameObject.Find("Canvas/SetupScreen/addAIPlayerGameButtons").gameObject;
            Button[] UIAIPlayerSelectionButtons= UIAIPlayerSelectionButtonsObject.GetComponentsInChildren<Button>();

            this.configSelectionButtonsObject = GameObject.Find("Canvas/SetupScreen/configButtons").gameObject;
            Button[] UIConfigButtons = configSelectionButtonsObject.GetComponentsInChildren<Button>();

            UIResetButton.onClick.AddListener(delegate {
                GameGlobals.players.Clear();
                foreach (Button button in UIAIPlayerSelectionButtons)
                {
                    button.interactable = true;
                }
            });


            UIStartGameButton.gameObject.SetActive(false);

            UIStartGameButton.onClick.AddListener(delegate { StartGame(); });
            UIAddPlayerButton.onClick.AddListener(delegate {
                GameGlobals.players.Add(new UIPlayer(playerUIPrefab, playerCanvas, playerWarningPoppupRef, GameGlobals.players.Count,UINameSelectionInputBox.text));
                CheckForAllPlayersRegistered();
            });

            for(int i=0; i < UIAIPlayerSelectionButtons.Length; i++)
            {
                Button button = UIAIPlayerSelectionButtons[i];
                button.onClick.AddListener(delegate
                {
                    int index = new List<Button>(UIAIPlayerSelectionButtons).IndexOf(button);
                    UIPlayer newPlayer = new UIPlayer(0,"");
                    switch ((GameProperties.AIPlayerType) (index+1))
                    {
                        case GameProperties.AIPlayerType.SIMPLE:
                            newPlayer = new AIPlayerSimple(playerUIPrefab, playerCanvas, playerWarningPoppupRef, GameGlobals.players.Count,"John0", GameProperties.isSpeechAllowed);
                            break;
                        case GameProperties.AIPlayerType.COOPERATIVE:
                            newPlayer = new AIPlayerCoopStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, GameGlobals.players.Count,"John1", GameProperties.isSpeechAllowed);
                            break;
                        case GameProperties.AIPlayerType.GREEDY:
                            newPlayer = new AIPlayerGreedyStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, GameGlobals.players.Count,"John2", GameProperties.isSpeechAllowed);
                            break;
                        case GameProperties.AIPlayerType.BALANCED:
                            newPlayer = new AIPlayerBalancedStrategy(playerUIPrefab, playerCanvas, playerWarningPoppupRef, GameGlobals.players.Count,"John3", GameProperties.isSpeechAllowed);
                            break;
                    }
                    GameGlobals.players.Add(newPlayer);
                    button.interactable = false;
                    CheckForAllPlayersRegistered();
                });
            }

            for (int i = 0; i < UIConfigButtons.Length; i++)
            {
                Button button = UIConfigButtons[i];
                button.onClick.AddListener(delegate
                {
                    if (button.gameObject.name.EndsWith("1"))
                    {
                        ConfigureAllHumanPlayers();
                    }
                    else if (button.gameObject.name.EndsWith("2"))
                    {
                        ConfigureRandomTestWithRobots();
                    }
                    else if (button.gameObject.name.EndsWith("3"))
                    {
                        ConfigureConditionA();
                    }
                    else if (button.gameObject.name.EndsWith("4"))
                    {
                        ConfigureConditionB();
                    }
                    button.interactable = false;
                    CheckForAllPlayersRegistered();
                });
            }

        }
        else
        {
            GameGlobals.players.Add(new AIPlayerGreedyStrategy(GameGlobals.players.Count,"PL1"));
            GameGlobals.players.Add(new AIPlayerBalancedStrategy(GameGlobals.players.Count,"PL2"));
            GameGlobals.players.Add(new AIPlayerCoopStrategy(GameGlobals.players.Count,"PL3"));
            GameGlobals.gameDiceNG = new RandomDiceNG();
            StartGame();
        }
    }
	
	void StartGame()
    {
        GameSceneManager.LoadMainScene();
    }

    void CheckForAllPlayersRegistered()
    {
        UINameSelectionInputBox.text = "";
        if (GameGlobals.players.Count == GameProperties.numberOfPlayersPerGame)
        {
            UIStartGameButton.gameObject.SetActive(true);
            customizeLabel.gameObject.SetActive(false);
            UIAddPlayerButton.gameObject.SetActive(false);
            UINameSelectionInputBox.gameObject.SetActive(false);

            UIAIPlayerSelectionButtonsObject.SetActive(false);
            configSelectionButtonsObject.SetActive(false);
            UIResetButton.gameObject.SetActive(false);
        }
    }
    
}
