using UnityEngine;
using UnityEngine.UI;

public class FlowFieldGpu : MonoBehaviour
{
    public ComputeShader shader;
    public RawImage rawImage;

    public int width = 768;
    public int height = 512;
    public int particleCount = 100_000;

    [Header("Noise Settings")]
    public int octaves = 4;
    public float frequency = 1f;
    public float lacunarity = 2f;
    public float gain = 0.5f;
    public float changeSpeed = 0.5f;

    [Header("Particle settings")]
    public float curlStrength = 1f;
    public float speed = 50f;
    public float particleSize = 3f; 
    public float fade = 0.97f;
    public Color[] colorPalette = new Color[]
    {
        new Color(1f, 0f, 0f, 1f),   // Red
        new Color(1f, 0.5f, 0f, 1f), // Orange
        new Color(1f, 1f, 0f, 1f),   // Yellow
        new Color(0f, 1f, 0f, 1f),   // Green
        new Color(0f, 1f, 1f, 1f),   // Cyan
        new Color(0f, 0f, 1f, 1f),   // Blue
        new Color(0.5f, 0f, 1f, 1f), // Purple
        new Color(1f, 0f, 1f, 1f)    // Magenta
    };

    RenderTexture target;
    ComputeBuffer particleA, particleB;
    ComputeBuffer paletteBuffer;
    bool swap;

    struct Particle
    {
        public Vector2 pos;
        public Vector2 vel;
    }

    void Start()
    {
        target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        target.Create();

        rawImage.texture = target;

        Particle[] particles = new Particle[particleCount];
        for (int i = 0; i < particles.Length; i++)
            particles[i].pos = new Vector2(Random.value * width, Random.value * height);
        particleA = new ComputeBuffer(particleCount, sizeof(float) * 4);
        particleB = new ComputeBuffer(particleCount, sizeof(float) * 4);
        particleA.SetData(particles);

        // Create palette buffer
        paletteBuffer = new ComputeBuffer(colorPalette.Length, sizeof(float) * 4);
        Vector4[] paletteData = new Vector4[colorPalette.Length];
        for (int i = 0; i < colorPalette.Length; i++)
            paletteData[i] = colorPalette[i];
        paletteBuffer.SetData(paletteData);
    }

    void Update()
    {
        // Set parameters
        shader.SetVector("resolution", new Vector2(width, height));
        shader.SetFloat("time", Time.time * changeSpeed);
        shader.SetFloat("deltaTime", Time.deltaTime * 10f);
        shader.SetInt("octaveCount", octaves);
        shader.SetFloat("frequency", frequency);
        shader.SetFloat("lacunarity", lacunarity);
        shader.SetFloat("gain", gain);
        shader.SetFloat("curlStrength", curlStrength);
        shader.SetFloat("speed", speed);
        shader.SetFloat("particleSize", particleSize);
        shader.SetFloat("fade", this.fade);
        shader.SetInt("paletteSize", colorPalette.Length);


        int fadeKernel = shader.FindKernel("Fade");
        shader.SetTexture(fadeKernel, "Result", target);
        shader.Dispatch(fadeKernel, width / 8, height / 8, 1);

        int kernel = shader.FindKernel("UpdateParticles");
        shader.SetTexture(kernel, "Result", target);
        shader.SetBuffer(kernel, "particlesRead", swap ? particleB : particleA);
        shader.SetBuffer(kernel, "particlesWrite", swap ? particleA : particleB);
        shader.SetBuffer(kernel, "palette", paletteBuffer);

        shader.Dispatch(kernel, particleCount / 256, 1, 1);

        swap = !swap;
        //RenderFBMTexture();
    }

    public void RenderFBMTexture()
    {
        int kernel = shader.FindKernel("RenderFBM");
        shader.SetTexture(kernel, "Result", target);
        shader.Dispatch(kernel, width / 8, height / 8, 1);
    }

    void OnDestroy()
    {
        particleA.Release();
        particleB.Release();
        paletteBuffer.Release();
    }
}
