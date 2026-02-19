using UnityEngine;

public class LiquidController : MonoBehaviour
{
    private LiquidSimulation simulation;
    private Camera mainCamera;

    [Header("Control Settings")]
    public bool useMouseControl = true;
    public KeyCode controlKey = KeyCode.Mouse0;

    [Header("Keyboard Control")]
    public float keyboardForceMultiplier = 2f;

    void Start()
    {
        simulation = GetComponent<LiquidSimulation>();
        if (simulation == null)
        {
            simulation = gameObject.AddComponent<LiquidSimulation>();
        }

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (useMouseControl)
        {
            HandleMouseControl();
        }

        HandleKeyboardControl();
    }

    void HandleMouseControl()
    {
        if (Input.GetKey(controlKey))
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            simulation.ApplyPlayerControl(mouseWorldPos);
        }
    }

    void HandleKeyboardControl()
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

        if (direction.magnitude > 0.1f)
        {
            Vector2 targetPos = (Vector2)transform.position + direction.normalized * keyboardForceMultiplier;
            simulation.ApplyPlayerControl(targetPos);
        }
    }
}