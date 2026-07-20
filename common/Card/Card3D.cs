using Godot;
using System;

public partial class Card3D : Node3D
{
    public Card CardData {get;private set;}

    private Sprite3D _frontSprite;
    private Sprite3D _backSprite;
    public override void _Ready()
    {
        this._frontSprite = GetNode<Sprite3D>("FrontSprite");
        this._backSprite = GetNode<Sprite3D>("BackSprite");
    }

    // turns card data into a physical form
    // takes in a back texture so that if we wanted to change
    // the deck style we could
    public void SetupCard(Card data, Texture2D backTexture)
    {
        CardData = data;

        // set the uniform back texture if the back sprite node exists
        if (_backSprite != null)
            _backSprite.Texture = backTexture;

        // apply card face
        SetupFace();
    }

    private void SetupFace()
    {   
        // if there is no card data or front sprite node then return
        if (CardData == null || _frontSprite == null) return;

        // load correct asset in the future
        // i.e.
        //      texture2d faceTexture = GD.Load<Texture2D>($"res://Sprites/Cards/{CardData.Suit}_{CardData.Rank}.png")
        //      _frontSprite.Texture = faceTexture
    }
}
