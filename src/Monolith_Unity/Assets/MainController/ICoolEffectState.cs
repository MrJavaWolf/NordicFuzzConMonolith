public interface ICoolEffectState
{
    public CoolEffectState SimulationState { get; }
    public void StartCoolEffect();
    public void StopCoolEffect();
}
