public interface IPlayerState
{
    void Enter(PlayerStateMachine ctx);
    void Execute();
    void FixedExecute();
    void Exit();
}
