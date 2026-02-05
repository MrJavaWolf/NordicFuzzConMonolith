using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SlimeSimulation : MonoBehaviour, ISimulation
{
    public ComputeShader agentCS;
    public ComputeShader diffuseCS;
    public ComputeShader rendererCS;

    public RawImage rawImage;
    public int width = 192;
    public int height = 512;
    public int numAgents = 7500;

    public float moveSpeed = 0.5f;
    public float turnAngle = 0.3f;
    public float sensorOffset = 2f;
    public float randomJitter = 0.1f;
    public float depositAmount = 0.5f;
    public float diffuseFactor = 0.3f;
    public float evapRate = 0.995f;
    public float maxValue = 50f;

    RenderTexture trailA, trailB;
    RenderTexture renderTexture;

    ComputeBuffer agentBuffer;

    public UnityEngine.Color[] paletteNeonSlime =
    {
        new(0f, 0f, 0f),
        new(30/255f, 100/255f, 200/255f),
        new(50/255f, 150/255f, 1f),
        new(80/255f, 200/255f, 1f),
        new(0f, 1f, 180/255f),
        new(0f, 1f, 140/255f),
        new(0f, 220/255f, 100/255f)
    };

    public UnityEngine.Color[] palettePlasma =
    {
        new(0f, 0f, 0f),
        new(0.3f, 0f, 0.2f),
        new(0.6f, 0.05f, 0.4f),
        new(0.9f, 0.1f, 0.2f),
        new(1f, 0.4f, 0f),
        new(1f, 0.7f, 0.2f),
        new(1f, 1f, 0.8f)
    };

    public UnityEngine.Color[] paletteBioluminescent =
    {
        new(0f, 0f, 0f),
        new(0f, 0.05f, 0.15f),
        new(0f, 0.15f, 0.35f),
        new(0f, 0.35f, 0.6f),
        new(0f, 0.7f, 0.8f),
        new(0.3f, 1f, 0.9f),
        new(0.8f, 1f, 1f)
    };

    public UnityEngine.Color[] paletteToxic =
    {
        new(0f, 0f, 0f),
        new(0.1f, 0.2f, 0f),
        new(0.2f, 0.4f, 0.05f),
        new(0.4f, 0.7f, 0.1f),
        new(0.7f, 1f, 0.2f),
        new(0.9f, 1f, 0.4f),
        new(1f, 1f, 0.7f)
    };


    public UnityEngine.Color[] paletteHeat =
    {
        new(0f, 0f, 0f),
        new(0f, 0f, 0.5f),
        new(0f, 0.5f, 1f),
        new(0f, 1f, 0f),
        new(1f, 1f, 0f),
        new(1f, 0.5f, 0f),
        new(1f, 0f, 0f)
    };

    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public SimulationState SimulationState { get; private set; } = SimulationState.Stopped;
    float stateChangeTime;
    float stateAlpha;

    struct Agent
    {
        public Vector2 position;
        public float angle;
    }

    void Start()
    {
        trailA = CreateRT();
        trailB = CreateRT();

        agentBuffer = new ComputeBuffer(numAgents, sizeof(float) * 3);
        Agent[] agents = new Agent[numAgents];

        for (int i = 0; i < numAgents; i++)
        {
            agents[i].position = new Vector2(
                Random.value * width,
                Random.value * height);
            agents[i].angle = Random.value * Mathf.PI * 2;
        }

        agentBuffer.SetData(agents);

        SetPalette(paletteNeonSlime);


        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        renderTexture.Create();

        rawImage.texture = renderTexture;
        rawImage.color = UnityEngine.Color.white;
    }

    RenderTexture CreateRT()
    {
        RenderTexture rt = new(width, height, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        rt.Create();
        return rt;
    }

    void Update()
    {

        UpdateStateAlpha();

        if (SimulationState == SimulationState.Stopped)
            return;


        var kb = Keyboard.current;
        if (kb != null)
        {

            if (kb.digit1Key.wasPressedThisFrame) SetPalette(paletteNeonSlime);
            if (kb.digit2Key.wasPressedThisFrame) SetPalette(palettePlasma);
            if (kb.digit3Key.wasPressedThisFrame) SetPalette(paletteBioluminescent);
            if (kb.digit4Key.wasPressedThisFrame) SetPalette(paletteToxic);
            if (kb.digit5Key.wasPressedThisFrame) SetPalette(paletteHeat);
        }


        int antSimulationStep = agentCS.FindKernel("CSMain");

        agentCS.SetBuffer(antSimulationStep, "agents", agentBuffer);
        agentCS.SetTexture(antSimulationStep, "trail", trailA);

        agentCS.SetFloat("moveSpeed", moveSpeed);
        agentCS.SetFloat("turnAngle", turnAngle);
        agentCS.SetFloat("sensorOffset", sensorOffset);
        agentCS.SetFloat("randomJitter", randomJitter);
        agentCS.SetFloat("depositAmount", depositAmount);
        agentCS.SetInt("width", width);
        agentCS.SetInt("height", height);
        agentCS.SetInt("numAgents", numAgents);
        agentCS.Dispatch(antSimulationStep, Mathf.CeilToInt(numAgents / 256f), 1, 1);

        int diffuseStep = diffuseCS.FindKernel("CSMain");
        diffuseCS.SetTexture(diffuseStep, "trail", trailA);
        diffuseCS.SetTexture(diffuseStep, "trailOut", trailB);
        diffuseCS.SetFloat("diffuseFactor", diffuseFactor);
        diffuseCS.SetFloat("evapRate", evapRate);
        diffuseCS.SetInt("width", width);
        diffuseCS.SetInt("height", height);
        diffuseCS.Dispatch(diffuseStep, width / 8, height / 8, 1);


        int rendererKernel = rendererCS.FindKernel("CSMain");
        rendererCS.SetTexture(rendererKernel, "_Trail", trailA);
        rendererCS.SetTexture(rendererKernel, "Result", renderTexture);
        rendererCS.SetFloat("_MaxValue", maxValue);

        rendererCS.Dispatch(rendererKernel,
            Mathf.CeilToInt(width / 8.0f),
            Mathf.CeilToInt(height / 8.0f), 1);


        // swap
        (trailA, trailB) = (trailB, trailA);
    }

    void SetPalette(UnityEngine.Color[] palette)
    {
        int rendererKernel = rendererCS.FindKernel("CSMain");
        Texture2D paletteTex = CreatePaletteTexture(palette);
        rendererCS.SetTexture(rendererKernel, "_Palette", paletteTex);
    }

    Texture2D CreatePaletteTexture(UnityEngine.Color[] colors)
    {
        int width = colors.Length;
        Texture2D tex = new Texture2D(
            width, 1,
            TextureFormat.RGBA32,
            false,
            true
        );

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

    void OnDestroy()
    {
        agentBuffer.Release();
    }


    public void StartSimulation()
    {
        if (SimulationState == SimulationState.Running ||
            SimulationState == SimulationState.Starting)
        {
            return;
        }

        SimulationState = SimulationState.Starting;
        stateChangeTime = Time.time;
    }

    public void StopSimulation()
    {
        if (SimulationState == SimulationState.Stopped ||
            SimulationState == SimulationState.Stopping)
        {
            return;
        }

        SimulationState = SimulationState.Stopping;
        stateChangeTime = Time.time;
    }


    void UpdateStateAlpha()
    {
        float t = Mathf.Clamp01((Time.time - stateChangeTime) / transitionDuration);

        switch (SimulationState)
        {
            case SimulationState.Starting:
                stateAlpha = t;
                if (t >= 1)
                {
                    Debug.Log($"Simulation {nameof(SlimeSimulation)} is now running");
                    SimulationState = SimulationState.Running;
                }
                break;
            case SimulationState.Running:
                stateAlpha = 1f;
                break;
            case SimulationState.Stopping:
                stateAlpha = 1f - t;
                if (t >= 1)
                {
                    SimulationState = SimulationState.Stopped;
                    Debug.Log($"Simulation {nameof(SlimeSimulation)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case SimulationState.Stopped:
                stateAlpha = 0f;
                break;
        }

        if (stateAlpha <= 0f)
        {
            rawImage.color = transparent;
        }
        else if (stateAlpha >= 1f)
        {
            rawImage.color = UnityEngine.Color.white;
        }
        else
        {
            rawImage.color = new UnityEngine.Color(1, 1, 1, stateAlpha);
        }
    }
    UnityEngine.Color transparent = new(1, 1, 1, 0);
}
