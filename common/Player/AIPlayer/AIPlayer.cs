using Godot;
using System;

public partial class AIPlayer : GamePlayer
{
    Random rand = new Random();
    public override async void StartTurn()
    {
        base.StartTurn();

        float thinkingTime = (float)rand.NextDouble() * 3f + 0.5f;

        await ToSignal(GetTree().CreateTimer(thinkingTime), SceneTreeTimer.SignalName.Timeout);

        Card3D randomCardFromHand = null;
        if (Hand.Count > 0)
        {
            randomCardFromHand = Hand[rand.Next(Hand.Count)];
        }

        EmitSignal(SignalName.CardSelected, randomCardFromHand);
    }

    public override void EndTurn()
    {
        base.EndTurn();
    }
}
