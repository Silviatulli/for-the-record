﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using IntegratedAuthoringTool;
using RolePlayCharacter;
using WellFormedNames;
using System.Threading;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class EmotionalModule : MonoBehaviour
{
    private ThalamusConnector thalamusConnector = null;

    private IntegratedAuthoringToolAsset iat;
    private RolePlayCharacterAsset rpc;
    private Thread rpcThread;
    private bool isStopped;

    public int DicesValue { get; internal set; }
    public int NumDices { get; internal set; }


    public bool Speaks { get; internal set; }
    private UIPlayer invoker; //when no speech the object is passed so that text is displayed
    private GameObject speechBalloon;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        isStopped = false;
        iat = IntegratedAuthoringToolAsset.LoadFromFile(Application.dataPath + "/.." + GameGlobals.FAtiMAScenarioPath);
        rpc = RolePlayCharacterAsset.LoadFromFile(iat.GetAllCharacterSources().FirstOrDefault().Source);
        rpc.LoadAssociatedAssets();
        iat.BindToRegistry(rpc.DynamicPropertiesRegistry);
        rpcThread = new Thread(UpdateCoroutine);
        rpcThread.Start();

    }

    public void ReceiveInvoker(UIPlayer invoker)
    {
        this.invoker = invoker;
        speechBalloon = invoker.GetSpeechBaloonUI();
    }

    public void Perceive(Name[] events)
    {
        rpc.Perceive(events);
    }

    public IEnumerator DisplaySpeechBalloonForAWhile(string message, float delay)
    {
        speechBalloon.GetComponentInChildren<Text>().text = message;
        speechBalloon.SetActive(true);
        yield return new WaitForSeconds(delay);
        speechBalloon.SetActive(false);
    }

    public string StripSpeechSentence(string rawMessage)
    {
        var strippedDialog = rawMessage;
        strippedDialog = this.name + strippedDialog;
        strippedDialog = strippedDialog.Replace("|dicesValue|", DicesValue.ToString());
        strippedDialog = strippedDialog.Replace("|numDices|", NumDices.ToString());
        strippedDialog = Regex.Replace(strippedDialog, "<.>", "");
        return strippedDialog;
    }

    public void Decide()
    {
        IEnumerable<ActionLibrary.IAction> possibleActions = rpc.Decide();
        ActionLibrary.IAction chosenAction = possibleActions.FirstOrDefault();


        if (chosenAction == null)
        {
            Console.WriteLine("No action");
            //saveToFile();
            return;
        }
        else
        {
            //saveToFile();


            switch (chosenAction.Key.ToString())
            {
                case "Speak":

                    Name currentState = chosenAction.Parameters[0];
                    Name nextState = chosenAction.Parameters[1];
                    Name meaning = chosenAction.Parameters[2];
                    Name style = chosenAction.Parameters[3];

                    var possibleDialogs = iat.GetDialogueActions(currentState, nextState, meaning, style);
                    int randomUttIndex = UnityEngine.Random.Range(0, possibleDialogs.Count());
                    var dialog = possibleDialogs[randomUttIndex].Utterance;

                    StartCoroutine(DisplaySpeechBalloonForAWhile(StripSpeechSentence(dialog), 2.0f));


                    if (Speaks && thalamusConnector != null)
                    {
                        if (currentState.ToString() == "SelfRollInstrumentDice")
                        {
                            string[] tags = new string[] { "|dicesValue|" };
                            string[] values = new string[] { DicesValue.ToString() };
                            if (DicesValue == 1)
                            {
                                values = new string[] { "" };
                            }
                            thalamusConnector.PerformUtterance(dialog, tags, values);
                        }
                        else if (currentState.ToString() == "PlayForInstrument" || currentState.ToString() == "LastDecisionsPhase")
                        {
                            string[] tags = new string[] { "|numDices|" };
                            string[] values = new string[] { NumDices.ToString() };
                            if (NumDices == 1)
                            {
                                values = new string[] { "" };
                            }
                            thalamusConnector.PerformUtterance(dialog, tags, values);
                        }
                        else
                        {
                            thalamusConnector.PerformUtterance(dialog, new string[] { }, new string[] { });
                        }
                        Debug.Log(name + " is performing " + dialog);
                    }
                    else
                    {
                        Debug.Log("ERROR: ThalamusConnector not defined yet.");
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void UpdateCoroutine()
    {
        string currentBelief = rpc.GetBeliefValue("State(Game)");

        while (currentBelief != "Game(End)" && !isStopped)
        {
            rpc.Update();
            Thread.Sleep(100);
        }
    }

    void OnDestroy()
    {
        if (!isStopped)
        {
            rpcThread.Abort();
            isStopped = true;
        }
    }

    void OnApplicationQuit()
    {
        if (!isStopped)
        {
            rpcThread.Abort();
            isStopped = true;
        }
    }

    public void FlushUtterance(string text)
    {
        StartCoroutine(DisplaySpeechBalloonForAWhile(StripSpeechSentence(text), 2.0f));
    }

    public void GazeAt(string target)
    {
        if (thalamusConnector != null)
        {
            thalamusConnector.GazeAt(target);
        }
        else
        {
            Debug.Log("agent did not gaze.");
        }
    }
}



//public class RoboticPlayerGreedyStrategy : AIPlayerGreedyStrategy
//{
//    private EmotionalRoboticPlayer robot;
//    private bool playedForInstrument;

//    public RoboticPlayerGreedyStrategy(int id, string name, bool speaks) : base(name)
//    {
//        this.id = id;
//        playedForInstrument = false;
//        GameObject erp = new GameObject("EmotionalRoboticPlayer");
//        robot = erp.AddComponent<EmotionalRoboticPlayer>();
//        robot.InitThalamusConnectorOnPort(7002, name);
//        robot.Speaks = speaks;
//        robot.ReceiveInvoker(this);
//    }

//public void FlushRobotUtterance(string text)
//{
//    robot.FlushRobotUtterance(text);
//}

//    public override void InitUI(GameObject playerUIPrefab, GameObject canvas, PoppupScreenFunctionalities warningScreenRef)
//    {
//        base.InitUI(playerUIPrefab, canvas, warningScreenRef);
//        robot.ReceiveInvoker(this); //only pass the invoker after it is initialized

//    }

//    public override void ChoosePreferredInstrument(Album currAlbum)
//    {
//        robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("Character(Name)", name, name),
//            EventHelper.PropertyChange("Album(Last)", "False", name),
//            EventHelper.PropertyChange("State(Game)", "ChoosePreferredInstrument", name) });
//        base.ChoosePreferredInstrument(currAlbum);
//        robot.Decide();
//    }
//    protected override void ChoosePreferredInstrumentActions(Album currAlbum)
//    {
//        GameProperties.Instrument preferredIntrument = GameProperties.Instrument.VOCALS;

//        foreach (Player player in GameGlobals.players)
//        {
//            if (player == this)
//            {
//                continue;
//            }
//            if (player.GetPreferredInstrument() == preferredIntrument)
//            {
//                base.ChoosePreferredInstrumentActions(currAlbum);
//                return;
//            }
//        }
//        ChangePreferredInstrument(preferredIntrument);
//    }

//    public override void InformChoosePreferredInstrument(Player nextPlayer)
//    {
//        if (nextPlayer.GetName() != name)
//        {
//            robot.GazeAt(nextPlayer.GetName());
//        }
//    }

//    public override void InformPlayForInstrument(Player nextPlayer)
//    {
//        if (nextPlayer.GetName() != name)
//        {
//            robot.GazeAt(nextPlayer.GetName());
//        }
//    }

//    public override void InformLastDecision(Player nextPlayer)
//    {
//        if (nextPlayer.GetName() != name)
//        {
//            robot.GazeAt(nextPlayer.GetName());
//        }
//    }

//    public override void InformLevelUp()
//    {
//        Player currentPlayer = gameManagerRef.GetCurrentPlayer();
//        int currSpeakingPlayerId = gameManagerRef.GetCurrSpeakingPlayerId();

//        if (currSpeakingPlayerId == id && currentPlayer.GetName() == "Player")
//        {
//            Debug.Log(name + ": É a vez do " + currentPlayer.GetName());
//            robot.Perceive(new Name[] {
//                EventHelper.PropertyChange("CurrentPlayer(Name)", currentPlayer.GetName(), name),
//                EventHelper.PropertyChange("State(Game)", "LevelUp", name) });
//            robot.Decide();
//        }
//        else if (currentPlayer != this)
//        {
//            robot.GazeAt(currentPlayer.GetName());
//        }
//    }

//    public override void LevelUp(Album currAlbum)
//    {
//        base.LevelUp(currAlbum);
//        robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("CurrentPlayer(Name)", name, name),
//            EventHelper.PropertyChange("State(Game)", "LevelUp", name) });
//        robot.Decide();
//    }

//    public override void PlayForInstrument(Album currAlbum)
//    {
//        robot.NumDices = skillSet[preferredInstrument];
//        Debug.Log(name + " numDices is  " + skillSet[preferredInstrument]);
//        robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "PlayForInstrument", name) });
//        base.PlayForInstrument(currAlbum);
//        robot.Decide();
//    }

//    public override void LastDecisionsPhase(Album currAlbum)
//    {
//        base.LastDecisionsPhase(currAlbum);

//        Debug.Log(name + " num of albums " + GameGlobals.albums.Count);

//        if (currAlbum.GetMarketingState() == GameProperties.AlbumMarketingState.MEGA_HIT)
//        {
//            robot.Perceive(new Name[] {
//                EventHelper.PropertyChange("State(Game)", "LastDecisionsPhase", name),
//                EventHelper.PropertyChange("Album(Result)", "Success", name) });
//        }
//        else
//        {
//            robot.Perceive(new Name[] {
//                EventHelper.PropertyChange("State(Game)", "LastDecisionsPhase", name),
//                EventHelper.PropertyChange("Album(Result)", "Fail", name) });
//        }

//        if (GameGlobals.albums.Count == GameProperties.numberOfAlbumsPerGame)
//        {
//            robot.Perceive(new Name[] {
//                EventHelper.PropertyChange("Album(Last)", "True", name) });
//        }
//        robot.Decide();
//    }

//    protected override void InformRollDicesValueActions(Player invoker, int maxValue, int obtainedValue)
//    {
//        robot.GazeAt("screen");

//        // rolling d6 dice(s)
//        if (maxValue % 20 != 0)
//        {
//            int currSpeakingPlayerId = gameManagerRef.GetCurrSpeakingPlayerId();
//            if (invoker == this && !playedForInstrument)
//            {
//                playedForInstrument = true;
//                robot.DicesValue = obtainedValue;
//                float luckFactor = (float)obtainedValue / (float)maxValue;

//                if (luckFactor >= 0.5)
//                {
//                    robot.Perceive(new Name[] {
//                        EventHelper.PropertyChange("State(Game)", "SelfRollInstrumentDice", name),
//                        EventHelper.PropertyChange("Roll(InstrumentDice)", "Luck", invoker.GetName()) });
//                }
//                else
//                {
//                    robot.Perceive(new Name[] {
//                        EventHelper.PropertyChange("State(Game)", "SelfRollInstrumentDice", name),
//                        EventHelper.PropertyChange("Roll(InstrumentDice)", "BadLuck", invoker.GetName()) });
//                }
//                robot.Decide();
//            }
//            else if (currSpeakingPlayerId == id && invoker.GetName() == "Player")
//            {

//                float luckFactor = (float)obtainedValue / (float)maxValue;

//                if (luckFactor > 0.7)
//                {
//                    robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "OtherRollInstrumentDice", name),
//            EventHelper.PropertyChange("Roll(InstrumentDice)", "Luck", invoker.GetName()) });
//                }
//                else if (luckFactor < 0.2)
//                {
//                    robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "OtherRollInstrumentDice", name),
//            EventHelper.PropertyChange("Roll(InstrumentDice)", "BadLuck", invoker.GetName()) });
//                }
//                else
//                {
//                    robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "OtherRollInstrumentDice", name),
//            EventHelper.PropertyChange("Roll(InstrumentDice)", "Neutral", invoker.GetName()) });
//                }
//                robot.Decide();
//            }
//            else if (invoker != this)
//            {
//                robot.GazeAt(invoker.GetName());
//            }
//        }
//    }

//    protected override void InformAlbumResultActions(int albumValue, int marketValue)
//    {
//        int currSpeakingPlayerId = gameManagerRef.GetCurrSpeakingPlayerId();

//        if (albumValue >= marketValue)
//        {
//            robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "RollMarketDice", name),
//            EventHelper.PropertyChange("Roll(MarketDice)", "Success", name) });
//        }
//        else
//        {
//            robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "RollMarketDice", name),
//            EventHelper.PropertyChange("Roll(MarketDice)", "Fail", name) });
//        }

//        if (currSpeakingPlayerId == id)
//        {
//            robot.Decide();
//        }
//        else
//        {
//            robot.GazeAt("Player");
//        }
//    }

//    protected override void InformGameResultActions(GameProperties.GameState state)
//    {
//        int currSpeakingPlayerId = gameManagerRef.GetCurrSpeakingPlayerId();
//        if (state == GameProperties.GameState.VICTORY)
//        {
//            robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "GameEnd", name),
//            EventHelper.PropertyChange("Game(Result)", "Victory", name) });
//        }
//        else if (state == GameProperties.GameState.LOSS)
//        {
//            robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "GameEnd", name),
//            EventHelper.PropertyChange("Game(Result)", "Loss", name) });
//        }

//        if (currSpeakingPlayerId == id)
//        {
//            robot.Decide();
//        }
//        else
//        {
//            robot.GazeAt("Player");
//        }
//    }

//    protected override void InformNewAlbumActions()
//    {
//        playedForInstrument = false;
//        int currSpeakingPlayerId = gameManagerRef.GetCurrSpeakingPlayerId();
//        if (currSpeakingPlayerId == id)
//        {
//            robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Game)", "NewAlbum", name) });

//            if (GameGlobals.albums.Count() == 0)
//            {
//                robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Career)", "Beginning", name) });
//            }
//            else if (GameGlobals.albums.Count() == 4)
//            {
//                robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Career)", "End", name) });
//            }
//            else
//            {
//                robot.Perceive(new Name[] {
//            EventHelper.PropertyChange("State(Career)", "Middle", name) });
//            }
//            robot.Decide();
//        }
//    }
//}
