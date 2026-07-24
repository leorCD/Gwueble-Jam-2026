using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class GameManager : Node3D
{
    [Export] public Godot.Collections.Array<CardRank> BannedRanks = new();
    [Export] public Godot.Collections.Array<CardSuit> BannedSuits = new();
    [Export] public int CardsToDeal = 5;


    [Export] public Deck3D VisualDeck;
    private List<Card> _serializedDeck = new List<Card>();
    private Random _random = new Random();    [Export] private Godot.Collections.Array<Node3D> ActivePlayers = new();
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
        if (ActivePlayers.Count <= 0 || VisualDeck == null) return;

        float seatHeightOffset = 1f;
        float radius = 3f; // distance from the center

        for (int i = 0; i < ActivePlayers.Count; i++)
        {
            GamePlayer currentPlayer = ActivePlayers[i] as GamePlayer;
            float angle = i * (2.0f * Mathf.Pi / ActivePlayers.Count);

            float x = VisualDeck.Position.X + (radius * Mathf.Cos(angle));
            float z = VisualDeck.Position.Z + (radius * Mathf.Sin(angle));
            float y = VisualDeck.Position.Y + seatHeightOffset;

            Vector3 newPosition = new Vector3(x, y, z);
            Transform3D newTransform = Transform3D.Identity;
            newTransform.Origin = newPosition; // set origin first to avoid error with a global vec3.zero
            
            Vector3 horizontalDir = newPosition - VisualDeck.GlobalPosition;
            horizontalDir.Y = 0;

            newTransform = newTransform.LookingAt(newPosition + horizontalDir, Vector3.Up);

            currentPlayer.GlobalTransform = newTransform;
        }
    }

    private void GenerateDeck()
    {
        _serializedDeck.Clear();

        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        {
            // If this entire suit is banned, skip it completely
            if (BannedSuits != null && BannedSuits.Contains(suit)) continue;

            foreach (CardRank rank in Enum.GetValues<CardRank>())
            {
                // If this rank is banned, skip it completely
                if (BannedRanks != null && BannedRanks.Contains(rank)) continue;

                _serializedDeck.Add(new Card(rank, suit));
            }
        }
        
        GD.Print($"[GameManager] Deck generated with {_serializedDeck.Count} matching cards.");
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

    private async void RunCurrentTurn()
    {
        if (_serializedDeck.Count <= 0)
        {
            EndGame();
            return;
        }

        if (ActivePlayers[_currentTurnIndex] is GamePlayer currentActor)
        {
            if (currentActor.Hand.Count == 0)
            {
                if (_serializedDeck.Count > 0) // if there are cards to draw from
                {
                    GD.Print($"[GameManager] {currentActor.Name} has no cards! Drawing a mandatory card to play.");
                    
                    // Force a draw and wait for the visual hand to update
                    DealCardToPlayer(currentActor);
                    await currentActor.BuildVisualHand();
                }
                else
                {
                    // No cards in hand and no cards in deck -> skip turn
                    GD.Print($"[GameManager] {currentActor.Name} has no cards and deck is empty. Skipping turn.");
                    StartNextTurn();
                    return;
                }
            }

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
        if (card != null)
        {
            await TakeMatchingCards(card);
        }

        StartNextTurn();
    }

    private async System.Threading.Tasks.Task CheckForBooks(GamePlayer currentPlayer, CardRank newCardRank)
    {
        List<Card3D> matchingRanks = currentPlayer.Hand.FindAll(c => c.CardData.Rank == newCardRank);

        if (matchingRanks.Count == GetRequiredCardsForBook())
        {
            currentPlayer.Books++;
            GD.Print($"[GamePlayer] {this.Name} found all {newCardRank}. Total Books: {currentPlayer.Books}");

            foreach (Card3D cardToRemove in matchingRanks)
            {
                currentPlayer.Hand.Remove(cardToRemove);
            }

            _ = currentPlayer.BuildVisualHand();

            Vector3 VisualDeckTargetPos = GetBookVisualDeckTopPosition(currentPlayer); 
            List<Tween> activeTweens = new List<Tween>();

            for (int i = 0; i < matchingRanks.Count; i++)
            {
                Card3D card = matchingRanks[i];
                
                if (card.GetNodeOrNull<Sprite3D>("FrontSprite") is Sprite3D sprite)
                {
                    sprite.Modulate = Colors.White;
                }

                // Reparent to the root scene so the cards don't rotate/move with the player mid-flight
                card.Reparent(GetTree().CurrentScene);

                // Add a tiny vertical offset (Y) per card so they stack neatly without Z-fighting
                Vector3 individualTarget = VisualDeckTargetPos + new Vector3(0, i * 0.003f, 0);
                
                // Set rotation to lay completely flat on the VisualDeck surface (pitch 90 deg)
                Vector3 targetRotation = new Vector3(90, 0, 0);

                // Fire off the parallel arcs!
                Tween jumpTween = AnimateCardToVisualDeck(card, individualTarget, targetRotation);
                if (jumpTween != null)
                {
                    activeTweens.Add(jumpTween);
                }
            }

            // 4. Wait for every card in the book to complete its flight
            foreach (Tween tween in activeTweens)
            {
                if (tween.IsValid())
                {
                    await ToSignal(tween, Tween.SignalName.Finished);
                }
            }

            foreach (Card3D cardToRemove in matchingRanks)
            {
                _serializedDeck.Remove(cardToRemove.CardData);
            }
        }
    }

    private int GetRequiredCardsForBook()
    {
        // A standard book has 4 cards (one per suit), minus any suits that are completely banned
        int suitsCount = Enum.GetValues<CardSuit>().Length;
        if (BannedSuits != null) suitsCount -= BannedSuits.Count;
        return suitsCount;
    }

    protected virtual Vector3 GetBookVisualDeckTopPosition(GamePlayer player)
    {
        if (VisualDeck == null)
        {
            GD.PrintErr("[GameManager] VisualDeck reference is missing! Defaulting to Vector3.Zero");
            return Vector3.Zero;
        }
        
        // 1. Start exactly at the VisualDeck's center base position
        Vector3 VisualDeckCenter = VisualDeck.GlobalPosition;

        // 2. Find the horizontal direction vector from the VisualDeck center pointing toward the player
        Vector3 dirToPlayer = player.GlobalPosition - VisualDeckCenter;
        dirToPlayer.Y = 0; // Keep it strictly horizontal on the flat plane
        dirToPlayer = dirToPlayer.Normalized();

        // 3. Offset the landing zone halfway toward the player (0.5 units out of the 1.0f radius)
        // This stops books from stacking up on top of each other in the exact center of the VisualDeck.
        float distanceOffsetFromCenter = 0.5f;
        Vector3 horizontalLandingSpot = VisualDeckCenter + (dirToPlayer * distanceOffsetFromCenter);

        // 4. Force the height to exactly match the VisualDeck's baseline position (Y)
        return new Vector3(horizontalLandingSpot.X, VisualDeckCenter.Y + (0.005f * player.Books), horizontalLandingSpot.Z);
    }

    protected Tween AnimateCardToVisualDeck(Card3D card, Vector3 globalPosition, Vector3 targetRotation)
    {
        // 1. Create a parallel tween for horizontal movement and rotation
        Tween tween = CreateTween().SetParallel(true);
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

        // Slide horizontally across the VisualDeck space
        tween.TweenProperty(card, "global_position:x", globalPosition.X, 0.6f);
        tween.TweenProperty(card, "global_position:z", globalPosition.Z, 0.6f);
        
        // Rotate the card so it lays flat on the VisualDeck surface
        tween.TweenProperty(card, "global_rotation_degrees", targetRotation, 0.6f);

        // 2. Create a separate, sequential tween chain strictly for the Y (height) axis to create the "jump" arc
        Tween yTween = CreateTween().SetTrans(Tween.TransitionType.Quad);
        
        // Calculate a peak height (adds 0.25 meters of altitude above the highest point)
        float peakHeight = Mathf.Max(card.GlobalPosition.Y, globalPosition.Y) + 0.25f; 
        
        // Phase 1: Launch upward into the air
        yTween.TweenProperty(card, "global_position:y", peakHeight, 0.25f).SetEase(Tween.EaseType.Out);
        
        // Phase 2: Fall down onto the VisualDeck surface
        yTween.TweenProperty(card, "global_position:y", globalPosition.Y, 0.35f).SetEase(Tween.EaseType.In);

        return tween;
    }

    private async System.Threading.Tasks.Task TakeMatchingCards(Card3D card)
    {
        GamePlayer currentActor = ActivePlayers[_currentTurnIndex] as GamePlayer;
        GamePlayer nextActor = ActivePlayers[(_currentTurnIndex + 1) % ActivePlayers.Count] as GamePlayer;
        
        Transform3D currentOrigTrans = currentActor.GlobalTransform;
        Transform3D nextOrigTrans = nextActor.GlobalTransform;

        // look at the player theyre asking
        Tween lookTween1 = GlanceAtPlayer(currentActor, nextActor, 0.15f);
        Tween lookTween2 = GlanceAtPlayer(nextActor, currentActor, 0.35f);
        await ToSignal(lookTween1, Tween.SignalName.Finished);
        if (lookTween2.IsValid()) await ToSignal(lookTween2, Tween.SignalName.Finished);

        GD.Print($"{currentActor.Name} asked {nextActor.Name} for any {card.CardData.Rank}s");
        currentActor.Say($"do you have any {card.CardData.Rank}s", 2f);

        // make the target look at their cards
        var (origin, downTween) = LookAtCards(nextActor, tiltDegrees: 15f, duration: 0.3f);
        await ToSignal(downTween, Tween.SignalName.Finished);

        // wait some time for them to think
        float thinkTime = (float)_random.NextDouble() * 1.5f + 0.5f;
        await ToSignal(GetTree().CreateTimer(thinkTime), SceneTreeTimer.SignalName.Timeout);

        // make look away from the cards
        ResetModelLook(nextActor, origin, 0.3f);

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
            
            await CheckForBooks(currentActor, card.CardData.Rank);
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

    private (Transform3D OriginalTransform, Tween AnimationTween) LookAtCards(GamePlayer actor, float tiltDegrees = 15f, float duration = 0.3f)
    {
        Node3D actorModel = actor.GetNodeOrNull<Node3D>("Model");
        if (actorModel == null) return (Transform3D.Identity, CreateTween());

        // Capture the snapshot right before the animation starts
        Transform3D originalModelTransform = actorModel.GlobalTransform;

        float radians = Mathf.DegToRad(tiltDegrees);
        Basis tiltedBasis = originalModelTransform.Basis * new Basis(Vector3.Right, radians);

        Transform3D lookDownTrans = originalModelTransform;
        lookDownTrans.Basis = tiltedBasis;

        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(actorModel, "global_transform", lookDownTrans, duration);

        // Return both pieces of data packed together
        return (originalModelTransform, tween);
    }    
    private Tween ResetModelLook(GamePlayer actor, Transform3D originalTransform, float duration = 0.4f)
    {
        Node3D actorModel = actor.GetNodeOrNull<Node3D>("Model");
        if (actorModel == null) return CreateTween();

        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(actorModel, "global_transform", originalTransform, duration);

        return tween;
    }
    
    public async void DealCardToPlayer(GamePlayer activePlayer)
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
            await CheckForBooks(activePlayer, drawnCard.CardData.Rank);

            // GD.Print($"{activePlayer.Name} drew a {drawnCard.CardData.Rank} of {drawnCard.CardData.Suit}");
        }
    }
}
