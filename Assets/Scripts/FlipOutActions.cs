using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FlipOut Game rules @ https://www.ultraboardgames.com/flipout/game-rules.php

// FlipOut Game rules @ https://www.ultraboardgames.com/flipout/game-rules.php
// Note: See FlipOutActions, duplicated enum values..
// TurnActions - 2 per turn!
// Flip - choose your own or opponent's card to flip
// Switch - choose 2 cards to switch positions - can be your own or opponent's
// Swap1 - swap one of your cards with another player's - no flipping
// Swap2 requires 2 adjacent same color cards from each player
// Score/Swipe require drawing more cards after action is complete
[Flags]
public enum TurnAction
{
    None  = 0x00,   // invalid
    Flip   = 0x01,  // flip your own or opponent's card
    Switch = 0x02,  // switch one card's position with another - your own or opponent's hand (within same hand)
    Swap1  = 0x04,  // swap one of your cards with another player's - WITHOUT flipping either card
    Swap2  = 0x08,  // swap 2 adjacent same-color cards of yours with another player's 2 adjacent same-color cards
                    // (doesn't have to be the same colors as yours)
    Score  = 0x10,  // score a set of 4 to 6 adjacent same-color cards from your hand, redraw up to 6
    Swipe  = 0x20   // score a set of 4 to 6 adjacent same-color cards from another player's hand
               // - you score total-1 (in scoring pile), they score 1 (in their scoring pile), both redraw up to 6
}

// !Note: See TurnAction in GameStateServer - most values here overlap, although
// there are more action types here to cover actions not defined in the game rules
[Flags]
public enum FlipOutAction
{
    None  = 0x00,   // invalid
    Flip   = 0x01,  // flip your own or opponent's card
    Switch = 0x02,  // switch one card's position with another - your own or opponent's hand (within same hand)
    Swap1  = 0x04,  // swap one of your cards with another player's - WITHOUT flipping either card
    Swap2  = 0x08,  // swap 2 adjacent same-color cards of yours with another player's 2 adjacent same-color cards
                    // (doesn't have to be the same colors as yours)
    Score  = 0x10,  // score a set of 4 to 6 adjacent same-color cards from your hand, redraw up to 6
    Swipe  = 0x20,   // score a set of 4 to 6 adjacent same-color cards from another player's hand
               // - you score total-1 (in scoring pile), they score 1 (in their scoring pile), both redraw up to 6
    Deal  = 0x40,   // deal cards to players (special automatic action that doesn't count towards player actions) -1 is top of draw pile?
    TurnEnd = 0x80, // end of player's turn
    EndGame = 0x100 // end of game
}

[System.Serializable]
public class CardActionInfo
{
    public int cardID = -1;
    public CardColor cardColor = CardColor.invalid;
}


[System.Serializable]
public class FlipOutActions
{
    static public FlipOutGame flipOutGame;
    public FlipOutAction actionTaken = FlipOutAction.None;

    public int playerTakingActionId = -1;
    public int playerTargetId = -1;

    //int[] cardSourceIds = null;
    //int[] cardDestIds = null;
    public CardActionInfo[] cardSourceInfos = null;
    public CardActionInfo[] cardDestInfos = null;
    public int[] positions = null;

    static public FlipOutActions CreateDealAction(int playerTargetId, CardActionInfo[] cardInfos, int[] positions, CardColor deckTopColor)
    {
        if (cardInfos.Length != positions.Length)
        {
            Debug.LogError("FlipOutActions->CreateDealAction(): cardInfos length does not match positions length!");
            return null;
        }
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Deal;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = cardInfos;
        action.cardDestInfos = new CardActionInfo[] { new() { cardID = -1, cardColor = deckTopColor } }; // deck top info
        action.positions = positions;
        return action;
    }

