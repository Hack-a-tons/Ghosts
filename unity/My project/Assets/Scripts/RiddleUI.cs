using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RiddleUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject riddlePanel;
    [SerializeField] private GameObject rewardPanel;
    
    [Header("Riddle UI")]
    [SerializeField] private TextMeshProUGUI ghostNameText;
    [SerializeField] private TextMeshProUGUI riddleText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    [Header("Reward UI")]
    [SerializeField] private TextMeshProUGUI rewardTypeText;
    [SerializeField] private TextMeshProUGUI rewardValueText;
    [SerializeField] private Button closeRewardButton;
    
    [Header("Proximity")]
    [SerializeField] private GameObject approachPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    
    private GhostVisual currentGhost;
    
    void Start()
    {
        HideAll();
        
        submitButton?.onClick.AddListener(OnSubmitAnswer);
        closeRewardButton?.onClick.AddListener(HideReward);
        
        var interactor = GhostInteractor.Instance;
        if (interactor != null)
        {
            interactor.OnGhostEnterRange.AddListener(OnGhostEnter);
            interactor.OnGhostExitRange.AddListener(OnGhostExit);
            interactor.OnRiddleAnswered.AddListener(OnRiddleResult);
        }
    }
    
    void HideAll()
    {
        riddlePanel?.SetActive(false);
        rewardPanel?.SetActive(false);
        approachPrompt?.SetActive(false);
    }
    
    void OnGhostEnter(GhostVisual ghost)
    {
        currentGhost = ghost;
        
        if (ghost.Data.interaction != null && ghost.Data.interaction.type == "riddle_unlock")
        {
            ShowRiddle(ghost);
        }
        else
        {
            ShowPrompt($"You found {ghost.Data.name}!");
        }
    }
    
    void OnGhostExit(GhostVisual ghost)
    {
        if (currentGhost == ghost)
        {
            currentGhost = null;
            HideAll();
        }
    }
    
    void ShowRiddle(GhostVisual ghost)
    {
        approachPrompt?.SetActive(false);
        riddlePanel?.SetActive(true);
        
        if (ghostNameText != null)
            ghostNameText.text = ghost.Data.name;
        
        if (riddleText != null)
            riddleText.text = ghost.Data.interaction.riddle;
        
        if (answerInput != null)
        {
            answerInput.text = "";
            answerInput.Select();
        }
        
        if (feedbackText != null)
            feedbackText.text = "";
    }
    
    void ShowPrompt(string message)
    {
        approachPrompt?.SetActive(true);
        if (promptText != null)
            promptText.text = message;
    }
    
    void OnSubmitAnswer()
    {
        if (answerInput == null || string.IsNullOrEmpty(answerInput.text)) return;
        
        GhostInteractor.Instance?.TryAnswerRiddle(answerInput.text);
    }
    
    void OnRiddleResult(GhostVisual ghost, bool correct)
    {
        if (correct)
        {
            ShowReward(ghost);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Wrong answer, try again!";
            
            if (answerInput != null)
                answerInput.text = "";
        }
    }
    
    void ShowReward(GhostVisual ghost)
    {
        riddlePanel?.SetActive(false);
        rewardPanel?.SetActive(true);
        
        var reward = ghost.Data.interaction?.reward;
        if (reward != null)
        {
            if (rewardTypeText != null)
                rewardTypeText.text = reward.type;
            if (rewardValueText != null)
                rewardValueText.text = reward.value;
        }
    }
    
    void HideReward()
    {
        rewardPanel?.SetActive(false);
    }
    
    // Call from UI button to manually open riddle
    public void OpenRiddle()
    {
        if (currentGhost != null)
            ShowRiddle(currentGhost);
    }
}
