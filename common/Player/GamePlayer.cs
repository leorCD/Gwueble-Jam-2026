using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public abstract partial class GamePlayer : Node3D
{
    public List<Card3D> Hand {get;set;} = new List<Card3D>();
    public int Books {get;set;}
    [Signal] public delegate void CardSelectedEventHandler(Card3D card);
    protected Dictionary<Card3D, Vector3> CardBasePositions = new();
    private HashSet<Card3D> _knownCards = new();
    
    public virtual void AddCard(Card3D newCard, Vector3? spawnGlobalPos = null)
    {
        Hand.Add(newCard); // add card to hand

        if (newCard.GetParent<Node3D>() == null) // reparent
            this.AddChild(newCard); // parent to player
        else
            newCard.Reparent(this);

        Vector3 oldGlobal = newCard.GlobalPosition;

        // if a spawn point was provided, snap the card there instantly
        if (spawnGlobalPos.HasValue)
            newCard.GlobalPosition = spawnGlobalPos.Value;
        else
            newCard.GlobalPosition = oldGlobal;

        CheckForBooks(newCard.CardData.Rank); // detect 4 of the same rank
    }
    public virtual void StartTurn() { throw new NotImplementedException(); }
    public virtual void EndTurn() { throw new NotImplementedException(); }

    private void CheckForBooks(CardRank newCardRank)
    {
        List<Card3D> matchingRanks = Hand.FindAll(c => c.CardData.Rank == newCardRank);

        if (matchingRanks.Count == 4)
        {
            Books++;
            GD.Print($"[GamePlayer] {this.Name} found all {newCardRank}. Total Books: {Books}");

            foreach (Card3D cardToRemove in matchingRanks)
            {
                Hand.Remove(cardToRemove);
                cardToRemove.QueueFree();
            }
        }
    }

    public void RemoveCard(Card3D card)
    {
        if (Hand.Remove(card))
        {
            card.Reparent(GetTree().CurrentScene);
        }
    }

    public void BuildVisualHand()
    {
        if (Hand.Count == 0) return;

        float cardSpacing = 0.05f;
        float defaultSpacing = 0.1f;
        float maxHandWidth = 0.3f;
        if (Hand.Count > 1)
        {
            float tightSpacing = maxHandWidth / (Hand.Count - 1);
            cardSpacing = Mathf.Min(defaultSpacing, tightSpacing);
        }

        float totalWidthOfHand = (Hand.Count - 1) * cardSpacing;
        float startX = -totalWidthOfHand / 2f;
        float distanceForward = 0.2f;
        float fanDipIntensity = 0.015f;
        float centerIndex = (Hand.Count - 1) / 2f;
        float maxRotationAngle = 12f;
        Vector3 offset = new Vector3(0, -0.15f, 0);

        for (int i = 0; i < Hand.Count; i++)
        {
            float normalizedCenterOffset = Hand.Count > 1 ? (i - centerIndex) / centerIndex : 0f;
            float zTilt = -normalizedCenterOffset * maxRotationAngle;
            float yDip = Mathf.Pow(normalizedCenterOffset, 2) * fanDipIntensity;

            Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), -yDip, distanceForward);
            Card3D card = Hand[i];
            Vector3 finalRotation = new Vector3(30, 0, zTilt);

            Vector3 basePosition = targetPosition + offset;
            Vector3 initialLocalZ = new Vector3(0, 0, i * 0.002f); // see note below

            Vector3 finalRestingPosition = basePosition + initialLocalZ;
            CardBasePositions[card] = finalRestingPosition;

            bool isNew = !_knownCards.Contains(card);
            _knownCards.Add(card);

            AnimateCardToRestingPosition(card, finalRestingPosition, finalRotation, isNew);
        }
    }

    protected virtual void AnimateCardToRestingPosition(Card3D card, Vector3 targetPosition, Vector3 targetRotation, bool isNew)
    {
        float duration = isNew ? 2f : 0.2f;

        Tween tween = CreateTween().SetParallel(true);
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(card, "position", targetPosition, duration);
        tween.TweenProperty(card, "rotation_degrees", targetRotation, duration);
    }

    public List<Card3D> hasCard(Card3D card)
    {
        List<Card3D> matchingRanks = new();

        foreach (Card3D cardInHand in Hand)
        {
            if (cardInHand.CardData.Rank == card.CardData.Rank)
            {
                matchingRanks.Add(cardInHand);
            }
        }

        return matchingRanks;
    }
}