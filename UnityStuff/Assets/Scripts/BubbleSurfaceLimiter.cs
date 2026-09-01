using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(ParticleSystem))]
public class BubbleSurfaceLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterBodyController waterController;
    [SerializeField] private Transform waterBody;

    [Header("Boiling")]
    [SerializeField] private bool isBoiling = true;

    [Header("Bubble Settings")]
    [SerializeField, Min(0.001f)]
    private float bubbleRiseSpeed = 0.03f;

    [SerializeField, Min(0.0001f)]
    private float bubbleSize = 0.004f;

    [SerializeField, Min(0.001f)]
    private float emissionRadius = 0.023f;

    [SerializeField, Range(0f, 1f)]
    private float surfaceCutoff = 0.8f;

    private ParticleSystem bubbleSystem;
    private ParticleSystem.Particle[] particles;

    private void Awake()
    {
        bubbleSystem = GetComponent<ParticleSystem>();

        particles = new ParticleSystem.Particle[
            bubbleSystem.main.maxParticles
        ];

        ConfigureParticleSystem();

        bubbleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void Update()
    {
        if (waterController == null || waterBody == null)
        {
            StopAndClear();
            return;
        }

        bool containsWater = waterController.FillAmount > 0f;

        if (!isBoiling || !containsWater)
        {
            StopAndClear();
            return;
        }

        ConfigureParticleSystem();

        if (!bubbleSystem.isPlaying)
        {
            bubbleSystem.Play();
        }
    }

    private void LateUpdate()
    {
        if (bubbleSystem == null ||
            waterBody == null ||
            !bubbleSystem.isPlaying)
        {
            return;
        }

        RemoveParticlesAtSurface();
    }

    private void ConfigureParticleSystem()
    {
        if (bubbleSystem == null || waterBody == null)
        {
            return;
        }

        Vector3 waterBottomWorld =
            waterBody.TransformPoint(Vector3.down);

        Vector3 waterTopWorld =
            waterBody.TransformPoint(Vector3.up);

        float currentWaterHeight = Vector3.Distance(
            waterBottomWorld,
            waterTopWorld
        );

        // Keep the emitter at the current water bottom.
        transform.position = waterBottomWorld;

        // A Cone emits along its local Z axis.
        transform.rotation = Quaternion.LookRotation(
            waterBody.up,
            waterBody.forward
        );

        ParticleSystem.MainModule main = bubbleSystem.main;

        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = bubbleRiseSpeed;
        main.startSize = bubbleSize;
        main.gravityModifier = 0f;

        // Distance from cylinder bottom (-1) to the cutoff.
        float travelFraction =
            (surfaceCutoff + 1f) * 0.5f;

        float permittedTravelDistance =
            currentWaterHeight * travelFraction;

        main.startLifetime = Mathf.Max(
            0.05f,
            permittedTravelDistance / bubbleRiseSpeed
        );

        ParticleSystem.ShapeModule shape = bubbleSystem.shape;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2f;
        shape.radius = emissionRadius;
        shape.radiusThickness = 1f;
        shape.length = 0.001f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = Vector3.one;
    }

    private void RemoveParticlesAtSurface()
    {
        int particleCount =
            bubbleSystem.GetParticles(particles);

        for (int i = 0; i < particleCount; i++)
        {
            // Simulation Space is World, so position is world-space.
            Vector3 positionInsideWater =
                waterBody.InverseTransformPoint(
                    particles[i].position
                );

            // Unity cylinder extends from local Y -1 to +1.
            if (positionInsideWater.y >= surfaceCutoff)
            {
                particles[i].remainingLifetime = 0f;
            }
        }

        bubbleSystem.SetParticles(
            particles,
            particleCount
        );
    }

    private void StopAndClear()
    {
        if (bubbleSystem == null)
        {
            return;
        }

        bubbleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    public void SetBoiling(bool boiling)
    {
        isBoiling = boiling;

        if (!isBoiling)
        {
            StopAndClear();
        }
    }
}