    static public FlipOutActions CreateFlipAction(int playerTakingActionId, int playerTargetId, CardActionInfo cardInfo, CardActionInfo oppositeSideInfo)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Flip;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = new CardActionInfo[] { cardInfo };
        action.cardDestInfos = new CardActionInfo[] { oppositeSideInfo };
        return action;
    }

    static public FlipOutActions CreateSwitchAction(int playerTakingActionId, int playerTargetId,
                                                    CardActionInfo cardSourceInfo, CardActionInfo cardDestInfo)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Switch;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = new CardActionInfo[] { cardSourceInfo };
        action.cardDestInfos = new CardActionInfo[] { cardDestInfo };
        return action;
    }
    static public FlipOutActions CreateSwap1Action(int playerTakingActionId, int playerTargetId,
                                                   CardActionInfo cardSourceInfo, CardActionInfo cardDestInfo)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Swap1;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = new CardActionInfo[] { cardSourceInfo };
        action.cardDestInfos = new CardActionInfo[] { cardDestInfo };
        return action;
    }

    static public FlipOutActions CreateSwap2Action(int playerTakingActionId, int playerTargetId, 
                                                   CardActionInfo cardSourceInfo1, CardActionInfo cardSourceInfo2,
                                                   CardActionInfo cardDestInfo1, CardActionInfo cardDestInfo2)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Swap2;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = new CardActionInfo[] { cardSourceInfo1, cardSourceInfo2 };
        action.cardDestInfos = new CardActionInfo[] { cardDestInfo1, cardDestInfo2 };
        return action;
    }

    static public FlipOutActions CreateScoreAction(int playerTakingActionId, CardActionInfo[] cardInfos, int[] positions)
    {
        if (cardInfos.Length != positions.Length)
        {
            Debug.LogError("FlipOutActions->CreateScoreAction(): cardInfos length does not match positions length!");
            return null;
        }
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Score;
        action.playerTakingActionId = playerTakingActionId;
        action.cardSourceInfos = cardInfos;
        action.positions = positions;
        return action;
    }

    static public FlipOutActions CreateSwipeAction(int playerTakingActionId, int playerTargetId, CardActionInfo[] cardInfos, int[] positions)
    {
        if (cardInfos.Length != positions.Length)
        {
            Debug.LogError("FlipOutActions->CreateSwipeAction(): cardInfos length does not match positions length!");
            return null;
        }

        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Swipe;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = cardInfos;
        action.positions = positions;
        return action;
    }

    static public FlipOutActions CreateTurnEndAction(int playerTakingActionId)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.TurnEnd;
        action.playerTakingActionId = playerTakingActionId;
        return action;
    }

    // Doesn't need player ID, but perhaps it helps to know which player caused an end-game
    static public FlipOutActions CreateEndGameAction(int playerTakingActionId)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.EndGame;
        action.playerTakingActionId = playerTakingActionId;
        return action;
    }


    static public void ActOnFlipOutActionForCurrentPlayer(FlipOutActions action)
    {
        Debug.Log("ActOnFlipOutActionForCurrentPlayer: Acting on action " + action.actionTaken.ToString() + " for current player (#" + GameStateClient.GetCurrentPlayerNumber() + ").");

            /*if (GameManager.Instance.uiManager.animationManager.IsRunningAnimations())
            {
                GameManager.Instance.uiManager.animationManager.SetActionCompleteCallback(() =>
                {
                    ActOnFlipOutActionForCurrentPlayer(action);
                }, true);
                return;
            }*/
            switch (action.actionTaken)
            {
                case FlipOutAction.Flip:
                    // Flip action is defined opposite(src)->(dest)facing-player side,
                    // Only the owning player will switch this around to facing->opposite (which means using Source color as destination color)
                    CardColor destColor = (GameStateClient.GetCurrentPlayerId() == action.playerTargetId) ? 
                        action.cardSourceInfos[0].cardColor : action.cardDestInfos[0].cardColor;
                    flipOutGame.FlipCardClient(action.cardDestInfos[0].cardID, destColor);
                    flipOutGame.ClearHighlightedCards();
                    break;
                case FlipOutAction.Switch:
                    flipOutGame.SwitchCardsClient(action.cardSourceInfos[0].cardID, action.cardDestInfos[0].cardID);
                    flipOutGame.ClearHighlightedCards();
                    break;
                case FlipOutAction.Swap1:
                    // Colors only necessary for non-participating players which will see flip+move effect
                    Debug.Log("ActOnFlipOutActionForCurrentPlayer: Processing Swap1 action between player " + action.playerTakingActionId +
                              " and player " + action.playerTargetId +
                              " swapping card " + action.cardSourceInfos[0].cardID +
                              " with card " + action.cardDestInfos[0].cardID);
                    flipOutGame.SwapCards1Client(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.cardSourceInfos[0].cardID,
                        action.cardDestInfos[0].cardID,
                        action.cardSourceInfos[0].cardColor,
                        action.cardDestInfos[0].cardColor);
                    flipOutGame.ClearHighlightedCards();
                    break;
                case FlipOutAction.Swap2:
                    // Colors only necessary for non-participating players which will see flip+move effect
                    flipOutGame.SwapCards2Client(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.cardSourceInfos[0].cardID,
                        action.cardSourceInfos[1].cardID,
                        action.cardDestInfos[0].cardID,
                        action.cardDestInfos[1].cardID,
                        action.cardSourceInfos[0].cardColor,
                        action.cardSourceInfos[1].cardColor,
                        action.cardDestInfos[0].cardColor,
                        action.cardDestInfos[1].cardColor);
                    flipOutGame.ClearHighlightedCards();
                    break;
                case FlipOutAction.Score:
                    flipOutGame.MoveCardsToScorePile(action.playerTakingActionId, action.positions, action.cardSourceInfos[0].cardColor);
                    break;
                case FlipOutAction.Swipe:
                    flipOutGame.SwipeCardsToScorePiles(
                        action.playerTakingActionId,
                        action.playerTargetId,
                        action.positions,
                        action.cardSourceInfos[0].cardColor);
                    break;
                case FlipOutAction.Deal:
                    //! Note: Dealing actually flips for the owner player, but we're showing the deck top
                    // as the opposite color (facing away from the player) so it looks correct
                    List<CardPODClient> dealtCards = new List<CardPODClient>();
                    for (int j = 0; j < action.cardSourceInfos.Length; j++)
                    {
                        CardPODClient cardPOD = new CardPODClient
                        {
                            cardID = action.cardSourceInfos[j].cardID,
                            color = action.cardSourceInfos[j].cardColor,
                            state = CardState.playerHolder,
                            ownerPlayerID = action.playerTargetId
                        };
                        dealtCards.Add(cardPOD);
                    }
                    GameStateClient.CurrentGameStateClient.AssignCardsToPlayerHand(
                        action.playerTargetId,
                        dealtCards,
                        action.positions);
                    
                    //GameManager.Instance.DealFullHandClientFromState(action.playerTargetId);
                    flipOutGame.DealNewCardsToClient(action.playerTargetId, dealtCards, action.positions, action.cardDestInfos[0].cardColor);
                    break;
                case FlipOutAction.TurnEnd:
                    // act on if current player
                    if (GameStateClient.GetCurrentPlayerId() == action.playerTakingActionId)
                    {
                        Debug.Log("ActOnFlipOutActionsForCurrentPlayer: Ending current player's turn as per action.");
                        GameStateClient.CurrentGameStateClient.ClearCurrentPlayerActionsTaken();
                        GameManager.Instance.uiManager.animationManager.SetActionCompleteCallback(() => GameManager.Instance.EndTurnClient(), true);
                    }
                    break;
                case FlipOutAction.EndGame:
                    // ignore - game is already ended by previous player (i think)
                    //! (or should I trigger end-game notification here?)
                    GameManager.Instance.EndGameClient(action.playerTakingActionId);
                    break;
                default:
                    Debug.LogWarning("ActOnFlipOutActionsForCurrentPlayer: Unknown action encountered.");
                    break;
            }
            GameManager.Instance.uiManager.animationManager.Run();
    }

    static public void ActOnFlipOutActionsForCurrentPlayer()
    {
        FlipOutActionsAndAnimationsHelper.ActOnFlipOutActionsForCurrentPlayerCo();
        
        /*List<FlipOutActions> listofActions = GameStateClient.CurrentGameStateClient.GetListOfActionsSinceLastTurn();
        Debug.Log("ActOnFlipOutActionsForCurrentPlayer: Acting on " + listofActions.Count + " actions for current player.");

        for (int i = 0; i < listofActions.Count; i++)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayer: Action " + i + " is " + listofActions[i].actionTaken.ToString());
            
            ActOnFlipOutActionForCurrentPlayer(listofActions[i]);
        }

        GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
        if (GameStateClient.CurrentGameStateClient.GetCurrentPlayerActionsTaken() >= 2)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayer: Current player has taken 2 actions, ending turn.");
            GameManager.Instance.serverDispatch.EndTurn();
        }*/
    }
}

