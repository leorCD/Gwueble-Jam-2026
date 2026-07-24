using Godot;
using System;

public partial class CameraTilt : Camera3D
{
    private float maxTilt = 22f;
    private Vector3 targetRotation = Vector3.Zero;
    private IInteractable interactable;
    public override void _UnhandledInput(InputEvent @event)
    {
        // mouse tilt
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2 screenSize = GetViewport().GetVisibleRect().Size;
            Vector2 screenCenter = screenSize / 2;

            // RAW, untouched relative vector
            // dividing by the screen center after subtracting gives us pos on a uniform scale of -1to1
            Vector2 mousePositionRelativeToCenter = (mouseMotion.Position - screenCenter) / screenCenter;

            targetRotation = new Vector3(-mousePositionRelativeToCenter.Y * maxTilt, -mousePositionRelativeToCenter.X * maxTilt, 0);
        }

        // mouse click
        if (@event is InputEventMouseButton mousePressed && mousePressed.Pressed)
        {
            if (mousePressed.ButtonIndex == MouseButton.Left)
            {
                if (interactable == null) return;
                interactable.Interact();
            }
        }
    }

    private Vector3 originRotation = new Vector3(0, -180, 0);
    public override void _Process(double delta)
    {
        RotationDegrees = RotationDegrees.Lerp(originRotation + targetRotation, 5f * (float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        ShootRay();
    }



    // private IInteractable interactable;
    public void ShootRay()
    {
        Vector2 mousePositionOnScreen = GetViewport().GetMousePosition();
        float rayLength = 100f;

        Vector3 rayOrigin = ProjectRayOrigin(mousePositionOnScreen);
        Vector3 rayDirection = ProjectRayNormal(mousePositionOnScreen);
        Vector3 rayEnd = rayOrigin + rayDirection * rayLength;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.CollideWithAreas = true;

        PhysicsDirectSpaceState3D directSpaceState = GetViewport().GetWorld3D().DirectSpaceState;
        Godot.Collections.Dictionary result = directSpaceState.IntersectRay(query);


        if (result.Count > 0) // targets
        {   
            Node collider = result["collider"].As<Node>();
            interactable = collider as IInteractable;
        }
        else // no targets
        {
            interactable = null;
        }
    }
}
