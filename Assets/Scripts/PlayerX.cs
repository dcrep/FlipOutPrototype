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

    public int GetIndexOfCard(CardPODClient cardPOD)
    {
        if (hand == null)
        {
            Debug.LogError("PlayerXC->IndexOfCard(): hand is null for player " + playerId);
            return -1;
        }
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardPOD.cardID)
                return i;
        }
        return -1;
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

    public int GetIndexOfCard(CardPODServer cardPOD)
    {
        if (hand == null)
        {
            Debug.LogError("PlayerXS->IndexOfCard(): hand is null for player " + playerId);
            return -1;
        }
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i].cardID == cardPOD.cardID)
                return i;
        }
        return -1;
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

    private List<CardPODClient> ScorePileToClientScorePile()
    {
        List<CardPODClient> clientScorePile = new List<CardPODClient>();
        foreach (CardPODServer cardPOD in scorePile)
        {
            clientScorePile.Add(cardPOD.CopyToClientCard());
        }
        return clientScorePile;
    }

    //! Hand/Score pile...
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
