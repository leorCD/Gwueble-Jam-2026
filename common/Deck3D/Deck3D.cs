using Godot;
using System.Collections.Generic;

public partial class Deck3D : Node3D
{
    [Export] public Texture2D CardBackTexture; // uniform deck style
    [Export] public Texture2D CardFrontTextureAtlas; // uniform deck style
    [Export] public PackedScene CardTemplate; // just contains an empty card
    private Stack<Card3D> _physicalStack = new Stack<Card3D>();
    private const float _cardPadding = 0.002f; // how thick each card is

    // public override void _Ready()
    // {
    //     MeshInstance3D debugPosition = GetNodeOrNull<MeshInstance3D>("debugPosition");
    //     if (debugPosition != null)
    //     {
    //         // hide the debugPosition box
    //         StandardMaterial3D mat = debugPosition.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
            
    //         Color newColor = mat.AlbedoColor;
    //         newColor.A = 0.0f; // 0% visible
    //         mat.AlbedoColor = newColor;
    //     }
    // }

    public void Init(List<Card> deckData)
    {
        ClearDeck();

        // .Count is a built in, number of children property
        for (int i = 0; i < deckData.Count; i++)
        {
            Card3D newCard = CardTemplate.Instantiate<Card3D>();
            this.AddChild(newCard);

            float yOffset = i * _cardPadding;
            newCard.Position = new Vector3(0, yOffset, 0); // always relative to parent
            newCard.RotationDegrees = new Vector3(-90f, 0, 0); // face down

            newCard.SetupCard(deckData[i], CardBackTexture, CardFrontTextureAtlas); // initialize faces
            newCard.Name = $"{newCard.CardData.Rank} of {newCard.CardData.Suit}";

            _physicalStack.Push(newCard);
        }
    }

    public void ClearDeck()
    {
        while (_physicalStack.Count > 0)
        {
            Card3D card = _physicalStack.Pop(); // remove card from stack
            card.QueueFree(); // delete card
        }
    }

    public Card3D DrawCard()
    {
        if (_physicalStack.Count == 0)
        {
            GD.PrintErr("[Deck3D] Attempted to draw a card from an empty deck");
            return null;
        }

        Card3D topCard = _physicalStack.Pop();
        RemoveChild(topCard); // we need to explicitly change the parent

        return topCard;
    }
}
