using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;


public class FireworksController : MonoBehaviour, ICoolEffectState
{


    public Image background;
    public ParticleSystem fireworks;




    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CoolEffectState SimulationState { get; private set; } = CoolEffectState.Stopped;
    float stateChangeTime;
    float stateAlpha;

    void Start()
    {
        FullyStopParticleSystem();
    }

    private void FullyStopParticleSystem()
    {
        fireworks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        UpdateStateAlpha();

        if (SimulationState == CoolEffectState.Stopped)
            return;
    }


    public void StartCoolEffect()
    {
        if (SimulationState == CoolEffectState.Running ||
            SimulationState == CoolEffectState.Starting)
        {
            return;
        }

        fireworks.Play(true);
        SimulationState = CoolEffectState.Starting;
        stateChangeTime = Time.time;
    }

    public void StopCoolEffect()
    {
        if (SimulationState == CoolEffectState.Stopped ||
            SimulationState == CoolEffectState.Stopping)
        {
            return;
        }

        SimulationState = CoolEffectState.Stopping;
        stateChangeTime = Time.time;
    }


    void UpdateStateAlpha()
    {
        float t = Mathf.Clamp01((Time.time - stateChangeTime) / transitionDuration);

        switch (SimulationState)
        {
            case CoolEffectState.Starting:
                stateAlpha = t;
                if (t >= 1)
                {
                    Debug.Log($"Simulation {nameof(FireworksController)} is now running");
                    SimulationState = CoolEffectState.Running;
                }
                break;
            case CoolEffectState.Running:
                stateAlpha = 1f;
                break;
            case CoolEffectState.Stopping:
                stateAlpha = 1f - t;
                if (t >= 1)
                {
                    SimulationState = CoolEffectState.Stopped;
                    Debug.Log($"Simulation {nameof(FireworksController)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case CoolEffectState.Stopped:
                stateAlpha = 0f;
                FullyStopParticleSystem();
                break;
        }

        if (stateAlpha <= 0f)
        {
            background.color = transparent;
        }
        else if (stateAlpha >= 1f)
        {
            background.color = Color.white;
        }
        else
        {
            SetParticelSystemAlpha(stateAlpha);
            background.color = new Color(1, 1, 1, stateAlpha);
        }
    }
    Color transparent = new(1, 1, 1, 0);


    public void SetParticelSystemAlpha(float alpha)
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[fireworks.particleCount];
        int count = fireworks.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            Color c = particles[i].startColor;
            c.a = alpha;
            particles[i].startColor = c;
        }

        fireworks.SetParticles(particles, count);
    }
}
