﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    private int numMegaHits;

    public GameObject canvas;

    private int numPlayersToLevelUp;
    private int numPlayersToPlayForInstrument;
    private int numPlayersToStartLastDecisions;
    private int numPlayersToChooseDiceRollInstrument;

    private bool canSelectToCheckAlbumResult;
    private bool canCheckAlbumResult;
    private bool checkedAlbumResult;

    //------------ UI -----------------------------
    public GameObject playerUIPrefab;
    public GameObject albumUIPrefab;

    public GameObject UInewRoundScreen;
    public Button UIadvanceRoundButton;
    public Text UIalbumNameText;
    
    public GameObject UIRollDiceForInstrumentOverlay;
    public Animator rollDiceForInstrumentOverlayAnimator;
    public GameObject UIRollDiceForMarketValueScreen;

    public GameObject dice6UI;
    public GameObject dice20UI;

    public GameObject diceArrowPrefab;


    public GameObject UIAlbumCollectionDisplay;
    public GameObject UIAlbumDisplay;
    public GameObject UIPrototypeArea;


    public GameObject poppupPrefab;
    public PoppupScreenFunctionalities infoPoppupNeutralRef;
    public PoppupScreenFunctionalities infoPoppupLossRef;
    public PoppupScreenFunctionalities infoPoppupWinRef;

    public PoppupScreenFunctionalities endPoppupWinRef;
    public PoppupScreenFunctionalities endPoppupLossRef;


    private bool gameMainSceneFinished;
    private int interruptionRequests; //changed whenever an interruption occurs (either a poppup, warning, etc.)
    private bool preferredInstrumentsChoosen;

    private bool choosePreferedInstrumentResponseReceived;
    private bool playForInstrumentResponseReceived;
    private bool levelUpResponseReceived;
    private bool lastDecisionResponseReceived;

    private int currPlayerIndex;
    private int currSpeakingPlayerId;

    private Album currAlbum;

    private float diceRollDelay;

    private int marketLimit;
    private int currNumberOfMarketDices;

    void Awake()
    {
        GameGlobals.gameManager = this;
        //mock to test
        //GameGlobals.gameLogManager.InitLogs();
        //GameGlobals.albums = new List<Album>(GameProperties.numberOfAlbumsPerGame);
        //GameGlobals.players = new List<Player>(GameProperties.numberOfPlayersPerGame);
        //GameGlobals.players.Add(new RoboticPlayerCoopStrategy(0,"PL2",false));
        //GameGlobals.players.Add(new RoboticPlayerGreedyStrategy(1,"PL3",false));
        //GameGlobals.players.Add(new UIPlayer("PL1"));
        //GameGlobals.gameDiceNG = new RandomDiceNG();
        //GameGlobals.currSessionId = "0";
        //GameGlobals.currGameId = 0;
        //GameGlobals.currGameRoundId = 0;
    }

    public int InterruptGame()
    {
        Debug.Log("interrupted");
        interruptionRequests++;
        return 0;
    }
    public int ContinueGame()
    {
        Debug.Log("continued");
        interruptionRequests--;
        return 0;
    }

    public void InitGame()
    {
        interruptionRequests = 0;
        InterruptGame(); //interrupt game update while loading...

        choosePreferedInstrumentResponseReceived = false;
        playForInstrumentResponseReceived = false;
        levelUpResponseReceived = false;
        lastDecisionResponseReceived = false;
        currPlayerIndex = 0;

        
        //get player poppups (can be from any player) and set methods
        if (GameGlobals.players.Count > 0)
        {
            UIPlayer firstUIPlayer = null;
            int pIndex = 0;
            while (firstUIPlayer == null && pIndex < GameGlobals.players.Count)
            {
                firstUIPlayer = (UIPlayer) GameGlobals.players[pIndex++];
                if (firstUIPlayer != null)
                {
                    firstUIPlayer.GetWarningScreenRef().AddOnShow(InterruptGame);
                    firstUIPlayer.GetWarningScreenRef().AddOnHide(ContinueGame);
                }
            }
        }
        infoPoppupLossRef = new PoppupScreenFunctionalities(false, InterruptGame, ContinueGame, poppupPrefab,canvas, GameGlobals.monoBehaviourFunctionalities, Resources.Load<Sprite>("Textures/UI/Icons/InfoLoss"), new Color(0.9f, 0.8f, 0.8f), "Audio/albumLoss");
        infoPoppupWinRef = new PoppupScreenFunctionalities(false, InterruptGame, ContinueGame, poppupPrefab,canvas, GameGlobals.monoBehaviourFunctionalities, Resources.Load<Sprite>("Textures/UI/Icons/InfoWin"), new Color(0.9f, 0.9f, 0.8f), "Audio/albumVictory");
        infoPoppupNeutralRef = new PoppupScreenFunctionalities(false, InterruptGame, ContinueGame, poppupPrefab,canvas, GameGlobals.monoBehaviourFunctionalities, Resources.Load<Sprite>("Textures/UI/Icons/Info"), new Color(0.9f, 0.9f, 0.9f), "Audio/snap");

        //these poppups load the end scene
        endPoppupLossRef = new PoppupScreenFunctionalities(false, InterruptGame, ContinueGame, poppupPrefab, canvas, GameGlobals.monoBehaviourFunctionalities, Resources.Load<Sprite>("Textures/UI/Icons/InfoLoss"), new Color(0.9f, 0.8f, 0.8f), delegate() { /*end game*/ if (this.gameMainSceneFinished) GameSceneManager.LoadEndScene(); return 0; });
        endPoppupWinRef = new PoppupScreenFunctionalities(false, InterruptGame, ContinueGame, poppupPrefab, canvas, GameGlobals.monoBehaviourFunctionalities, Resources.Load<Sprite>("Textures/UI/Icons/InfoWin"), new Color(0.9f, 0.9f, 0.8f), delegate () { /*end game*/ if (this.gameMainSceneFinished) GameSceneManager.LoadEndScene(); return 0; });

        ChangeActivePlayerUI(((UIPlayer)(GameGlobals.players[0])), 2.0f);
        

        gameMainSceneFinished = false;
        preferredInstrumentsChoosen = false;

        //diceRollDelay = UIRollDiceForInstrumentOverlay.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        diceRollDelay = 4.0f;

        canCheckAlbumResult = false;
        checkedAlbumResult = false;
        canSelectToCheckAlbumResult = true;
        int numPlayers = GameGlobals.players.Count;

        Player currPlayer = null;
        for (int i = 0; i < numPlayers; i++)
        {
            currPlayer = GameGlobals.players[i];
            currPlayer.ReceiveGameManager(this);
            currPlayer.ReceiveTokens(1);
        }
       
        GameGlobals.currGameRoundId = 0; //first round
        numMegaHits = 0;

        marketLimit = Mathf.FloorToInt(GameProperties.configurableProperties.numberOfAlbumsPerGame * 4.0f / 5.0f) - 1;
        currNumberOfMarketDices = GameProperties.configurableProperties.initNumberMarketDices;

        rollDiceForInstrumentOverlayAnimator = UIRollDiceForInstrumentOverlay.GetComponent<Animator>();

        ContinueGame();
    }

    //warning: works only when using human players!
    private IEnumerator ChangeActivePlayerUI(UIPlayer player, float delay)
    {
        player.GetPlayerUI().transform.SetAsLastSibling();
        //yield return new WaitForSeconds(delay);
        int numPlayers = GameGlobals.players.Count;
        for (int i = 0; i < numPlayers; i++)
        {
            if (GameGlobals.players[i] == player)
            {
                player.GetPlayerMarkerUI().SetActive(true);
                player.GetPlayerDisablerUI().SetActive(true);
                continue;
            }
            UIPlayer currPlayer = (UIPlayer)GameGlobals.players[i];
            currPlayer.GetPlayerMarkerUI().SetActive(false);
            currPlayer.GetPlayerDisablerUI().SetActive(false);
        }
        return null;
    }

    void Start()
    {

        InitGame();

        numPlayersToChooseDiceRollInstrument = GameGlobals.players.Count;
        numPlayersToLevelUp = GameGlobals.players.Count;
        numPlayersToPlayForInstrument = GameGlobals.players.Count;
        numPlayersToStartLastDecisions = GameGlobals.players.Count;

        GameGlobals.currGameState = GameProperties.GameState.NOT_FINISHED;

        //players talk about the initial album
        currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);
        foreach (var player in GameGlobals.players)
        {
            player.InformNewAlbum();
        }


        if (GameProperties.configurableProperties.isSimulation) //start imidiately in simulation
        {
            StartGameRoundForAllPlayers("SimAlbum");
        }
        else
        {
            UIadvanceRoundButton.onClick.AddListener(delegate () {
                UInewRoundScreen.SetActive(false);
                StartGameRoundForAllPlayers(UIalbumNameText.text);
            });

            UIRollDiceForInstrumentOverlay.SetActive(false);
            UIRollDiceForMarketValueScreen.SetActive(false);

            Button rollDiceForMarketButton = UIRollDiceForMarketValueScreen.transform.Find("rollDiceForMarketButton").GetComponent<Button>();
            rollDiceForMarketButton.onClick.AddListener(delegate () {
                canCheckAlbumResult = true;
            });

        }
        
    }

    public void StartGameRoundForAllPlayers(string albumName)
    {
        Album newAlbum = new Album(albumName, albumUIPrefab);
        newAlbum.GetAlbumUI().SetActive(true);
        GameGlobals.albums.Add(newAlbum);
        UIDisplayAlbum(newAlbum);
        //UIAddAlbumToCollection(newAlbum);
        this.currAlbum = newAlbum;

        int numPlayers = GameGlobals.players.Count;
        for (int i = 0; i < numPlayers; i++)
        {
            Player currPlayer = GameGlobals.players[i];
            currPlayer.InitAlbumContributions();
        }

        if (!preferredInstrumentsChoosen)
        {
            StartChoosePreferredInstrumentPhase();
        }
        else
        {
            StartLevelingUpPhase();
        }
    }



    public int RollDicesForInstrument(Player currPlayer, GameProperties.Instrument instrument)
    {
        var skillSet = currPlayer.GetSkillSet();

        int newAlbumInstrumentValue = 0;
        int numTokensForInstrument = skillSet[instrument];

        //UI stuff
        UIRollDiceForInstrumentOverlay.transform.Find("title/Text").GetComponent<Text>().text = currPlayer.GetName() + " rolling "+ numTokensForInstrument + " dice for " + instrument.ToString() + " ...";

        int[] rolledDiceNumbers = new int[numTokensForInstrument]; //save each rolled dice number to display in the UI

        for (int i = 0; i < numTokensForInstrument; i++)
        {
            int randomIncrease = GameGlobals.gameDiceNG.RollTheDice(currPlayer, instrument, 6, i, numTokensForInstrument);
            rolledDiceNumbers[i] = randomIncrease;
            newAlbumInstrumentValue += randomIncrease;
        }
        if (!GameProperties.configurableProperties.isSimulation)
        {
            string arrowText = "";
            if(instrument == GameProperties.Instrument.MARKETING)
            {
                arrowText = "+" + newAlbumInstrumentValue * GameProperties.configurableProperties.marketingPointValue + " $";
            }
            else
            {
                arrowText = "+ " + newAlbumInstrumentValue + " Album Value";
            }

            StartCoroutine(PlayDiceUIs(currPlayer, newAlbumInstrumentValue, rolledDiceNumbers, 6, dice6UI, "Animations/RollDiceForInstrumentOverlay/dice6/sprites_3/endingAlternatives/", Color.yellow, arrowText, diceRollDelay));
        }

        GameGlobals.gameLogManager.WriteEventToLog(GameGlobals.currSessionId.ToString(), GameGlobals.currGameId.ToString(), GameGlobals.currGameRoundId.ToString(), currPlayer.GetId().ToString(), currPlayer.GetName().ToString(), "ROLLED_INSTRUMENT_DICES", "-", newAlbumInstrumentValue.ToString());
        return newAlbumInstrumentValue;
    }


    private IEnumerator PlayDiceUIs(Player diceThrower, int totalDicesValue, int[] rolledDiceNumbers, int diceNum, GameObject diceImagePrefab, string diceNumberSpritesPath, Color diceArrowColor, string diceArrowText, float delayToClose)
    //the sequence number aims to void dice overlaps as it represents the order for which this dice is going to be rolled. We do not want to roll a dice two times for the same place
    {
        InterruptGame();
        UIRollDiceForInstrumentOverlay.SetActive(true);
        List<GameObject> diceUIs = new List<GameObject>();

        int numDiceRolls = rolledDiceNumbers.Length;
        for (int i = 0; i < numDiceRolls; i++)
        {
            int currDiceNumber = rolledDiceNumbers[i];
            Sprite currDiceNumberSprite = Resources.Load<Sprite>(diceNumberSpritesPath + currDiceNumber);
            if (currDiceNumberSprite == null)
            {
                Debug.Log("cannot find sprite for dice number " + currDiceNumber);
            }
            else
            {
                GameObject diceUIClone = Instantiate(diceImagePrefab, UIRollDiceForInstrumentOverlay.transform);
                diceUIs.Add(diceUIClone);
                StartCoroutine(PlayDiceUI(diceUIClone, diceThrower, numDiceRolls, i, diceNum, currDiceNumberSprite, delayToClose));
            }
        }


        while (!rollDiceForInstrumentOverlayAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            yield return null;
        }
        

        rollDiceForInstrumentOverlayAnimator.speed = 0;
        
        //get and disable arrow animation until end of dice animation
        GameObject diceArrowClone = Instantiate(diceArrowPrefab, UIRollDiceForInstrumentOverlay.transform);
        diceArrowClone.GetComponentInChildren<Image>().color = diceArrowColor;
        
        Text arrowText = diceArrowClone.GetComponentInChildren<Text>();
        arrowText.text = diceArrowText;
        arrowText.color = diceArrowColor;


        yield return new WaitForSeconds(delayToClose);

        //players see the dice result
        currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);
        foreach (var player in GameGlobals.players)
        {
            player.InformRollDicesValue(diceThrower, numDiceRolls * diceNum, totalDicesValue); //max value = the max dice number * number of rolls
        }

        rollDiceForInstrumentOverlayAnimator.speed = 1;
        while (!rollDiceForInstrumentOverlayAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle2"))
        {
            yield return null;
        }

        //destroy arrows, dice images and finally set screen active to false
        Destroy(diceArrowClone);
        for(int i=0; i<diceUIs.Count; i++)
        {
            GameObject currDice = diceUIs[i];
            Destroy(currDice);
        }

        ContinueGame();
        UIRollDiceForInstrumentOverlay.SetActive(false);
    }

    private IEnumerator PlayDiceUI(GameObject diceUIClone, Player diceThrower, int numDicesInThrow, int sequenceNumber, int diceNum, Sprite currDiceNumberSprite, float delayToClose)
    //the sequence number aims to void dice overlaps as it represents the order for which this dice is going to be rolled. We do not want to roll a dice two times for the same place
    {

        Image diceImage = diceUIClone.GetComponentInChildren<Image>();
        Animator diceAnimator = diceImage.GetComponentInChildren<Animator>();

        float translationFactorX = Screen.width * 0.04f;
        float translationFactorY = Screen.width * 0.02f;
        diceUIClone.transform.Translate(new Vector3(Random.Range(-translationFactorX, translationFactorY), Random.Range(-translationFactorX, translationFactorY), 0));

        
        float diceRotation = sequenceNumber * (360.0f / numDicesInThrow);

        diceUIClone.transform.Rotate(new Vector3(0, 0, 1), diceRotation);
        diceImage.overrideSprite = null;
        diceAnimator.Rebind();
        diceAnimator.Play(0);
        diceAnimator.speed = Random.Range(0.8f,1.0f);

        while (!diceAnimator.GetCurrentAnimatorStateInfo(0).IsName("endState"))
        {
            yield return null;
        }
        diceImage.overrideSprite = currDiceNumberSprite;
        
    }

    //assuming the first player rolls the market dices
    public int RollDicesForMarketValue()
    {
        UIRollDiceForInstrumentOverlay.transform.Find("title/Text").GetComponent<Text>().text = "Rolling dices for market...";

        int marketValue = 0;
        int[] rolledDiceNumbers = new int[currNumberOfMarketDices];
        for(int i=0; i < currNumberOfMarketDices; i++)
        {
            int randomIncrease = GameGlobals.gameDiceNG.RollTheDice(this.GetCurrentPlayer(), GameProperties.Instrument.NONE, 20, i, currNumberOfMarketDices);
            rolledDiceNumbers[i] = randomIncrease;
            marketValue += randomIncrease;
        }
        GameGlobals.gameLogManager.WriteEventToLog(GameGlobals.currSessionId.ToString(), GameGlobals.currGameId.ToString(), GameGlobals.currGameRoundId.ToString(), "-", "-", "ROLLED_MARKET_DICES", "-", marketValue.ToString());

        if (!GameProperties.configurableProperties.isSimulation)
        {
            StartCoroutine(PlayDiceUIs(GameGlobals.players[0], marketValue, rolledDiceNumbers, 20, dice20UI, "Animations/RollDiceForInstrumentOverlay/dice20/sprites/endingAlternatives/", Color.red, "Market Value: " + marketValue, diceRollDelay));
        }

        return marketValue;
    }
    
    public void CheckAlbumResult()
    {
        int numAlbums = GameGlobals.albums.Count;
        int numPlayers = GameGlobals.players.Count;
        
        int newAlbumValue = currAlbum.CalcAlbumValue();
        int marketValue = RollDicesForMarketValue();
        if (newAlbumValue >= marketValue)
        {
            currAlbum.SetMarketingState(GameProperties.AlbumMarketingState.MEGA_HIT);
            numMegaHits++;
        }
        else
        {
            currAlbum.SetMarketingState(GameProperties.AlbumMarketingState.FAIL);
        }

        if (!GameProperties.configurableProperties.isSimulation)
        {
            if (newAlbumValue >= marketValue)
            {
                infoPoppupWinRef.DisplayPoppupWithDelay("As your album value (" + newAlbumValue + ") was EQUAL or HIGHER than the market value (" + marketValue + "), the album was successfully published! Congratulations! Everyone can now receive based on their own marketing skill.", diceRollDelay);
            }
            else
            {
                infoPoppupLossRef.DisplayPoppupWithDelay("As your album value (" + newAlbumValue + ") was LOWER than the market value (" + marketValue + "), the album could not be published. Everyone receives 0 $.", diceRollDelay);
            }
        }


        //players see the album result
        currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);
        foreach (var player in GameGlobals.players)
        {
            player.InformAlbumResult(newAlbumValue, marketValue);
        }


        //check for game loss (collapse) or victory on album registry
        float victoryThreshold = Mathf.Ceil(GameProperties.configurableProperties.numberOfAlbumsPerGame / 2.0f);
        float numAlbumsLeft = (float)(GameProperties.configurableProperties.numberOfAlbumsPerGame - numAlbums);
        if (numAlbumsLeft < victoryThreshold - numMegaHits)
        {
            GameGlobals.currGameState = GameProperties.GameState.LOSS;
        }
        else
        {
            if(numAlbumsLeft == 0)
            {
                GameGlobals.currGameState = GameProperties.GameState.VICTORY;
            }
        }

        this.checkedAlbumResult = true;
    }

    private void ResetAllPlayers()
    {
        foreach (Player player in GameGlobals.players)
        {
            player.ResetPlayer();
        }
    }

    // wait for all players to exit one phase and start other phase
    void Update () {
        
        //avoid rerun in this case because load scene is asyncronous
        if (this.gameMainSceneFinished || this.interruptionRequests>0)
        {
            //Debug.Log("pause...");
            return;
        }
        

        //middle of the phases
        if (choosePreferedInstrumentResponseReceived)
        {
            currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);

            choosePreferedInstrumentResponseReceived = false;
            Player currPlayer = GameGlobals.players[currPlayerIndex];
            Player nextPlayer = ChangeToNextPlayer(currPlayer);
            //if (numPlayersToChooseDiceRollInstrument > 0)
            //{
                foreach (var player in GameGlobals.players)
                {
                    if (player == currPlayer) continue;
                    player.InformChoosePreferredInstrument(nextPlayer);
                }
            //}    
            numPlayersToChooseDiceRollInstrument--;
            if (numPlayersToChooseDiceRollInstrument > 0)
            {
                nextPlayer.ChoosePreferredInstrumentRequest(currAlbum);
            }
        }
        if (levelUpResponseReceived) 
        {
            currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);

            levelUpResponseReceived = false;
            Player currPlayer = GameGlobals.players[currPlayerIndex];
            Player nextPlayer = ChangeToNextPlayer(currPlayer);
            //if (numPlayersToLevelUp > 0)
            //{
                foreach (var player in GameGlobals.players)
                {
                    if (player == currPlayer) continue;
                    player.InformLevelUp(currPlayer, currPlayer.GetLeveledUpInstrument());
                }
            //}
            numPlayersToLevelUp--;
            if (numPlayersToLevelUp > 0)
            {
                nextPlayer.LevelUpRequest(currAlbum);
            }   
        }
        if (playForInstrumentResponseReceived)
        {
            currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);

            playForInstrumentResponseReceived = false;
            Player currPlayer = GameGlobals.players[currPlayerIndex];
            Player nextPlayer = ChangeToNextPlayer(currPlayer);
            //if (numPlayersToPlayForInstrument > 0)
            //{
                foreach (var player in GameGlobals.players)
                {
                    if (player == currPlayer) continue;
                    player.InformPlayForInstrument(nextPlayer);
                }

            //}
            numPlayersToPlayForInstrument--;
            if (numPlayersToPlayForInstrument > 0)
            {
                nextPlayer.PlayForInstrumentRequest(currAlbum);
            }
        }
        if (lastDecisionResponseReceived)
        {
            currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);

            lastDecisionResponseReceived = false;
            Player currPlayer = GameGlobals.players[currPlayerIndex];
            Player nextPlayer = ChangeToNextPlayer(currPlayer);
            //if (numPlayersToStartLastDecisions > 0)
            //{
                foreach (var player in GameGlobals.players)
                {
                    if (player == currPlayer) continue;
                    player.InformLastDecision(nextPlayer);
                }
            //}
            numPlayersToStartLastDecisions--;
            if (numPlayersToStartLastDecisions > 0)
            {
                nextPlayer.LastDecisionsPhaseRequest(currAlbum);
            }
        }

        //end of first phase; trigger second phase
        if (!preferredInstrumentsChoosen && numPlayersToChooseDiceRollInstrument == 0)
        {

            //Debug.Log("running1...");
            StartPlayForInstrumentPhase(); //choose instrument phase skips level up phase
            //numPlayersToChooseDiceRollInstrument = GameGlobals.players.Count; //is not performed to ensure this phase is only played once
            preferredInstrumentsChoosen = true;
        }

        //end of second phase; trigger third phase
        if (numPlayersToLevelUp == 0)
        {
            //Debug.Log("running2...");
            StartPlayForInstrumentPhase();
            numPlayersToLevelUp = GameGlobals.players.Count;
        }
        
        //end of third phase;
        if (numPlayersToPlayForInstrument == 0)
        {
            if (checkedAlbumResult)
            {
                //Debug.Log("running3...");
                checkedAlbumResult = false;
                StartLastDecisionsPhase();
                numPlayersToPlayForInstrument = GameGlobals.players.Count;
            }
            else if(canSelectToCheckAlbumResult)
            {
                canSelectToCheckAlbumResult = false;
                if (GameProperties.configurableProperties.isSimulation) //if simulation just do it, with no loads!
                {
                    canCheckAlbumResult = true;
                }
                else
                {
                    //make phase UI active (this step is interim but must be done before last phase)
                    UIRollDiceForMarketValueScreen.SetActive(true);
                }
            }
            
            if (canCheckAlbumResult)
            {
                CheckAlbumResult();
                canCheckAlbumResult = false;
                canSelectToCheckAlbumResult = true;

                if (!GameProperties.configurableProperties.isSimulation)
                {
                    UIRollDiceForMarketValueScreen.SetActive(false);
                }
            }
            
        }

        //end of forth phase; trigger and log album result
        if (numPlayersToStartLastDecisions == 0)
        {
            //Debug.Log("running4...");
            int numPlayedAlbums = GameGlobals.albums.Count;

            //write curr game logs
            GameGlobals.gameLogManager.WriteAlbumResultsToLog(GameGlobals.currSessionId.ToString(), GameGlobals.currGameId.ToString(), GameGlobals.currGameRoundId.ToString(), currAlbum.GetId().ToString(), currAlbum.GetName(), currAlbum.GetMarketingState().ToString());
            foreach (Player player in GameGlobals.players)
            {
                GameGlobals.gameLogManager.WritePlayerResultsToLog(GameGlobals.currSessionId.ToString(), GameGlobals.currGameId.ToString(), GameGlobals.currGameRoundId.ToString(), player.GetId().ToString(), player.GetName(), player.GetMoney().ToString());
            }

            numPlayersToStartLastDecisions = GameGlobals.players.Count;
            GameGlobals.currGameRoundId++;

            //start next game round whenever ready, but only if game hasn't finished
            if(GameGlobals.currGameState == GameProperties.GameState.NOT_FINISHED)
            {
                if (!GameProperties.configurableProperties.isSimulation)
                {
                    UIAddAlbumToCollection(currAlbum);
                    UInewRoundScreen.SetActive(true);
                }
                else
                {
                    StartGameRoundForAllPlayers("SimAlbum");
                }
            
                if (GameGlobals.albums.Count < GameProperties.configurableProperties.numberOfAlbumsPerGame)
                {
                    currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);
                    foreach (var player in GameGlobals.players)
                    {
                        player.InformNewAlbum();
                    }
                }
            }


            //enter international market on the next album, increase the number of dices played for market
            if (GameGlobals.currGameRoundId == marketLimit)
            {
                int oldNumberOfMarketDices = currNumberOfMarketDices;
                currNumberOfMarketDices++;

                //poppups are not displayed on simulations
                if (!GameProperties.configurableProperties.isSimulation)
                {
                    infoPoppupNeutralRef.DisplayPoppup("You gained some experience publishing your last albums and so you will try your luck on the international market. From now on, "+ currNumberOfMarketDices +" dices (instead of "+ oldNumberOfMarketDices + ") are rolled for the market.");
                }
            }


            //reinit some things for next game if game result is known or max albums are achieved
            if (GameGlobals.currGameState != GameProperties.GameState.NOT_FINISHED)
            {
                GameGlobals.currGameRoundId = 0;
                //GameGlobals.currGameState = GameProperties.GameState.NOT_FINISHED;
                Debug.Log("GameGlobals.currGameState: " + GameGlobals.currGameState);

                //move albums to root so they can be saved through scenes
                foreach (Album album in GameGlobals.albums)
                {
                    UIRemoveAlbumFromCollection(album);
                    album.GetAlbumUI().SetActive(false); //do not show albums before final scene
                    Object.DontDestroyOnLoad(album.GetAlbumUI()); //can only be made after getting the object on root
                }

                if(GameGlobals.currGameState == GameProperties.GameState.LOSS)
                {
                    foreach(Player player in GameGlobals.players)
                    {
                        player.TakeAllMoney();
                    }

                    if (!GameProperties.configurableProperties.isSimulation)
                    {
                        endPoppupLossRef.DisplayPoppup("The band incurred in too much debt! No more albums can be produced!");
                    }
                }
                else
                {
                    if (!GameProperties.configurableProperties.isSimulation)
                    {
                        endPoppupWinRef.DisplayPoppup("The band had a successful journey! Congratulations!");
                    }
                }


                //players see the game result
                currSpeakingPlayerId = Random.Range(0, GameGlobals.numberOfSpeakingPlayers);
                foreach (Player player in GameGlobals.players)
                {
                    player.InformGameResult(GameGlobals.currGameState);
                }


                this.gameMainSceneFinished = true;
                if (!GameProperties.configurableProperties.isSimulation)
                {
                    UIadvanceRoundButton.gameObject.SetActive(false);
                    UIPrototypeArea.gameObject.SetActive(false);
                }
                else
                {
                    GameSceneManager.LoadEndScene();
                }
            }

        }

    }


    public void StartChoosePreferredInstrumentPhase()
    {
        ResetAllPlayers();
        int numPlayers = GameGlobals.players.Count;
        GameGlobals.players[0].ChoosePreferredInstrumentRequest(currAlbum);
    }
    public void StartLevelingUpPhase()
    {
        ResetAllPlayers();
        int numPlayers = GameGlobals.players.Count;
        GameGlobals.players[0].LevelUpRequest(currAlbum);
    }
    public void StartPlayForInstrumentPhase()
    {
        ResetAllPlayers();
        int numPlayers = GameGlobals.players.Count;
        GameGlobals.players[0].PlayForInstrumentRequest(currAlbum);
    }
    public void StartLastDecisionsPhase()
    {
        ResetAllPlayers();
        Album currAlbum = GameGlobals.albums[GameGlobals.albums.Count - 1];
        int numPlayers = GameGlobals.players.Count;
        GameGlobals.players[0].LastDecisionsPhaseRequest(currAlbum);
    }


    //------------------------------------------Responses---------------------------------------
    public void ChoosePreferredInstrumentResponse(Player invoker)
    {
        //auto level up after choosing instrument
        invoker.SpendToken(invoker.GetPreferredInstrument());
        invoker.BuyTokens(1);
        invoker.SpendToken(GameProperties.Instrument.MARKETING);
        choosePreferedInstrumentResponseReceived = true;
    }
    public void LevelUpResponse(Player invoker)
    {   
        levelUpResponseReceived = true;
    }
    public void PlayerPlayForInstrumentResponse(Player invoker)
    {
        GameProperties.Instrument rollDiceInstrument = invoker.GetDiceRollInstrument();
        if (rollDiceInstrument != GameProperties.Instrument.NONE) //if there is a roll dice instrument
        {
            int newAlbumInstrumentValue = RollDicesForInstrument(invoker, rollDiceInstrument);
            invoker.SetAlbumContribution(rollDiceInstrument, newAlbumInstrumentValue);
            currAlbum.SetInstrumentValue(invoker.GetDiceRollInstrument(), newAlbumInstrumentValue);
        }
        playForInstrumentResponseReceived = true;
    }
    public void LastDecisionsPhaseGet0Response(Player invoker)
    {
        //receive 0
        invoker.ReceiveMoney(0);
        lastDecisionResponseReceived = true;
    }
    public void LastDecisionsPhaseGet3000Response(Player invoker)
    {
        //receive 3000
        invoker.ReceiveMoney(3000);
        lastDecisionResponseReceived = true;
    }
    public void LastDecisionsPhaseGetMarketingResponse(Player invoker)
    {
        //roll dices for marketing
        int marketingValue = RollDicesForInstrument(invoker, GameProperties.Instrument.MARKETING);
        invoker.SetAlbumContribution(GameProperties.Instrument.MARKETING, marketingValue);
        invoker.ReceiveMoney(GameProperties.configurableProperties.marketingPointValue * marketingValue);

        lastDecisionResponseReceived = true;
    }


    public Player ChangeToNextPlayer(Player currPlayer)
    {
        currPlayerIndex = (currPlayerIndex + 1) % GameGlobals.players.Count;
        Player nextPlayer = GameGlobals.players[currPlayerIndex];
        ChangeActivePlayerUI((UIPlayer) nextPlayer, 2.0f);
        return nextPlayer;
    }

    public void UIDisplayAlbum(Album albumToDisplay)
    {
        GameObject currAlbumUI = albumToDisplay.GetAlbumUI();
        currAlbumUI.transform.SetParent(UIAlbumDisplay.transform);
        currAlbumUI.transform.localPosition = new Vector3(0, 0, 0);
        currAlbumUI.transform.localScale = new Vector3(1, 1, 1);
    }
    public void UIAddAlbumToCollection(Album albumToAdd)
    {
        int albumsSize = GameGlobals.albums.Count;
        GameObject currAlbumUI = albumToAdd.GetAlbumUI();

        Animator animator = currAlbumUI.GetComponentInChildren<Animator>();
        animator.Rebind();
        animator.Play(0);

        currAlbumUI.transform.SetParent(UIAlbumCollectionDisplay.transform);
        currAlbumUI.transform.localPosition = new Vector3(0, 0, 0);
        currAlbumUI.transform.localScale = new Vector3(1, 1, 1);

        currAlbumUI.transform.Translate(new Vector3(albumsSize * Screen.width * 0.03f, 0, 0));
    }
    public void UIRemoveAlbumFromCollection(Album albumToRemove)
    {
        GameObject currAlbumUI = albumToRemove.GetAlbumUI();
        currAlbumUI.transform.SetParent(null);
    }


    public Player GetCurrentPlayer()
    {
        return GameGlobals.players[this.currPlayerIndex];
    }
    public int GetCurrSpeakingPlayerId()
    {
        return this.currSpeakingPlayerId;
    }

}
