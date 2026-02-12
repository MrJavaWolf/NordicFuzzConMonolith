using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EffectRotationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EffectManager effectManager;

    [Header("Effect Groups")]
    [SerializeField] private List<CoolEffectType> mainEffects;
    [SerializeField] private List<CoolEffectType> transitionEffects;

    [Header("Durations")]
    [SerializeField] private float mainEffectDuration = 10f;
    [SerializeField] private float transitionEffectDuration = 5f;

    private enum RotationState
    {
        MainEffect,
        TransitionEffect
    }

    private RotationState rotationState;

    private float timer;
    private int mainIndex = 0;
    private int transitionIndex = 0;

    private void Start()
    {
        if (mainEffects.Count == 0 || transitionEffects.Count == 0)
        {
            Debug.LogError("Effect lists not configured!");
            enabled = false;
            return;
        }

        rotationState = RotationState.MainEffect;

        effectManager.RunCoolEffect(mainEffects[mainIndex]);
        timer = mainEffectDuration;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        AdvanceRotation();
    }

    private void AdvanceRotation()
    {
        if (rotationState == RotationState.MainEffect)
        {
            // Go to transition
            rotationState = RotationState.TransitionEffect;

            transitionIndex = (transitionIndex + 1) % transitionEffects.Count;
            effectManager.RunCoolEffect(transitionEffects[transitionIndex]);

            timer = transitionEffectDuration;
        }
        else
        {
            // Go to next main
            rotationState = RotationState.MainEffect;

            mainIndex = (mainIndex + 1) % mainEffects.Count;
            effectManager.RunCoolEffect(mainEffects[mainIndex]);

            timer = mainEffectDuration;
        }
    }
}
