using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FluidGPU : MonoBehaviour, ICoolEffectState
{
    public ComputeShader fluidCompute;
    public RawImage rawImage;
    public int size = 128;
    public float diff = 0.2f;
    public float colorDecay = 0.99f;
    public float velocityDecay = 0.99f;
    public int jacobiIterations = 20;

    [Header("Interaction")]
    public float forceStrength = 500f;
    public float colorStrength = 1f;
    public float radius = 0.03f;
    public Color paintColor = Color.cyan;


    [Header("Source")]
    public bool enableSource = true;
    public Vector2 sourceUV = new Vector2(0.5f, 0.1f);
    public Vector2 sourceVelocity = new Vector2(0f, 2f);
    public Color sourceColor = new Color(1f, 0.5f, 0.1f, 1f);
    public float sourceRadius = 0.04f;


    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CoolEffectState SimulationState { get; private set; } = CoolEffectState.Stopped;
    float stateChangeTime;
    float stateAlpha;


    RenderTexture color, colorPrev;
    RenderTexture velocity, velocityPrev;
    RenderTexture pressure, pressurePrev;

    Vector2 lastMousePos;
    bool isDragging;


    void Start()
    {
        color = CreateRT(RenderTextureFormat.ARGBFloat);
        colorPrev = CreateRT(RenderTextureFormat.ARGBFloat);
        velocity = CreateRT(RenderTextureFormat.RGFloat);
        velocityPrev = CreateRT(RenderTextureFormat.RGFloat);
        pressure = CreateRT(RenderTextureFormat.RGFloat);
        pressurePrev = CreateRT(RenderTextureFormat.RGFloat);

        rawImage.texture = color;
    }


    RenderTexture CreateRT(RenderTextureFormat renderTextureFormat)
    {
        RenderTexture rt = new(size, size, 0, renderTextureFormat)
        {
            enableRandomWrite = true
        };
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    void Update()
    {

        UpdateStateAlpha();

        if (SimulationState == CoolEffectState.Stopped)
            return;

        if (enableSource)
        {
            Inject(
                sourceUV,
                sourceVelocity * Time.deltaTime,
                sourceColor,
                sourceRadius
            );
        }

        HandleInput();

        // Advect Velocity
        Dispatch("AdvectVelocity");
        Swap(ref velocity, ref velocityPrev);

        // Diffuse Velocity
        Dispatch("Diffuse");
        Swap(ref velocity, ref velocityPrev);

        // Pressure Solve
        ClearRT(pressure);
        for (int i = 0; i < jacobiIterations; i++)
        {
            Dispatch("Jacobi");
            Swap(ref pressure, ref pressurePrev);
        }

        // Subtract Gradient
        Dispatch("SubtractGradient");

        // Advect Color
        Dispatch("AdvectColor");

        // Color Decay
        Dispatch("Decay");
        Swap(ref color, ref colorPrev);

        rawImage.texture = color;

    }

    void Dispatch(string kernelName)
    {
        int kernel = fluidCompute.FindKernel(kernelName);
        fluidCompute.SetInt("size", size);
        fluidCompute.SetFloat("dt", Time.deltaTime);
        fluidCompute.SetFloat("diff", diff);
        fluidCompute.SetFloat("colorDecay", colorDecay);
        fluidCompute.SetFloat("velocityDecay", velocityDecay);

        fluidCompute.SetTexture(kernel, "Color", color);
        fluidCompute.SetTexture(kernel, "ColorPrev", colorPrev);
        fluidCompute.SetTexture(kernel, "Velocity", velocity);
        fluidCompute.SetTexture(kernel, "VelocityPrev", velocityPrev);
        fluidCompute.SetTexture(kernel, "Pressure", pressure);
        fluidCompute.SetTexture(kernel, "PressurePrev", pressurePrev);

        int groups = Mathf.CeilToInt(size / 8f);
        fluidCompute.Dispatch(kernel, groups, groups, 1);
    }

    void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    void ClearRT(RenderTexture rt)
    {
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = null;
    }

    void Inject(Vector2 uv, Vector2 force, Color injectColor, float radius)
    {
        int kernel = fluidCompute.FindKernel("Inject");

        fluidCompute.SetInt("size", size);
        fluidCompute.SetVector("injectPos", uv);
        fluidCompute.SetVector("injectForce", force);
        fluidCompute.SetVector("injectColor", injectColor);
        fluidCompute.SetFloat("injectRadius", radius);


        int groups = Mathf.CeilToInt(size / 8f);
        // Inject into CURRENT
        fluidCompute.SetTexture(kernel, "Color", this.color);
        fluidCompute.SetTexture(kernel, "Velocity", this.velocity);
        fluidCompute.Dispatch(kernel, groups, groups, 1);

        // Inject into PREVIOUS
        fluidCompute.SetTexture(kernel, "Color", this.colorPrev);
        fluidCompute.SetTexture(kernel, "Velocity", this.velocityPrev);
        fluidCompute.Dispatch(kernel, groups, groups, 1);

    }

    public void ResetSimulation()
    {
        // Clear all render textures
        ClearRT(color);
        ClearRT(colorPrev);
        ClearRT(velocity);
        ClearRT(velocityPrev);
        ClearRT(pressure);
        ClearRT(pressurePrev);
    }


    void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            lastMousePos = mouse.position.ReadValue();
            isDragging = true;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (!isDragging) return;

        Vector2 pos = mouse.position.ReadValue();
        Vector2 delta = pos - lastMousePos;
        lastMousePos = pos;

        Vector2 uv = new(
            pos.x / Screen.width,
            pos.y / Screen.height
        );

        Vector2 force = delta * forceStrength * Time.deltaTime;
        Inject(uv, force, paintColor, radius);
    }


    public void StartCoolEffect()
    {
        if (SimulationState == CoolEffectState.Running ||
            SimulationState == CoolEffectState.Starting)
        {
            return;
        }

        ResetSimulation();
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
                    Debug.Log($"Simulation {nameof(FluidGPU)} is now running");
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
                    Debug.Log($"Simulation {nameof(FluidGPU)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case CoolEffectState.Stopped:
                stateAlpha = 0f;
                break;
        }

        if (stateAlpha <= 0f)
        {
            rawImage.color = transparent;
        }
        else if (stateAlpha >= 1f)
        {
            rawImage.color = Color.white;
        }
        else
        {
            rawImage.color = new Color(1, 1, 1, stateAlpha);
        }
    }
    Color transparent = new(1, 1, 1, 0);


}
