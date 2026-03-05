using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    private PatrolAI[] allAIs;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        allAIs = FindObjectsOfType<PatrolAI>();

        // Subscribe all AIs to detection event
        foreach (var ai in allAIs)
        {
            ai.OnPlayerDetected += OnAISpottedPlayer;
        }

        Debug.Log($"GameManager initialized with {allAIs.Length} AI units");
    }

    void OnAISpottedPlayer()
    {
        // Get the player and kill them
        LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
        if (player != null && !player.GetIsDead())
        {
            player.ForceKill();
            Debug.Log("GAME OVER - Spotted by AI!");
        }
    }

    void OnDestroy()
    {
        if (allAIs != null)
        {
            foreach (var ai in allAIs)
            {
                ai.OnPlayerDetected -= OnAISpottedPlayer;
            }
        }
    }
}