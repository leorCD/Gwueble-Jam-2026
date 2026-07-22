using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node3D
{
    [Export] public Deck3D VisualDeck;
    private List<Card> _serializedDeck = new List<Card>();
    private Random _random = new Random();
    private const int CardsToDeal = 10;
    public override void _Ready()
    {
        StartNewGame();
    }

    [Export] private Godot.Collections.Array<Node3D> ActivePlayers = new();
    private int _currentTurnIndex = 0;
    private void StartNewGame()
    {
        // setup deck
        GenerateDeck();
        ShuffleDeck();

        if (VisualDeck != null)
        {
            VisualDeck.Init(_serializedDeck);
            GD.Print($"[GameManager] Successfully generated and spawned {_serializedDeck.Count} cards");
        }
        else
        {
            GD.PrintErr("[GameManager] VisualDeck reference is missing in the inspector");
        }

        // deal cards
        DealCards();

        // start turn cycle
        StartTurnCycle();
    }

    private void GenerateDeck()
    {
        _serializedDeck.Clear();

        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        foreach (CardRank rank in Enum.GetValues<CardRank>())
        {
            _serializedDeck.Add(new Card(rank, suit));
        }
    }

    private void ShuffleDeck()
    {
        int unshuffledCards = _serializedDeck.Count; // amount of cards in the deck
        while (unshuffledCards > 1)
        {
            unshuffledCards--;
            int k = _random.Next(unshuffledCards + 1); // one argument = max value

            Card tempCard = _serializedDeck[k];
            _serializedDeck[k] = _serializedDeck[unshuffledCards];
            _serializedDeck[unshuffledCards] = tempCard;
        }

        GD.Print("[GameManager] Shuffled current deck");
    }


    private void StartTurnCycle()
    {
        GD.Print("-- Starting Turn Cycle --");

        _currentTurnIndex = 0;
        RunCurrentTurn();
    }

    private void DealCards()
    {
        GD.Print("-- Dealing out cards --");
        foreach (GamePlayer player in ActivePlayers)
        {
            for (int i = 0; i < CardsToDeal; i++)
            {
                DealCardToPlayer(player);
            }
        }
    }

    private void RunCurrentTurn()
    {
        if (_currentTurnIndex >= ActivePlayers.Count) // everyone took their turn
        {
            // EndRound();
            _currentTurnIndex = 0;
            return;
        }

        if (ActivePlayers[_currentTurnIndex] is GamePlayer currentActor)
        {
            GD.Print($"[GameManager] {currentActor.GetType().Name}'s turn");

            currentActor.TakeTurn();
        }
    }

    private void OnTurnFinished() // called by both human and AI to tell the manager they finished their turn
    {
        _currentTurnIndex++;
        RunCurrentTurn();
    }


    public void DealCardToPlayer(GamePlayer activePlayer)
    {
        if (VisualDeck == null)
        {
            GD.PrintErr("[GameManager] VisualDeck reference is missing in the inspector");
            return;
        }

        Card3D drawnCard = VisualDeck.DrawCard();
        if (drawnCard != null)
        {
            // pass card to player
            activePlayer.AddCard(drawnCard);

            GD.Print($"{activePlayer.Name} drew a {drawnCard.CardData.Rank} of {drawnCard.CardData.Suit}");
        }
    }
}
