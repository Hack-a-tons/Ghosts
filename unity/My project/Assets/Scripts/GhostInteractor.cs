using System;
using UnityEngine;
using UnityEngine.Events;

public class GhostInteractor : MonoBehaviour
{
    public static GhostInteractor Instance { get; private set; }
    
    [SerializeField] private float interactionRange = 3f;
    
    public UnityEvent<GhostVisual> OnGhostEnterRange;
    public UnityEvent<GhostVisual> OnGhostExitRange;
    public UnityEvent<GhostVisual, bool> OnRiddleAnswered; // ghost, correct
    
    private GhostVisual currentGhost;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Update()
    {
        CheckNearbyGhosts();
    }
    
    void CheckNearbyGhosts()
    {
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        GhostVisual nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (var ghost in ghosts)
        {
            float dist = ghost.DistanceToPlayer();
            if (dist < interactionRange && dist < nearestDist)
            {
                nearest = ghost;
                nearestDist = dist;
            }
        }
        
        if (nearest != currentGhost)
        {
            if (currentGhost != null)
                OnGhostExitRange?.Invoke(currentGhost);
            
            currentGhost = nearest;
            
            if (currentGhost != null)
                OnGhostEnterRange?.Invoke(currentGhost);
        }
    }
    
    public GhostVisual GetCurrentGhost() => currentGhost;
    
    public bool TryAnswerRiddle(string answer)
    {
        if (currentGhost == null) return false;
        
        var interaction = currentGhost.Data.interaction;
        if (interaction == null || interaction.type != "riddle_unlock") return false;
        
        bool correct = string.Equals(
            answer.Trim(), 
            interaction.correct_answer, 
            StringComparison.OrdinalIgnoreCase
        );
        
        OnRiddleAnswered?.Invoke(currentGhost, correct);
        return correct;
    }
    
    public string GetCurrentRiddle()
    {
        if (currentGhost?.Data.interaction?.riddle == null) return null;
        return currentGhost.Data.interaction.riddle;
    }
    
    public GhostReward GetCurrentReward()
    {
        return currentGhost?.Data.interaction?.reward;
    }
}
