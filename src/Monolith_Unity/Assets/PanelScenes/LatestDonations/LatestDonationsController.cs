using System.Collections.Generic;
using UnityEngine;

public class LatestDonationsController : MonoBehaviour, ICoolEffectState
{
    public GameObject LatestDonorPrefab;
    public GameObject SpecialDonorPrefab;

    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CoolEffectState SimulationState { get; private set; } = CoolEffectState.Stopped;
    float stateChangeTime;
    float stateAlpha;
    private readonly List<SpriteRenderer> spriteRenderers = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
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
        RefreshSpriteCache();
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
        RefreshSpriteCache();
        SimulationState = CoolEffectState.Stopping;
        stateChangeTime = Time.time;
    }


    public void RefreshSpriteCache()
    {
        spriteRenderers.Clear();
        GetComponentsInChildren(spriteRenderers);
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
                    Debug.Log($"Simulation {nameof(LatestDonationsController)} is now running");
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
                    Debug.Log($"Simulation {nameof(LatestDonationsController)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case CoolEffectState.Stopped:
                stateAlpha = 0f;
                break;
        }

        SetSpritsAlpha(stateAlpha);
    }

    private void SetSpritsAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] == null)
                continue;
            var color = spriteRenderers[i].color;
            color.a = alpha;
            spriteRenderers[i].color = color;
        }
    }
}
