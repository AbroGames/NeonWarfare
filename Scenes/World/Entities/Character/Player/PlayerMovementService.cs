using Godot;

public class PlayerMovementService
{
    public PlayerMovementService()
    {
        Root.Instance.EventBus.Subscribe<PlayerPhysicsUpdateEvent>(OnPlayerPhysicsUpdateEvent);
    }
    
    public void OnPlayerPhysicsUpdateEvent(PlayerPhysicsUpdateEvent playerPhysicsUpdateEvent) {
        MoveByKeyboard(playerPhysicsUpdateEvent.Player, playerPhysicsUpdateEvent.Delta);
    }
    
    private void MoveByKeyboard(Player player, double delta)
    {
        var movementInput = GetInput();
        player.MoveAndCollide(movementInput * player.MovementSpeed * delta);
    }
    
    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
}