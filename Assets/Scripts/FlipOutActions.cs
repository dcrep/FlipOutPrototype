using System;
using UnityEngine;

[Flags]
public enum FlipOutAction
{
    None  = 0x00,   // invalid
    Flip   = 0x01,  // flip your own or opponent's card
    Switch = 0x02,  // switch one card's position with another - your own or opponent's hand
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
    public CardColor cardColor = CardColor.red;
}


[System.Serializable]
public class FlipOutActions
{
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

    static public FlipOutActions CreateFlipAction(int playerTakingActionId, int playerTargetId, CardActionInfo cardInfo)
    {
        FlipOutActions action = new FlipOutActions();
        action.actionTaken = FlipOutAction.Flip;
        action.playerTakingActionId = playerTakingActionId;
        action.playerTargetId = playerTargetId;
        action.cardSourceInfos = new CardActionInfo[] { cardInfo };
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

}