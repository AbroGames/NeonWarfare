using AbroDraft.Scripts;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.Scenes.World.Entities.Character.Player;

[GameService]
public class PlayerMovementService
{
    
    [GameEventListener]
    public void OnPlayerPhysicsProcessEvent(PlayerPhysicsProcessEvent playerPhysicsProcessEvent) {
        var (player, delta) = playerPhysicsProcessEvent;
        
        var movementInput = GetInput();
        player.MoveAndCollide(movementInput * player.MovementSpeed * delta);
    }
    
    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
}