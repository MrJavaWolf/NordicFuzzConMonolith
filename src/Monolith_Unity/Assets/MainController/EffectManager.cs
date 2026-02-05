using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField] private List<SimulationEntry> simulations;

    private Dictionary<SimulationType, SimulationEntry> simulationMap;

    private SimulationEntry current;
    private SimulationEntry changeToSimulation;

    private void Awake()
    {
        simulationMap = new Dictionary<SimulationType, SimulationEntry>();

        foreach (var sim in simulations)
        {
            sim.Initialize();
            simulationMap.Add(sim.type, sim);
        }
    }

    public void Start()
    {
        RunSimulation(SimulationType.Fluid);
    }

    public void RunSimulation(SimulationType type)
    {
        if (current != null &&
            current.type == type &&
            (current.simulation.SimulationState == SimulationState.Running ||
            current.simulation.SimulationState == SimulationState.Starting))
        {
            Debug.Log($"Is already on simulation type: {type}");
            return;
        }

        Debug.Log($"Changes simulation to type: {type}");
        changeToSimulation = simulationMap[type];
    }

    public void Update()
    {
        if (changeToSimulation != null)
        {
            if (current == null ||
                current.simulation.SimulationState == SimulationState.Stopped)
            {

                current = changeToSimulation;
                Debug.Log($"Starts simulation: {current.type}");
                current.simulation.StartSimulation();
                changeToSimulation = null;
            }
            else if (current.simulation.SimulationState == SimulationState.Running ||
                current.simulation.SimulationState == SimulationState.Starting)
            {
                Debug.Log($"Stops simulation: {current.type}");
                current.simulation.StopSimulation();
            }
            else if (current.simulation.SimulationState == SimulationState.Stopping)
            {
                // Do nothing waiting for the current simulation to stop
            }
        }
    }
}



[System.Serializable]
public class SimulationEntry
{
    public SimulationType type;
    public GameObject root;

    [HideInInspector] public ISimulation simulation;

    public void Initialize()
    {
        simulation = root.GetComponent<ISimulation>();
    }
}


public enum SimulationType
{
    Fluid,
    Boids,
    Flow,
    Ant
}
