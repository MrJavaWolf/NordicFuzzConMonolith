using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public CoolEffectType StartEffect;
    [SerializeField] private float blackWaitTimeBetweenEffects = 3f;
    [SerializeField] private List<CoolEffectEntry> simulations;
    private Dictionary<CoolEffectType, CoolEffectEntry> simulationMap;

    private CoolEffectEntry current;
    private CoolEffectEntry changeToSimulation;

    private float lastStoppingTime = 0;
    private void Awake()
    {
        simulationMap = new Dictionary<CoolEffectType, CoolEffectEntry>();
        foreach (var sim in simulations)
        {
            sim.Initialize();
            simulationMap.Add(sim.type, sim);
        }
    }


    public void Start()
    {
        RunCoolEffect(StartEffect);
        Application.targetFrameRate = 25;
    }

    public void RunCoolEffect(CoolEffectType type)
    {
        if (current != null &&
            current.type == type &&
            (current.CoolEffect.SimulationState == CoolEffectState.Running ||
            current.CoolEffect.SimulationState == CoolEffectState.Starting))
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
                (current.CoolEffect.SimulationState == CoolEffectState.Stopped && Time.time - lastStoppingTime > blackWaitTimeBetweenEffects))
            {

                current = changeToSimulation;
                Debug.Log($"Starts simulation: {current.type}");
                current.CoolEffect.StartCoolEffect();
                changeToSimulation = null;
            }
            else if (current.CoolEffect.SimulationState == CoolEffectState.Running ||
                current.CoolEffect.SimulationState == CoolEffectState.Starting)
            {
                Debug.Log($"Stops simulation: {current.type}");
                current.CoolEffect.StopCoolEffect();
            }
            else if (current.CoolEffect.SimulationState == CoolEffectState.Stopping)
            {
                // Do nothing waiting for the current simulation to stop
                lastStoppingTime = Time.time;
            }
        }
    }
}



[System.Serializable]
public class CoolEffectEntry
{
    public CoolEffectType type;
    public GameObject root;

    [HideInInspector] public ICoolEffectState CoolEffect;

    public void Initialize()
    {
        CoolEffect = root.GetComponent<ICoolEffectState>();
    }
}


public enum CoolEffectType
{
    Fluid = 0,
    Boids = 1,
    Flow = 2,
    Ant = 3,
    Fireworks = 4,
    TotalAmountDonated = 100,
    LatestDonors = 101,
}

