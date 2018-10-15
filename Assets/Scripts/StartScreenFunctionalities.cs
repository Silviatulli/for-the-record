﻿using AssetManagerPackage;
using FAtiMAScripts;

using IntegratedAuthoringTool;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenFunctionalities : MonoBehaviour {

    private StreamReader fileReader;

    private Button UIStartGameButton;
    public GameObject UIGameCodeDisplayPrefab;
    public GameObject monoBehaviourDummyPrefab;

    private void InitGameGlobals()
    {

        //Assign configurable game properties from file
        DynamicallyConfigurableGameProperties configs = JsonUtility.FromJson<DynamicallyConfigurableGameProperties>(File.ReadAllText(Application.streamingAssetsPath + "/config.cfg"));
        GameProperties.configurableProperties = configs;

        GameObject monoBehaviourDummy = Instantiate(monoBehaviourDummyPrefab);
        DontDestroyOnLoad(monoBehaviourDummy);
        GameGlobals.monoBehaviourFunctionalities = monoBehaviourDummy.GetComponent<MonoBehaviourFunctionalities>();

        GameGlobals.numberOfSpeakingPlayers = 0;
        GameGlobals.currGameId++;
        GameGlobals.currGameRoundId = 0;
        GameGlobals.albumIdCount = 0;

        GameGlobals.gameLogManager = new MySQLLogManager();
        GameGlobals.audioManager = new AudioManager();


        GameGlobals.gameLogManager.InitLogs();
        //GameGlobals.playerIdCount = 0;
        //GameGlobals.albumIdCount = 0;

        GameGlobals.albums = new List<Album>(GameProperties.configurableProperties.numberOfAlbumsPerGame);
        
        //destroy UIs if any
        if (GameGlobals.players!=null && GameGlobals.players.Count > 0)
        {
            UIPlayer firstUIPlayer = null;
            int pIndex = 0;
            while (firstUIPlayer == null && pIndex < GameGlobals.players.Count)
            {
                firstUIPlayer = (UIPlayer)GameGlobals.players[pIndex++];
                if (firstUIPlayer != null)
                {
                    firstUIPlayer.GetWarningScreenRef().DestroyPoppupPanel();
                    Destroy(firstUIPlayer.GetPlayerCanvas());
                }
            }

        }
        GameGlobals.players = new List<Player>(GameProperties.configurableProperties.numberOfPlayersPerGame);


        //only generate session data in the first game
        if (GameGlobals.currGameId == 1)
        {
            string date = System.DateTime.Now.ToString("ddHHmm");

            //generate external game code from currsessionid and lock it in place
            //gamecode is in the format ddmmhhmmss<3RandomLetters>[TestGameCondition]

            string generatedCode = date; //sb.ToString();
            
            //generate 3 random letters
            for (int i = 0; i < 3; i++)
            {
                generatedCode += (char)('A' + Random.Range(0, 26));
            }

            if (GameProperties.configurableProperties.isAutomaticalBriefing) //generate condition automatically (asynchronous)
            {
                GameGlobals.gameLogManager.GetLastSessionConditionFromLog(AppendConditionToGameCode);
            }
            else{
                this.UIStartGameButton.interactable = true;
            }

            GameGlobals.currSessionId = generatedCode;

            //update the gamecode UI
            //GameObject UIGameCodeDisplay = Object.Instantiate(UIGameCodeDisplayPrefab);
            //UIGameCodeDisplay.GetComponentInChildren<Text>().text = "Game Code: " + GameGlobals.currSessionId;
            //Object.DontDestroyOnLoad(UIGameCodeDisplay);
        }

        //init fatima strings
        GameGlobals.FAtiMAScenarioPath = "/Scenarios/ForTheRecord.iat";

        AssetManager.Instance.Bridge = new AssetManagerBridge();
        GameGlobals.FAtiMAIat = IntegratedAuthoringToolAsset.LoadFromFile(GameGlobals.FAtiMAScenarioPath);
    }

    private int AppendConditionToGameCode()
    {
        string lastConditionString = ((MySQLLogManager) GameGlobals.gameLogManager).phpConnection.text;
        char lastCondition = (lastConditionString == "") ? 'A' : lastConditionString.ToString()[0];
        GameProperties.testGameParameterization = (char)('A' + ((lastCondition - 'A') + 1) % GameProperties.configurableProperties.possibleConditions);
        GameGlobals.currSessionId += GameProperties.testGameParameterization;
        if(!GameProperties.configurableProperties.isSimulation) this.UIStartGameButton.interactable = true;
        return 0;
    }


    private void StartGame()
    {
        GameSceneManager.LoadPlayersSetupScene();
    }

	void Start()
    {

        // Make the game perform as good as possible
        Application.targetFrameRate = 40;

        this.UIStartGameButton = GameObject.Find("Canvas/StartScreen/startGameButton").gameObject.GetComponent<Button>();
        this.UIStartGameButton.interactable = false;
        
        //play theme song
        //GameGlobals.audioManager.PlayInfinitClip("Audio/theme/themeIntro", "Audio/theme/themeLoop");
        UIStartGameButton.onClick.AddListener(delegate () { StartGame(); });


        InitGameGlobals();

        if (!GameProperties.configurableProperties.isSimulation)
        {
            if (GameProperties.configurableProperties.isAutomaticalBriefing)
            {
                Text startButtonText = UIStartGameButton.GetComponentInChildren<Text>();
                if (GameGlobals.currGameId < (GameProperties.configurableProperties.numTutorialGamesToPlay+1))
                {
                    startButtonText.text = "Start Tutorial Game";
                }
                else
                {
                    startButtonText.text = "Start Experiment Game";
                }
            }
        }
        else
        {
            StartGame();
        }
	}
}
