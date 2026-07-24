using System;
using Godot;

public partial class DialogueManager : Node
{
    // Assign your FloatingText3D.tscn scene to this slot in the inspector
    [Export] public PackedScene FloatingTextScene;
    private Random _random = new Random();

    public override void _Ready()
    {
        EventBus.Instance.DialogueRequested += SpawnDialogue;
    }
    public override void _ExitTree()
    {
        EventBus.Instance.DialogueRequested -= SpawnDialogue;
    }

    public void SpawnDialogue(Vector3 spawnPosition, string text, float lifetime, Color textColor)
    {
        if (FloatingTextScene == null)
        {
            GD.PrintErr("FloatingTextScene is not assigned in the DialogueManager!");
            return;
        }

        // Instantiate the scene
        FloatingText3D textInstance = FloatingTextScene.Instantiate<FloatingText3D>();

        // Add it to the main tree (or current scene) so it processes
        AddChild(textInstance);

        // Configure its unique properties BEFORE it starts moving
        textInstance.GlobalPosition = spawnPosition;
        textInstance.Lifetime = lifetime;
        textInstance.Text = text;
        textInstance.Modulate = textColor.A == 0 ? Colors.White : textColor;
    }
}