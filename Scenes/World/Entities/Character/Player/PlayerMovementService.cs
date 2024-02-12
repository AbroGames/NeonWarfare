using Godot;

[GameService]
public class PlayerMovementService
{
    public PlayerMovementService()
    {
        //Root.Instance.EventBus.Subscribe<PlayerPhysicsProcessEvent>(OnPlayerPhysicsProcessEvent);
    }
    
    [GameEventListener]
    public void OnPlayerPhysicsProcessEvent(PlayerPhysicsProcessEvent playerPhysicsProcessEvent) {
        MoveByKeyboard(playerPhysicsProcessEvent.Player, playerPhysicsProcessEvent.Delta);
    }
    
    public void MoveByKeyboard(Player player, double delta)
    {
        var movementInput = GetInput();
        player.MoveAndCollide(movementInput * player.MovementSpeed * delta);
    }
    
    public Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
}