using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class GameManager : Node3D
{
    [Export] public Deck3D VisualDeck;
    private List<Card> _serializedDeck = new List<Card>();
    private Random _random = new Random();
    private const int CardsToDeal = 5;

    [Export] private Godot.Collections.Array<Node3D> ActivePlayers = new();
    private int _currentTurnIndex = 0;
    public void StartNewGame()
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

            player.BuildVisualHand();
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

            currentActor.CardSelected -= onCardSelected;
            currentActor.CardSelected += onCardSelected;
            currentActor.StartTurn();
        }
    }

    private void onCardSelected(Card3D card)
    {
        GamePlayer currentActor = ActivePlayers[_currentTurnIndex] as GamePlayer;
        GamePlayer nextActor = ActivePlayers[(_currentTurnIndex + 1) % ActivePlayers.Count] as GamePlayer;
        GD.Print($"{currentActor.Name} asked {nextActor.Name} for any {card.CardData.Rank}s");

        if (nextActor.hasCard(card) is { Count: > 0} matchingCards)
        {
            GD.Print($"{nextActor.Name} has {string.Join(", ", matchingCards.Select(c => c.Name))}");

            foreach (Card3D matchedCard in matchingCards)
            {
                nextActor.RemoveCard(matchedCard);
                currentActor.AddCard(matchedCard);
            }

            nextActor.BuildVisualHand();
            currentActor.BuildVisualHand();
        }
        else
        {
            GD.Print($"{nextActor.Name} does not have any {card.CardData.Rank}s, Go Fish");
            DealCardToPlayer(currentActor);
            currentActor.BuildVisualHand();
        }

        StartNextTurn();
    }
    private void StartNextTurn()
    {
        _currentTurnIndex = (_currentTurnIndex + 1) % ActivePlayers.Count;;
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
            activePlayer.AddCard(drawnCard, VisualDeck.GlobalPosition);

            GD.Print($"{activePlayer.Name} drew a {drawnCard.CardData.Rank} of {drawnCard.CardData.Suit}");
        }
    }
}
