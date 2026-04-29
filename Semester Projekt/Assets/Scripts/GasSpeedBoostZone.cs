using UnityEngine;

public class GasSpeedBoostZone : MonoBehaviour
{
    [Header("Boost Settings")]
    public float gasSpeedMultiplier = 2f;

    private LiquidSolidForm player;
    private bool playerInside = false;
    private bool boostApplied = false;
    private float originalGasSpeedMultiplier = 1f;

    void Start()
    {
        player = FindObjectOfType<LiquidSolidForm>();
        if (player == null)
        {
            Debug.LogError("GasSpeedBoostZone: LiquidSolidForm not found!");
            return;
        }

        originalGasSpeedMultiplier = player.gasMoveSpeedMultiplier;
    }

    void Update()
    {
        if (player == null) return;

        bool shouldBoost = playerInside && player.GetIsGas();

        if (shouldBoost && !boostApplied)
        {
            originalGasSpeedMultiplier = player.gasMoveSpeedMultiplier;
            player.gasMoveSpeedMultiplier = originalGasSpeedMultiplier * gasSpeedMultiplier;
            boostApplied = true;
        }
        else if (!shouldBoost && boostApplied)
        {
            player.gasMoveSpeedMultiplier = originalGasSpeedMultiplier;
            boostApplied = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (player == null) return;
        if (other.GetComponentInParent<LiquidSolidForm>() == player)
        {
            playerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (player == null) return;
        if (other.GetComponentInParent<LiquidSolidForm>() == player)
        {
            playerInside = false;
        }
    }
}
