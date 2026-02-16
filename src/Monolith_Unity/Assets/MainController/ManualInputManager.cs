using UnityEngine;
using UnityEngine.InputSystem;

public class ManualInputManager : MonoBehaviour
{
    private EffectManager effectManager;

    private void Awake()
    {
        effectManager = GetComponent<EffectManager>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.Fluid);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.Boids);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.Flow);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.Ant);

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.Fireworks);

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.TotalAmountDonated);
        
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            effectManager.RunCoolEffect(CoolEffectType.LatestDonors);
    }
}
