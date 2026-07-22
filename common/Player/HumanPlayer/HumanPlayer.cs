using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

public partial class HumanPlayer : GamePlayer
{
    private bool isMyTurn = false;
    public Camera3D PlayerCamera;
    private Card3D _currentlyHoveredCard;
    private Card3D _previouslyHoveredCard;
    public float maxInteractionDistance = 10f;

    public override void _Ready()
    {
        PlayerCamera = GetNode<Camera3D>("Camera3D"); 
    }

    public override void AddCard(Card3D newCard, Vector3? spawnGlobalPos)
    {
        base.AddCard(newCard, spawnGlobalPos);

        newCard.CardHovered += onCardHovered;
        newCard.CardUnhovered += onCardUnhovered;
    }


    public override void _PhysicsProcess(double delta)
    {
        if (!isMyTurn) return;
        PerformCardRaycast();

        if (Input.IsActionJustPressed("MouseLeft"))
        {
            HandleCardSelection();
        }
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
                _previouslyHoveredCard = _currentlyHoveredCard;
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

    private void onCardHovered(Card3D card)
    {
        if (card == null) return; // empty guard

        Vector3 restingPos = GetRestingPosition(card);
        Vector3 localUp = card.Transform.Basis.Y * 0.01f;
        Vector3 localForward = card.Transform.Basis.Z * -0.03f;
        Vector3 hoverTarget = restingPos + localUp + localForward;

        Tween hover = CreateTween();
        hover.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
        hover.TweenProperty(card, "position", hoverTarget, 0.15f);
    }
    private void onCardUnhovered(Card3D card)
    {
        if (card == null) return; // empty guard

        Vector3 restingPos = GetRestingPosition(card);
        Tween unhover = CreateTween();
        unhover.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
        unhover.TweenProperty(card, "position", restingPos, 0.15f);
    }
    
    private void RearrangeHandZOffsets()
    {
        int hoveredIndex = _currentlyHoveredCard != null
            ? Hand.IndexOf(_currentlyHoveredCard)
            : Hand.IndexOf(_previouslyHoveredCard);

        if (hoveredIndex == -1) return;

        for (int i = 0; i < Hand.Count; i++)
        {
            Card3D card = Hand[i];
            if (card == _currentlyHoveredCard) continue;

            if (CardBasePositions.TryGetValue(card, out Vector3 originalPos))
            {
                int distance = Mathf.Abs(hoveredIndex - i);
                Vector3 localZOffset = card.Transform.Basis.Z * (distance * 0.005f);

                Tween t = CreateTween();
                t.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Circ);
                t.TweenProperty(card, "position", originalPos + localZOffset, 0.15f);
            }
        }
    }

    private void HandleCardSelection()
    {
        if (_currentlyHoveredCard == null) return; // no card to select

        EmitSignal(SignalName.CardSelected, _currentlyHoveredCard);

        EndTurn();
    }

    private Vector3 GetRestingPosition(Card3D card)
    {
        int index = Hand.IndexOf(card);
        Vector3 basePos = CardBasePositions.TryGetValue(card, out var p) ? p : card.Position;
        return basePos + card.Transform.Basis.Z * (index * 0.002f);
    }

    public override void StartTurn()
    {
        isMyTurn = true;
    }
    public override void EndTurn()
    {
        isMyTurn = false;
    }
}
