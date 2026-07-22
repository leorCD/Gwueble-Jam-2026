using Godot;
using System;
using System.Collections.Generic;

public partial class HumanPlayer : GamePlayer
{
    private bool isMyTurn = false;
    public Camera3D PlayerCamera;
    public float maxInteractionDistance = 10f;
    public override void _Ready()
    {
        PlayerCamera = GetNode<Camera3D>("Camera3D"); 
    }

    public override void AddCard(Card3D newCard)
    {
        base.AddCard(newCard);

        newCard.CardHovered += onCardHovered;
        newCard.CardUnhovered += onCardUnhovered;
    }


    public override void _PhysicsProcess(double delta)
    {
        if (!isMyTurn) return;
        PerformCardRaycast();
    }

    private void PerformCardRaycast()
    {
        if (PlayerCamera == null) return;
        Card3D hitCard = null;

        Vector2 mousePositionOnScreen = GetViewport().GetMousePosition();
        float rayLength = 100f;

        Vector3 rayOrigin = PlayerCamera.ProjectRayOrigin(mousePositionOnScreen);
        Vector3 rayDirection = PlayerCamera.ProjectRayNormal(mousePositionOnScreen);
        Vector3 rayEnd = rayOrigin + rayDirection * rayLength;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.CollisionMask = 2;
        query.CollideWithAreas = true;

        PhysicsDirectSpaceState3D directSpaceState = GetViewport().GetWorld3D().DirectSpaceState;
        Godot.Collections.Dictionary result = directSpaceState.IntersectRay(query);

        // check if the ray hit something
        if (result.Count > 0)
        {
            Node intersectedNode = (Node)result["collider"];
            
            if (intersectedNode is Area3D area && area.GetParent() is Card3D card && Hand.Contains(card))
            {
                hitCard = card;
            }
        }

        // handle the Hover state transitions
        if (hitCard != _currentlyHoveredCard)
        {
            // If we were looking at a card before, trigger unhover
            if (_currentlyHoveredCard != null)
            {
                onCardUnhovered(hitCard);
            }

            _currentlyHoveredCard = hitCard;

            // If we are looking at a new card, trigger hover
            if (hitCard != null)
            {
                onCardHovered(hitCard);
            }
            else
            {
                _currentlyHoveredCard = null;
            }

            RearrangeHandZOffsets();
        }
    }

    private Card3D _currentlyHoveredCard;
    private Dictionary<Card3D, Vector3> _cardOrigins = new Dictionary<Card3D, Vector3>();
    private void onCardHovered(Card3D card)
    {
        if (card == null) return; // empty guard

        if (!_cardOrigins.ContainsKey(card))
        {
            _cardOrigins[card] = card.Position;
        }

        // Fix: Use local basis vectors to pull the card upward and forward relative to the hand's tilt
        Vector3 localUp = card.Transform.Basis.Y * 0.03f;
        Vector3 localForward = card.Transform.Basis.Z * -0.015f; // Pull forward 1.5cm to cleanly mount the stack
        Vector3 hoverTarget = _cardOrigins[card] + localUp + localForward;

        Tween hover = CreateTween();
        hover.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
        hover.TweenProperty(card, "position", hoverTarget, 0.15f);
    }
    private void onCardUnhovered(Card3D card)
    {
        if (card == null) return; // empty guard

        // retrieve original postiion and pattern match
        if (!_cardOrigins.TryGetValue(card, out Vector3 originalPosition))
        {
            // Calculate its resting layout index to blend smoothly back into the base Z stack
            int index = Hand.IndexOf(card);
            Vector3 localZOffset = card.Transform.Basis.Z * (index * 0.002f);

            Tween unhover = CreateTween();
            unhover.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
            unhover.TweenProperty(card, "position", originalPosition + localZOffset, 0.15f);
        }
    }

    public override void UpdateVisualHand()
    {
        base.UpdateVisualHand();
        _cardOrigins.Clear();

        for (int i = 0; i < Hand.Count; i++)
        {
            Card3D card = Hand[i];
            
            // store the exact coordinate GamePlayer just assigned it
            _cardOrigins[card] = card.Position;

            // apply the baseline depth stack if we aren't actively hovering something
            if (card != _currentlyHoveredCard)
            {
                // gives a natural left-to-right stacking priority out of the box
                Vector3 localZOffset = card.Transform.Basis.Z * (i * 0.002f);
                card.Position = _cardOrigins[card] + localZOffset;
            }
        }
    }
    private void RearrangeHandZOffsets()
    {
        // 1. Reset everything to baseline layout order if nothing is hovered
        if (_currentlyHoveredCard == null)
        {
            for (int i = 0; i < Hand.Count; i++)
            {
                Card3D card = Hand[i];
                if (_cardOrigins.TryGetValue(card, out Vector3 originalPos))
                {
                    // Clean cascading baseline stack from left to right
                    Vector3 localZOffset = card.Transform.Basis.Z * (i * 0.002f);
                    
                    Tween t = CreateTween();
                    t.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
                    t.TweenProperty(card, "position", originalPos + localZOffset, 0.15f);
                }
            }
            return;
        }

        int hoveredIndex = Hand.IndexOf(_currentlyHoveredCard);
        if (hoveredIndex == -1) return;

        // 2. Adjust non-hovered cards dynamically to bend away from the highlighted one
        for (int i = 0; i < Hand.Count; i++)
        {
            Card3D card = Hand[i];
            if (card == _currentlyHoveredCard) continue; // Let onCardHovered control the peak card

            if (_cardOrigins.TryGetValue(card, out Vector3 originalPos))
            {
                int distance = Mathf.Abs(hoveredIndex - i);
                
                // Scale distance mathematically (push background cards further out along local Z)
                Vector3 localZOffset = card.Transform.Basis.Z * (distance * 0.005f);

                Tween t = CreateTween();
                t.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
                t.TweenProperty(card, "position", originalPos + localZOffset, 0.15f);
            }
        }
    }

    public override void TakeTurn()
    {
        isMyTurn = true;

        
        foreach (Card3D card in Hand)
        {
            GD.Print(card);
        }
    }

}
