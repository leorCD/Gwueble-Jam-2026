using Godot;
using System;

public partial class EventBus : Node
{
    public static EventBus Instance {get;private set;}
    public override void _Ready()
    {
        Instance = this;
    }

    [Signal] public delegate void DialogueRequestedEventHandler(Vector3 position, string text, float lifetime, Color color);
}