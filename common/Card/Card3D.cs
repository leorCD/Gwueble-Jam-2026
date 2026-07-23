using Godot;
using System;

public partial class Card3D : Node3D
{
    public Card CardData {get;private set;}
    [Signal] public delegate void CardHoveredEventHandler(Card3D card);
    [Signal] public delegate void CardUnhoveredEventHandler(Card3D card);

    // turns card data into a physical form
    // takes in a back texture so that if we wanted to change
    // the deck style we could
    public void SetupCard(Card data, Texture2D backTexture, Texture2D frontAltas)
    {
        CardData = data;
        // set the uniform back texture if the back sprite node exists
        Sprite3D _backSprite = GetNodeOrNull<Sprite3D>("BackSprite");
        if (_backSprite != null)
            _backSprite.Texture = backTexture;

        // apply card face
        SetupFace(frontAltas);
    }

    private void SetupFace(Texture2D frontAltas)
    {   
        // if there is no card data or front sprite node then return
        Sprite3D _frontSprite = this.GetNode<Sprite3D>("FrontSprite");
        if (CardData == null || _frontSprite == null) return;

        // get the baseline column index from your rank enum
        int rankColumn = (int)CardData.Rank;
    
        // if (rankColumn >= 9)
        // {   // if it's a face card (index 9 or higher), remap it to match the art columns
        //     rankColumn = CardData.Rank switch
        //     {
        //         CardRank.Whale   => 9,  // First face card column
        //         CardRank.Shark   => 10, // Second face card column 
        //         CardRank.Butter  => 11, // Third face card column
        //         CardRank.Puffer  => 12, // Fourth face card column
        //         _ => rankColumn
        //     };
        // }

        // flatten the grid using the corrected column
        _frontSprite.Texture = frontAltas;
        _frontSprite.Frame = ((int)CardData.Suit * 13) + rankColumn;
    }


    public void onHover() {EmitSignal(SignalName.CardHovered, this);}

    public void onUnhover() {EmitSignal(SignalName.CardUnhovered, this);}
}