[Serializable]
public class FlipOutActionsAndAnimationsHelper : MonoBehaviour
{
    public static FlipOutActionsAndAnimationsHelper Instance;

    void CreateThisInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    static public void ActOnFlipOutActionsForCurrentPlayerCo()
    {
        if (Instance == null)
        {
            GameObject helperObj = new GameObject("FlipOutActionsAndAnimationsHelper");
            Instance = helperObj.AddComponent<FlipOutActionsAndAnimationsHelper>();
            DontDestroyOnLoad(helperObj);
        }
        Instance.StartCoroutine(Instance.ActOnFlipOutActionsForCurrentPlayerCoroutine());
    }

    private IEnumerator ActOnFlipOutActionsForCurrentPlayerCoroutine()
    {
        List<FlipOutActions> listofActions = GameStateClient.CurrentGameStateClient.GetListOfActionsSinceLastTurn();
        Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Acting on " + listofActions.Count + " actions for current player.");

        for (int i = 0; i < listofActions.Count; i++)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Action " + i + " is " + listofActions[i].actionTaken.ToString());

            FlipOutActions.ActOnFlipOutActionForCurrentPlayer(listofActions[i]);

            // Wait until all animations complete before proceeding to next action
            while (GameManager.Instance.uiManager.animationManager.IsRunningAnimations())
            {
                yield return null;
            }
            GameManager.Instance.uiManager.UpdateScoresDisplay();
        }

        GameStateClient.CurrentGameStateClient.ClearActionsSinceLastTurn();
        if (GameStateClient.CurrentGameStateClient.GetCurrentPlayerActionsTaken() >= 2)
        {
            Debug.Log("ActOnFlipOutActionsForCurrentPlayerCoroutine: Current player has taken 2 actions, ending turn.");
            GameManager.Instance.serverDispatch.EndTurn();
        }
    }
}
