public interface ISimulation
{
    public SimulationState SimulationState { get; }
    public void StartSimulation();
    public void StopSimulation();
}
