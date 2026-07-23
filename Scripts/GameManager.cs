using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class GameManager : Node3D
{
    [Export] public Deck3D VisualDeck;
    [Export] public Node3D Table;
    private List<Card> _serializedDeck = new List<Card>();
    private Random _random = new Random();
    private const int CardsToDeal = 5;
    [Export] private Godot.Collections.Array<Node3D> ActivePlayers = new();
    private int _currentTurnIndex = 0;
    private bool playing = false;

    public override void _Ready()
    {
        // setup players
        PlacePlayers();
    }

    public async void StartNewGame()
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
        await DealCards();

        // start turn cycle
        StartTurnCycle();
    }

    private void PlacePlayers()
    {
        if (ActivePlayers.Count <= 0 || Table == null) return;

        float seatHeightOffset = 0.25f;
        float radius = 1f; // distance from the center

        for (int i = 0; i < ActivePlayers.Count; i++)
        {
            GamePlayer currentPlayer = ActivePlayers[i] as GamePlayer;
            float angle = i * (2.0f * Mathf.Pi / ActivePlayers.Count);

            float x = Table.Position.X + (radius * Mathf.Cos(angle));
            float z = Table.Position.Z + (radius * Mathf.Sin(angle));
            float y = Table.Position.Y + seatHeightOffset;

            Vector3 newPosition = new Vector3(x, y, z);
            Transform3D newTransform = Transform3D.Identity;
            newTransform.Origin = newPosition; // set origin first to avoid error with a global vec3.zero
            
            Vector3 horizontalDir = newPosition - Table.GlobalPosition;
            horizontalDir.Y = 0;

            newTransform = newTransform.LookingAt(newPosition + horizontalDir, Vector3.Up);

            currentPlayer.GlobalTransform = newTransform;
        }
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

    private async System.Threading.Tasks.Task DealCards()
    {
        GD.Print("-- Dealing out cards --");

        List<System.Threading.Tasks.Task> dealingTasks = new();
        foreach (GamePlayer player in ActivePlayers)
        {
            for (int i = 0; i < CardsToDeal; i++)
            {
                DealCardToPlayer(player);
            }
            
            System.Threading.Tasks.Task playerHandTask = player.BuildVisualHand();
            dealingTasks.Add(playerHandTask);
        }

        await System.Threading.Tasks.Task.WhenAll(dealingTasks);
        
        GD.Print("[GameManager] All cards finished dealing!");
    }

    private void RunCurrentTurn()
    {
        if (_currentTurnIndex >= ActivePlayers.Count) // everyone took their turn
        {
            EndGame();
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

    private void StartNextTurn()
    {
        _currentTurnIndex = (_currentTurnIndex + 1) % ActivePlayers.Count;;
        RunCurrentTurn();
    }

    private async void onCardSelected(Card3D card)
    {
        await TakeMatchingCards(card);

        StartNextTurn();
    }

    private async System.Threading.Tasks.Task TakeMatchingCards(Card3D card)
    {
        GamePlayer currentActor = ActivePlayers[_currentTurnIndex] as GamePlayer;
        GamePlayer nextActor = ActivePlayers[(_currentTurnIndex + 1) % ActivePlayers.Count] as GamePlayer;
        
        Transform3D currentOrigTrans = currentActor.GlobalTransform;
        Transform3D nextOrigTrans = nextActor.GlobalTransform;

        // look at the player theyre asking
        Tween lookTween1 = GlanceAtPlayer(currentActor, nextActor, 0.15f);
        Tween lookTween2 = GlanceAtPlayer(nextActor, currentActor);
        await ToSignal(lookTween1, Tween.SignalName.Finished);
        if (lookTween2.IsValid()) await ToSignal(lookTween2, Tween.SignalName.Finished);

        GD.Print($"{currentActor.Name} asked {nextActor.Name} for any {card.CardData.Rank}s");
        currentActor.Say($"do you have any {card.CardData.Rank}s", 2f);

        // make the target look at their cards
        var (origin, downTween, nextactorsprite) = LookAtCards(nextActor, tiltDegrees: 15f, duration: 0.3f);
        await ToSignal(downTween, Tween.SignalName.Finished);

        // wait some time for them to think
        float thinkTime = (float)_random.NextDouble() * 1.5f + 0.5f;
        await ToSignal(GetTree().CreateTimer(thinkTime), SceneTreeTimer.SignalName.Timeout);

        // make look away from the cards
        ResetSprite(nextactorsprite, origin, 0.3f);

        if (nextActor.hasCard(card) is { Count: > 0} matchingCards)
        {
            GD.Print($"{nextActor.Name} has {string.Join(", ", matchingCards.Select(c => c.Name))}");
            nextActor.Say("yeah i have that", 2f);
            
            foreach (Card3D matchedCard in matchingCards)
            {
                nextActor.RemoveCard(matchedCard);
                currentActor.AddCard(matchedCard);
            }

            await nextActor.BuildVisualHand();
        }
        else
        {
            GD.Print($"{nextActor.Name} does not have any {card.CardData.Rank}s, Go Fish");
            nextActor.Say("Go fish", 2f);

            DealCardToPlayer(currentActor);
        }

        await currentActor.BuildVisualHand();

        Tween returnTween1 = ResetGlance(currentActor, currentOrigTrans);
        Tween returnTween2 = ResetGlance(nextActor, nextOrigTrans);
        
        // Await both returns before finishing the method
        await ToSignal(returnTween1, Tween.SignalName.Finished);
        if (returnTween2.IsValid()) await ToSignal(returnTween2, Tween.SignalName.Finished);
    }

    private Tween GlanceAtPlayer(GamePlayer actor, GamePlayer target, float glanceWeight = 0.35f, float duration = 0.4f)
    {
        Transform3D origTrans = actor.GlobalTransform;

        // Calculate horizontal vector to target (factoring in the inverted model direction)
        Vector3 dirToTarget = -(target.GlobalPosition - actor.GlobalPosition);
        dirToTarget.Y = 0;
        Vector3 lookTarget = actor.GlobalPosition + dirToTarget;

        // Calculate full look and interpolate it
        Transform3D fullLook = origTrans.LookingAt(lookTarget, Vector3.Up);
        Transform3D glanceTrans = origTrans.InterpolateWith(fullLook, glanceWeight);

        // Create and execute the tween
        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(actor, "global_transform", glanceTrans, duration);

        return tween;
    }
    private Tween ResetGlance(GamePlayer actor, Transform3D originalTransform, float duration = 0.4f)
    {
        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(actor, "global_transform", originalTransform, duration);

        return tween;
    }

    private (Transform3D OriginalTransform, Tween AnimationTween, Sprite3D sprite) LookAtCards(GamePlayer actor, float tiltDegrees = 15f, float duration = 0.3f)
    {
        Sprite3D actorSprite = actor.GetNodeOrNull<Sprite3D>("Sprite3D");
        if (actorSprite == null) 
        {
            return (Transform3D.Identity, CreateTween(), null);
        }

        // Capture the snapshot right before the animation starts
        Transform3D originalSpriteTransform = actorSprite.GlobalTransform;

        float radians = Mathf.DegToRad(tiltDegrees);
        Basis tiltedBasis = originalSpriteTransform.Basis * new Basis(Vector3.Right, radians);

        Transform3D lookDownTrans = originalSpriteTransform;
        lookDownTrans.Basis = tiltedBasis;

        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(actorSprite, "global_transform", lookDownTrans, duration);

        // Return both pieces of data packed together
        return (originalSpriteTransform, tween, actorSprite);
    }    
    private Tween ResetSprite(Sprite3D actor, Transform3D originalTransform, float duration = 0.4f)
    {
        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(actor, "global_transform", originalTransform, duration);

        return tween;
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

    private void EndGame()
    {
        _currentTurnIndex = 0;

        GamePlayer playerWithMostBooks = null;
        foreach (GamePlayer player in ActivePlayers)
        {
            GD.Print($"{player.Name} - {player.Books} books");

            if (playerWithMostBooks == null || player.Books > playerWithMostBooks.Books)
            {
                playerWithMostBooks = player;
            }
        }

        GD.Print($"{playerWithMostBooks.Name} won with {playerWithMostBooks.Books} books");
    }
}
