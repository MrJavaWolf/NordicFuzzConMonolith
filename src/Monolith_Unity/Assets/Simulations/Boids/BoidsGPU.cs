using UnityEngine;
using UnityEngine.UI;

public class BoidsGPU : MonoBehaviour
{
    public ComputeShader boidsCompute;
    public RawImage rawImage;

    public int boidCount = 1024;
    public int width = 768;
    public int height = 512;
    [Header("Color Palette")]
    public Color[] colorPalette1 = new[]
    {
        Color.cyan,
        Color.magenta,
        Color.yellow,
        Color.green,
        Color.red,
        Color.blue
    };


    [Header("Boids")]
    public float neighborRadius = 20f;
    public float separationRadius = 8f;
    public float maxSpeed = 60f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 0.05f;
    public float separationWeight = 1.5f;

    [Header("Trails")]
    [Range(0, 1)] public float trailFade = 0.05f;
    [Range(1, 10)] public int trailRadius = 2;
    public bool renderBoids = true;

    ComputeBuffer boidBuffer;
    ComputeBuffer paletteBuffer;
    RenderTexture renderTexture;

    int updateKernel;
    int fadeKernel;
    int drawTrailKernel;

    struct Boid
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    void Start()
    {
        updateKernel = boidsCompute.FindKernel("UpdateBoids");
        fadeKernel = boidsCompute.FindKernel("FadeTrails");
        drawTrailKernel = boidsCompute.FindKernel("DrawTrails");

        paletteBuffer = new ComputeBuffer(colorPalette1.Length, sizeof(float) * 4);
        paletteBuffer.SetData(colorPalette1);

        foreach (int k in new[] { drawTrailKernel })
        {
            boidsCompute.SetBuffer(k, "palette", paletteBuffer);
        }
        boidsCompute.SetInt("paletteCount", colorPalette1.Length);

        Boid[] boids = new Boid[boidCount];
        for (int i = 0; i < boidCount; i++)
        {
            boids[i].position = new Vector2(
                Random.value * width,
                Random.value * height
            );
            boids[i].velocity = Random.insideUnitCircle * 20f;
        }

        boidBuffer = new ComputeBuffer(boidCount, sizeof(float) * 4);
        boidBuffer.SetData(boids);

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        rawImage.texture = renderTexture;
        rawImage.color = Color.white; // transparency comes from texture alpha

        foreach (int k in new[] { updateKernel, drawTrailKernel })
            boidsCompute.SetBuffer(k, "boids", boidBuffer);

        boidsCompute.SetTexture(fadeKernel, "Result", renderTexture);
        boidsCompute.SetTexture(drawTrailKernel, "Result", renderTexture);
    }

    void Update()
    {
        boidsCompute.SetInt("boidCount", boidCount);
        boidsCompute.SetFloat("deltaTime", Time.deltaTime);
        boidsCompute.SetVector("resolution", new Vector2(width, height));

        boidsCompute.SetFloat("neighborRadius", neighborRadius);
        boidsCompute.SetFloat("separationRadius", separationRadius);
        boidsCompute.SetFloat("maxSpeed", maxSpeed);
        boidsCompute.SetFloat("alignmentWeight", alignmentWeight);
        boidsCompute.SetFloat("cohesionWeight", cohesionWeight);
        boidsCompute.SetFloat("separationWeight", separationWeight);

        boidsCompute.SetFloat("trailFade", trailFade);
        boidsCompute.SetFloat("trailRadius", trailRadius);
        boidsCompute.SetBool("renderBoids", renderBoids);

        boidsCompute.Dispatch(updateKernel, Mathf.CeilToInt(boidCount / 256f), 1, 1);

        boidsCompute.Dispatch(
            fadeKernel,
            Mathf.CeilToInt(width / 8f),
            Mathf.CeilToInt(height / 8f),
            1
        );

        boidsCompute.Dispatch(drawTrailKernel, Mathf.CeilToInt(boidCount / 256f), 1, 1);
    }

    void OnDestroy()
    {
        boidBuffer?.Release();
        paletteBuffer?.Release();
    }
}
