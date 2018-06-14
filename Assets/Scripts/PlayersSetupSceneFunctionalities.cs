﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayersSetupSceneFunctionalities : MonoBehaviour {

    private Text UINameSelectionInputBox;
    private Button UIStartGameButton;
    private Button UIAddPlayerButton;

    void Start ()
    {
        if (!GameProperties.isSimulation)
        {
            this.UINameSelectionInputBox = GameObject.Find("Canvas/SetupScreen/nameSelectionInputBox/Text").gameObject.GetComponent<Text>();
            this.UIStartGameButton = GameObject.Find("Canvas/SetupScreen/startGameButton").gameObject.GetComponent<Button>();
            this.UIAddPlayerButton = GameObject.Find("Canvas/SetupScreen/addPlayerGameButton").gameObject.GetComponent<Button>();

            UIStartGameButton.gameObject.SetActive(false);


            UIStartGameButton.onClick.AddListener(delegate { StartGame(); });
            UIAddPlayerButton.onClick.AddListener(delegate
            {
                AddUIPlayer(UINameSelectionInputBox.text);
                Debug.Log(GameGlobals.players.Count);
                if (GameGlobals.players.Count == GameProperties.numberOfPlayersPerGame)
                {
                    UIStartGameButton.gameObject.SetActive(true);
                    UIAddPlayerButton.gameObject.SetActive(false);
                }
            });
        }
        else
        {
            AddSimPlayer("PL1");
            AddSimPlayer("PL2");
            AddSimPlayer("PL3");
            StartGame();
        }
    }
	
	void StartGame()
    {
        GameSceneManager.LoadMainScene();
    }

    void AddUIPlayer(string name)
    {
        GameGlobals.players.Add(new UIPlayer(name));
    }

    void AddSimPlayer(string name)
    {
        GameGlobals.players.Add(new SimPlayer(name));
    }
}
