using Godot;
using System;

public partial class Opponent : Node3D
{
    [Export] string EntityName {get;set;} = "Fishy Fish";
    [Export] Texture2D CharacterSprite {get;set;}
    // [Export] Deck3D Deck {get;set;}

    public override void _Ready()
    {
        SetupSprite();
        // SetupHand();
    }

    private void SetupSprite()
    {
        if (CharacterSprite == null)
        {
            GD.Print($"{EntityName} has an empty sprite config");
            return;
        }
        
        Sprite3D spriteModule = GetNodeOrNull<Sprite3D>("Sprite3D");
        if (spriteModule != null)
            spriteModule.Texture = CharacterSprite;
        else
            GD.Print($"{EntityName} is missing a Sprite3D component");
    }

    // private void SetupHand()
    // {
    //     if (Deck == null)
    //         GD.Print($"{EntityName} is missing a Deck3D component");
        

    // }
}
