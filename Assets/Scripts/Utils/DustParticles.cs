using UnityEngine;

public class DustParticles : MonoBehaviour
{
    [Header("Particle Configuration")]
    public int maxParticles = 120;
    public float emissionRate = 18f;
    public float minSize = 0.005f;
    public float maxSize = 0.02f;
    public float minLifetime = 4f;
    public float maxLifetime = 8f;
    public float minSpeed = 0.01f;
    public float maxSpeed = 0.05f;

    [Header("Spawn Volume")]
    public Vector3 spawnBoxScale = new Vector3(6f, 5f, 6f);

    private Camera playerCamera;
    private ParticleSystem particleSystemComponent;
    private ParticleSystemRenderer particleRenderer;

    void Start()
    {
        // Find player camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogWarning("DustParticles: No camera found. Spawning particles relative to Player transform.");
        }

        CreateParticleSystem();
    }

    private void CreateParticleSystem()
    {
        // 1. Create a child GameObject for particles
        GameObject psObject = new GameObject("PlayerDustMotes");
        // Attach to the camera or player so the emitter follows the player
        psObject.transform.SetParent(playerCamera != null ? playerCamera.transform : transform, false);
        psObject.transform.localPosition = new Vector3(0, 0, 2f); // Offset slightly forward in front of the camera

        // 2. Add and configure ParticleSystem
        particleSystemComponent = psObject.AddComponent<ParticleSystem>();

        // Set Main Module
        var main = particleSystemComponent.main;
        main.maxParticles = maxParticles;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.22f); // Faint white/grey particles
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Emitter moves, but particles stay in World space!
        main.playOnAwake = true;

        // Set Emission Module
        var emission = particleSystemComponent.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        // Set Shape Module (Box shape in front of the player)
        var shape = particleSystemComponent.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = spawnBoxScale;

        // Set Velocity Over Lifetime (gentle drift)
        var velocity = particleSystemComponent.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

        // Set Noise Module (organic floating movement)
        var noise = particleSystemComponent.noise;
        noise.enabled = true;
        noise.strength = 0.04f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;

        // 3. Configure Renderer and Material (using URP Lit shader so flashlight lights it up)
        particleRenderer = psObject.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            // Try URP Simple Lit shader first
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (particleShader == null)
            {
                // Fallbacks if URP is not set up correctly or using standard
                particleShader = Shader.Find("Particles/Standard Unlit");
            }
            if (particleShader == null)
            {
                particleShader = Shader.Find("Sprites/Default");
            }

            Material mat = new Material(particleShader);
            
            // Configure material properties for URP Transparent Blend mode
            if (particleShader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Surface", 1f); // 1 = Transparent surface type
                mat.SetFloat("_Blend", 0f); // 0 = Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            // The shader will naturally use its default built-in soft particle texture if we don't override it.

            particleRenderer.material = mat;
        }

        particleSystemComponent.Play();
    }
}
