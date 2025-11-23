using System;

namespace NeonWarfare.Scenes.Game.Network;

public class NetworkStateMachine
{
    public enum State { NotInitialized, Connecting, Connected, Hosting, Hosted, Disconnected }
    public State CurrentState => _state;
    public bool CanInitialize => _state == State.NotInitialized;
    public bool IsInitializing => _state == State.Connecting || _state == State.Hosting;
    public bool IsActiveGameState => _state == State.Connected || _state == State.Hosted;
    public bool IsClient => _state == State.Connecting || _state == State.Connected;
    public bool IsServer => _state == State.Hosting || _state == State.Hosted;
    
    public event Action<State> StateChanged;
    
    private State _state = State.NotInitialized;

    public void SetState(State newState)
    {
        if (_state != newState)
        {
            _state = newState;
            StateChanged?.Invoke(_state);
        }
    }

}