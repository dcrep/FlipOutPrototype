using System.Collections.Generic;
using UnityEngine;

#region PlayerX-Client
[System.Serializable]
public class PlayerXClient //: MonoBehaviour
{
    
    public string playerName = "PlayerX";
    public int playerId = -1;   // unique id for player
    public int playerNumber = -1;   // player # in-game (0-based)
    //[SerializeField] private HandX playerHand = null;

    public CardPODClient[] hand = null; //new CardPODClient[6];

    // Network connection info?
    // NetManager...

    public List<CardPODClient> scorePile = null; //new List<CardPODClient>();

    public PlayerXClient()
    {
        hand = new CardPODClient[6];
        scorePile = new List<CardPODClient>();
    }

    public int GetIndexOfCardByID(int cardID)
    {
        if (hand == null)
        {
            Debug.LogError("PlayerXC->IndexOfCardByID(): hand is null for player " + playerId);
            return -1;
        }
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardID)
                return i;
        }
        return -1;
    }

    public int GetIndexOfCard(CardPODClient cardPOD)
    {
        int index = GetIndexOfCardByID(cardPOD.cardID);
        return index;
    }

    public CardPODClient GetCardInHandByID(int cardID)
    {
        int index = GetIndexOfCardByID(cardID);
        if (index != -1)
            return hand[index];
        else
            return null;
    }

    public CardColor[] GetHandAsColors()
    {
        CardColor[] handColors = new CardColor[6];
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
            {
                Debug.LogError("PlayerXC->GetHandAsColors(): hand[" + i + "] is null for player " + playerId);
                return null;
            }
            handColors[i] = hand[i].color;
        }
        return handColors;
    }

    public void SwitchCardsInHandByID(int cardID1, int cardID2)
    {
        int index1 = -1;
        int index2 = -1;
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardID1)
                index1 = i;
            else if (hand[i].cardID == cardID2)
                index2 = i;
        }
        if (index1 != -1 && index2 != -1)
        {
            // Swap the cards in hand
            CardPODClient temp = hand[index1];
            hand[index1] = hand[index2];
            hand[index2] = temp;
        }
        else
        {
            Debug.LogError("PlayerXC->SwitchCardsInHandByID(): Could not find both cards to switch for player " + playerId);
        }
    }
}
#endregion

#region PlayerX-Server
[System.Serializable]
public class PlayerXServer
{
    public string playerName = "PlayerX";
    public int playerId = -1;   // unique id for player

    public int playerNumber = -1;   // player # in-game (0-based)

    public CardPODServer[] hand; // = new CardPODServer[6];
    public List<CardPODServer> scorePile = null;

    public PlayerXServer()
    {
        hand = new CardPODServer[6];
        scorePile = new List<CardPODServer>();
    }

    public int GetIndexOfCardByID(int cardID)
    {
        if (hand == null)
        {
            Debug.LogError("PlayerXS->IndexOfCardByID(): hand is null for player " + playerId);
            return -1;
        }
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardID)
                return i;
        }
        return -1;
    }

    public int GetIndexOfCard(CardPODServer cardPOD)
    {
        int index = GetIndexOfCardByID(cardPOD.cardID);
        return index;
    }

    public CardPODServer GetCardInHandByID(int cardID)
    {
        int index = GetIndexOfCardByID(cardID);
        if (index != -1)
            return hand[index];
        else
            return null;
    }

    public void SwitchCardsInHandByID(int cardID1, int cardID2)
    {
        int index1 = -1;
        int index2 = -1;
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardID1)
                index1 = i;
            else if (hand[i].cardID == cardID2)
                index2 = i;
        }
        if (index1 != -1 && index2 != -1)
        {
            // Swap the cards in hand
            CardPODServer temp = hand[index1];
            hand[index1] = hand[index2];
            hand[index2] = temp;
        }
        else
        {
            Debug.LogError("PlayerXS->SwitchCardsInHandByID(): Could not find both cards to switch for player " + playerId);
        }
    }

    private List<CardPODClient> ScorePileToClientScorePile()
    {
        List<CardPODClient> clientScorePile = new List<CardPODClient>();
        foreach (CardPODServer cardPOD in scorePile)
        {
            clientScorePile.Add(cardPOD.CopyToClientCard());
        }
        return clientScorePile;
    }

    private CardPODClient[] HandToClientHand()
    {
        CardPODClient[] clientHand = new CardPODClient[hand.Length];
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] != null)
                clientHand[i] = hand[i].CopyToClientCard();
            else
                clientHand[i] = null;
        }
        return clientHand;
    }

    //! ?? Hand/Score pile...
    public PlayerXClient CopyToClientPlayerX()
    {
        PlayerXClient clientPlayer = new PlayerXClient();
        clientPlayer.playerName = this.playerName;
        clientPlayer.playerId = this.playerId;
        clientPlayer.playerNumber = this.playerNumber;
        // Hand and score pile - need CardObjects! - client-side only
        // ! Maybe redesign of client?
        clientPlayer.hand = HandToClientHand();
        clientPlayer.scorePile = ScorePileToClientScorePile();
        return clientPlayer;
    }

    public CardColor[] GetHandAsColors()
    {
        return GetHandAsColorsForPlayer(playerId);
    }
    public CardColor[] GetHandAsColorsForPlayer(int playerID)
    {
        CardColor[] handColors = new CardColor[6];
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
            {
                Debug.LogError("PlayerXS->GetHandAsColors(): hand[" + i + "] is null for player " + playerId);
                return null;
            }
            handColors[i] = hand[i].ColorBasedOnPlayer(playerID);
        }
        return handColors;
    }
}
#endregion
