using UnityEngine;

public class LiquidController : MonoBehaviour
{
    private LiquidSimulation simulation;
    private LiquidSolidForm solidForm;
    private Pipe nearbyPipe;
    private PipeUI pipeUI;
    private PatrolAI controlledAI;

    [Header("Keyboard Control")]
    public float moveForce = 120f;
    public float cohesionForce = 40f;
    public float wobbleStrength = 15f;

    [Header("Jump Settings")]
    public float jumpForce = 200f;
    public float jumpCooldown = 0.5f;

    [Header("AI Control")]
    public KeyCode takeControlKey = KeyCode.E;

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

        pipeUI = FindObjectOfType<PipeUI>();
    }

    void FixedUpdate()
    {
        if (solidForm != null && solidForm.GetIsDead()) return;

        if (controlledAI != null)
        {
            HandleAIControl();
        }
        else
        {
            HandleKeyboardControl();
        }
    }

    void Update()
    {
        if (solidForm != null && solidForm.GetIsDead()) return;

        HandleJump();
        HandlePipeDetection();
        HandlePipeInput();
        HandleAIControlInput();
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

    void HandleAIControl()
    {
        Vector2 direction = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction += Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction += Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction += Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction += Vector2.right;

        controlledAI.TakeControl(direction.normalized);

        // Exit control if AI is knocked out
        if (controlledAI.IsKnockedOut())
        {
            controlledAI = null;
            Debug.Log("*** AI CONTROL RELEASED - GUARD KNOCKED OUT ***");
        }
    }

    void HandleAIControlInput()
    {
        if (Input.GetKeyDown(takeControlKey))
        {
            if (controlledAI == null)
            {
                // Try to control nearby AI
                PatrolAI[] allGuards = FindObjectsOfType<PatrolAI>();
                PatrolAI closestGuard = null;
                float closestDist = 5f;

                foreach (var guard in allGuards)
                {
                    if (guard.IsKnockedOut()) continue;

                    float dist = Vector2.Distance(transform.position, guard.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestGuard = guard;
                    }
                }

                if (closestGuard != null)
                {
                    controlledAI = closestGuard;
                    Debug.Log("*** TAKING CONTROL OF AI GUARD ***");
                }
            }
            else
            {
                // Release control
                controlledAI = null;
                Debug.Log("*** AI CONTROL RELEASED ***");
            }
        }
    }

    void HandleJump()
    {
        jumpTimer -= Time.deltaTime;

        if (controlledAI != null) return; // No jumping while controlling AI

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

    void HandlePipeDetection()
    {
        if (controlledAI != null) return; // No pipe interaction while controlling AI

        // Find nearby pipes
        Pipe[] allPipes = FindObjectsOfType<Pipe>();
        nearbyPipe = null;
        float closestDistance = float.MaxValue;

        foreach (var pipe in allPipes)
        {
            if (pipe.IsPlayerNearby())
            {
                float distance = Vector2.Distance(transform.position, pipe.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearbyPipe = pipe;
                }
            }
        }

        // Update UI
        if (pipeUI != null)
        {
            if (nearbyPipe != null && solidForm != null && solidForm.GetCurrentState() == MatterState.Liquid)
            {
                pipeUI.ShowPrompt();
            }
            else
            {
                pipeUI.HidePrompt();
            }
        }
    }

    void HandlePipeInput()
    {
        if (Input.GetKeyDown(KeyCode.F) && nearbyPipe != null && solidForm != null)
        {
            if (solidForm.GetCurrentState() == MatterState.Liquid)
            {
                if (pipeUI != null)
                {
                    pipeUI.ShowTraversalMessage();
                }

                if (nearbyPipe.TryEnterPipe(solidForm))
                {
                    Debug.Log("Entered pipe!");
                }
            }
        }
    }
}