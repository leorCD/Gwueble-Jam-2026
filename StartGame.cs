using Godot;
using System;
using System.Collections.Generic;

public partial class StartGame : Area3D, IInteractable
{
    [Export] public GameManager game;
    [Export] public Button button;

    public void Interact()
    {
        game.StartNewGame();
        this.GetParent<Node3D>().QueueFree();
    }
}
