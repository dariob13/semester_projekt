using System.Collections.Generic;
using UnityEngine;

public class LiquidSimulation : MonoBehaviour
{
    [Header("Particle Settings")]
    public GameObject particlePrefab;
    public int particleCount = 30;
    public float particleSize = 0.15f;
    public Color liquidColor = new Color(0.2f, 0.5f, 1f, 0.9f);

    [Header("Physics Settings")]
    public float springStrength = 50f;
    public float springDamping = 8f;
    public float restDistance = 0.4f;
    public float maxConnectionDistance = 1.2f;
    public float viscosity = 0.8f;
    public float groundFriction = 0.3f;

    [Header("Environment")]
    public LayerMask environmentLayer;
    public LayerMask movableLayer;
    public float gravity = 9.81f;

    [Header("Player Control")]
    public float controlForce = 30f;
    public float controlRadius = 2f;

    private List<LiquidParticle> particles = new List<LiquidParticle>();
    private List<SpringConnection> connections = new List<SpringConnection>();
    private float initialSpringStrength;
    private Vector2 blobCenter;
    private MatterState currentState = MatterState.Liquid;

    private class SpringConnection
    {
        public LiquidParticle particleA;
        public LiquidParticle particleB;
        public float restLength;

        public SpringConnection(LiquidParticle a, LiquidParticle b, float rest)
        {
            particleA = a;
            particleB = b;
            restLength = rest;
        }
    }

    void Start()
    {
        initialSpringStrength = springStrength;
        CreateLiquidBlob();
    }

    void CreateLiquidBlob()
    {
        float radius = Mathf.Sqrt(particleCount) * particleSize;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (i / (float)particleCount) * Mathf.PI * 2f;
            float distance = Random.Range(0f, radius);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            GameObject particleObj;

            if (particlePrefab != null)
            {
                particleObj = Instantiate(particlePrefab, transform.position + (Vector3)offset, Quaternion.identity, transform);
            }
            else
            {
                particleObj = new GameObject("Particle");
                particleObj.transform.SetParent(transform);
                particleObj.transform.position = transform.position + (Vector3)offset;
            }

            particleObj.transform.localScale = Vector3.one * particleSize;

            LiquidParticle particle = particleObj.GetComponent<LiquidParticle>();
            if (particle == null)
            {
                particle = particleObj.AddComponent<LiquidParticle>();
            }

            particle.Initialize(this, liquidColor);
            particles.Add(particle);
        }

        UpdateConnections();
    }

    void UpdateConnections()
    {
        connections.Clear();

        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                float distance = Vector2.Distance(particles[i].transform.position, particles[j].transform.position);

                if (distance < maxConnectionDistance)
                {
                    connections.Add(new SpringConnection(particles[i], particles[j], restDistance));
                }
            }
        }
    }

    void FixedUpdate()
    {
        UpdateBlobCenter();

        foreach (var connection in connections)
        {
            Vector2 delta = connection.particleB.transform.position - connection.particleA.transform.position;
            float distance = delta.magnitude;

            if (distance > 0.01f)
            {
                Vector2 direction = delta / distance;
                float stretch = distance - connection.restLength;

                Vector2 springForce = direction * stretch * springStrength;

                Vector2 relativeVelocity = connection.particleB.velocity - connection.particleA.velocity;
                Vector2 dampingForce = relativeVelocity * springDamping;

                Vector2 totalForce = springForce + dampingForce;

                connection.particleA.ApplyForce(totalForce);
                connection.particleB.ApplyForce(-totalForce);
            }
        }

        ApplyViscosity();

        LayerMask collisionMask = currentState == MatterState.Solid
            ? (environmentLayer | movableLayer)
            : environmentLayer;

        // Gas has reversed/reduced gravity
        float currentGravity = gravity;
        if (currentState == MatterState.Gas)
        {
            LiquidSolidForm form = GetComponent<LiquidSolidForm>();
            if (form == null) form = FindObjectOfType<LiquidSolidForm>();
            if (form != null)
            {
                currentGravity = gravity * form.GetGasGravityMultiplier();
            }
        }

        foreach (var particle in particles)
        {
            particle.ApplyGravity(currentGravity);

            if (particle.IsGrounded())
            {
                particle.velocity.x *= (1f - groundFriction * Time.fixedDeltaTime);
            }

            particle.UpdatePhysics(Time.fixedDeltaTime);
            particle.CheckEnvironmentCollision(collisionMask);
        }

        if (Time.frameCount % 20 == 0)
        {
            UpdateConnections();
        }
    }

    void UpdateBlobCenter()
    {
        if (particles.Count == 0) return;

        blobCenter = Vector2.zero;
        foreach (var particle in particles)
        {
            blobCenter += (Vector2)particle.transform.position;
        }
        blobCenter /= particles.Count;
    }

    void ApplyViscosity()
    {
        // Gas has much higher viscosity (sludgy)
        float currentViscosity = currentState == MatterState.Gas ? viscosity * 3f : viscosity;

        for (int i = 0; i < particles.Count; i++)
        {
            Vector2 avgVelocity = Vector2.zero;
            int neighbors = 0;

            for (int j = 0; j < particles.Count; j++)
            {
                if (i != j)
                {
                    float distance = Vector2.Distance(particles[i].transform.position, particles[j].transform.position);
                    if (distance < restDistance * 2.5f)
                    {
                        avgVelocity += particles[j].velocity;
                        neighbors++;
                    }
                }
            }

            if (neighbors > 0)
            {
                avgVelocity /= neighbors;
                Vector2 viscosityForce = (avgVelocity - particles[i].velocity) * currentViscosity;
                particles[i].ApplyForce(viscosityForce);
            }
        }
    }

    public void SetMatterState(MatterState state)
    {
        currentState = state;
    }

    // Keep backward compatibility
    public void SetSolidState(bool solid)
    {
        currentState = solid ? MatterState.Solid : MatterState.Liquid;
    }

    public void ApplyDirectionalForce(Vector2 direction, float strength)
    {
        foreach (var particle in particles)
        {
            particle.ApplyForce(direction * strength);
        }
    }

    public void ApplyCohesion(float strength)
    {
        if (particles.Count == 0) return;

        foreach (var particle in particles)
        {
            Vector2 toCenter = blobCenter - (Vector2)particle.transform.position;
            float distance = toCenter.magnitude;

            if (distance > restDistance * 0.5f)
            {
                float factor = Mathf.Clamp01(distance / (restDistance * 3f));
                particle.ApplyForce(toCenter.normalized * strength * factor);
            }
        }
    }

    public void ApplyPlayerControl(Vector2 targetPosition)
    {
        foreach (var particle in particles)
        {
            Vector2 toTarget = targetPosition - (Vector2)particle.transform.position;
            float distance = toTarget.magnitude;

            if (distance < controlRadius && distance > 0.1f)
            {
                Vector2 force = toTarget.normalized * controlForce * (1f - distance / controlRadius);
                particle.ApplyForce(force);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || particles == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        foreach (var connection in connections)
        {
            if (connection.particleA != null && connection.particleB != null)
            {
                Gizmos.DrawLine(connection.particleA.transform.position, connection.particleB.transform.position);
            }
        }
    }
}