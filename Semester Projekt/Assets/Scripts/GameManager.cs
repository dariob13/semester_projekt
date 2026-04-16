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
        if (FindObjectOfType<AlertUI>() == null)
        {
            GameObject alertObj = new GameObject("AlertUI");
            alertObj.AddComponent<AlertUI>();
            Debug.Log("GameManager: Auto-created AlertUI.");
        }

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
        // AI handles kill timing via its own alert timer.
        // Keep this callback for global hooks/logging only.
        Debug.Log("Player detected by AI.");
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