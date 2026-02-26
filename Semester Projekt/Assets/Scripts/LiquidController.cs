using UnityEngine;

public class LiquidController : MonoBehaviour
{
    private LiquidSimulation simulation;
    private LiquidSolidForm solidForm;

    [Header("Keyboard Control")]
    public float moveForce = 120f;
    public float cohesionForce = 40f;
    public float wobbleStrength = 15f;

    [Header("Jump Settings")]
    public float jumpForce = 200f;
    public float jumpCooldown = 0.5f;

    private float wobbleTimer = 0f;
    private float jumpTimer = 0f;

    void Start()
    {
        simulation = GetComponent<LiquidSimulation>();
        if (simulation == null)
        {
            simulation = gameObject.AddComponent<LiquidSimulation>();
        }

        solidForm = GetComponent<LiquidSolidForm>();
        if (solidForm == null)
        {
            solidForm = FindObjectOfType<LiquidSolidForm>();
        }
    }

    void FixedUpdate()
    {
        if (solidForm != null && solidForm.GetIsDead()) return;

        HandleKeyboardControl();
    }

    void Update()
    {
        if (solidForm != null && solidForm.GetIsDead()) return;

        HandleJump();
    }

    void HandleKeyboardControl()
    {
        Vector2 direction = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction += Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction += Vector2.right;

        bool wantsDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Gas state moves much slower
        float currentMoveForce = moveForce;
        float currentWobble = wobbleStrength;
        if (solidForm != null && solidForm.GetIsGas())
        {
            currentMoveForce *= solidForm.GetGasMoveSpeedMultiplier();
            currentWobble *= 0.5f;
        }

        if (direction.x != 0)
        {
            wobbleTimer += Time.fixedDeltaTime * 12f;
            float wobble = Mathf.Sin(wobbleTimer) * currentWobble;

            Vector2 moveDir = new Vector2(direction.x, 0).normalized;
            Vector2 wobbleDir = new Vector2(0, wobble);

            simulation.ApplyDirectionalForce(moveDir, currentMoveForce);
            simulation.ApplyDirectionalForce(wobbleDir.normalized, Mathf.Abs(wobble));
        }
        else
        {
            wobbleTimer = 0f;
        }

        if (wantsDown)
        {
            simulation.ApplyDirectionalForce(Vector2.down, currentMoveForce * 0.5f);
        }

        simulation.ApplyCohesion(cohesionForce);
    }

    void HandleJump()
    {
        jumpTimer -= Time.deltaTime;

        // Only solid state can jump
        if (solidForm == null || !solidForm.GetIsSolid())
            return;

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && jumpTimer <= 0f)
        {
            LiquidParticle[] particles = GetComponentsInChildren<LiquidParticle>();
            bool anyGrounded = false;

            foreach (var particle in particles)
            {
                if (particle.IsGrounded())
                {
                    anyGrounded = true;
                    break;
                }
            }

            if (anyGrounded)
            {
                simulation.ApplyDirectionalForce(Vector2.up, jumpForce);
                jumpTimer = jumpCooldown;
            }
        }
    }
}