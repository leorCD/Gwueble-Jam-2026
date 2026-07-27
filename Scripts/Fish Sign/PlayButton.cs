using Godot;
using System;

public partial class PlayButton : Area3D, IInteractable
{
    public void Interact()
    {
        ChangeScene("res://scenes/Day1.tscn");
    }

    private void ChangeScene(string path)
    {
        Error e = GetTree().ChangeSceneToFile(path);

        if (e != Error.Ok)
        {
            GD.PrintErr($"Scene failed to load : {e}");
        }
    }
}
