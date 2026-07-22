using Godot;
using System;
using System.Collections.Generic;
public abstract partial class GamePlayer : Node3D
{
    public List<Card3D> Hand {get;set;} = new List<Card3D>();
    public int Books {get;set;}
    public virtual void AddCard(Card3D newCard)
    {
        Hand.Add(newCard); // add card to hand
        this.AddChild(newCard); // parent to player
        CheckForBooks(newCard.CardData.Rank); // detect 4 of the same rank
        
        UpdateVisualHand();
    }

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
    public virtual void UpdateVisualHand()
    {
        if (Hand.Count == 0) return; // no anchor or empty hand

        float cardSpacing = 0.05f;
        float defaultSpacing = 0.1f;
        float maxHandWidth = 0.3f;
        if (Hand.Count > 1)
        {   // calculate spacing to fit layout width
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
        
        // position each card
        for (int i = 0; i < Hand.Count; i++)
        {
            float normalizedCenterOffset = 0f; // calculate card from the center from a value of -1 to 1
            if (Hand.Count > 1)
            {
                normalizedCenterOffset = (i - centerIndex) / centerIndex;
            }
            
            float zTilt = -normalizedCenterOffset * maxRotationAngle;
            float yDip = Mathf.Pow(normalizedCenterOffset, 2) * fanDipIntensity; // squared to make a parabola
            
            Vector3 targetPosition = new Vector3(
                startX + (i * cardSpacing),
                -yDip,
                distanceForward
            );
        
            Card3D card = Hand[i];
            card.RotationDegrees = new Vector3(30, 0, zTilt);
            
            Vector3 basePosition = targetPosition + offset;
            Vector3 initialLocalZ = card.Transform.Basis.Z * (i * 0.002f);
            card.Position = basePosition + initialLocalZ;
        }
    }



    [Signal] public delegate void TurnFinishedEventHandler();
    public abstract void TakeTurn(); // abstract method bc ai is auto while player is prompted
    public void FinishTurn()
    {
        EmitSignal(SignalName.TurnFinished);
    }
}
