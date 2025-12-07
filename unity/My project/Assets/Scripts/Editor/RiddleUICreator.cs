#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class RiddleUICreator : EditorWindow
{
    [MenuItem("GhostLayer/Create Riddle UI")]
    static void CreateRiddleUI()
    {
        // Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create RiddleUI root
        GameObject uiRoot = new GameObject("RiddleUI");
        uiRoot.transform.SetParent(canvas.transform, false);
        RiddleUI riddleUI = uiRoot.AddComponent<RiddleUI>();
        
        // Create Riddle Panel
        GameObject riddlePanel = CreatePanel("RiddlePanel", uiRoot.transform);
        
        // Ghost name
        GameObject nameObj = CreateText("GhostName", riddlePanel.transform, "Ghost Name", 32);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(0, 120);
        
        // Riddle text
        GameObject riddleObj = CreateText("RiddleText", riddlePanel.transform, "Solve this riddle...", 24);
        RectTransform riddleRect = riddleObj.GetComponent<RectTransform>();
        riddleRect.anchoredPosition = new Vector2(0, 40);
        riddleRect.sizeDelta = new Vector2(400, 100);
        
        // Answer input
        GameObject inputObj = CreateInputField("AnswerInput", riddlePanel.transform);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(0, -40);
        
        // Submit button
        GameObject submitObj = CreateButton("SubmitButton", riddlePanel.transform, "Submit");
        RectTransform submitRect = submitObj.GetComponent<RectTransform>();
        submitRect.anchoredPosition = new Vector2(0, -100);
        
        // Feedback text
        GameObject feedbackObj = CreateText("FeedbackText", riddlePanel.transform, "", 20);
        RectTransform feedbackRect = feedbackObj.GetComponent<RectTransform>();
        feedbackRect.anchoredPosition = new Vector2(0, -150);
        feedbackObj.GetComponent<TextMeshProUGUI>().color = Color.red;
        
        // Create Reward Panel
        GameObject rewardPanel = CreatePanel("RewardPanel", uiRoot.transform);
        rewardPanel.SetActive(false);
        
        GameObject rewardTitle = CreateText("RewardTitle", rewardPanel.transform, "🎉 Reward!", 36);
        rewardTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
        
        GameObject rewardType = CreateText("RewardType", rewardPanel.transform, "Coupon", 24);
        rewardType.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        
        GameObject rewardValue = CreateText("RewardValue", rewardPanel.transform, "10% OFF", 28);
        rewardValue.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        
        GameObject closeBtn = CreateButton("CloseButton", rewardPanel.transform, "Claim");
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        
        // Create Approach Prompt
        GameObject promptPanel = CreatePanel("ApproachPrompt", uiRoot.transform);
        promptPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
        promptPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);
        promptPanel.SetActive(false);
        
        GameObject promptText = CreateText("PromptText", promptPanel.transform, "A ghost is nearby!", 24);
        
        // Wire up references via SerializedObject
        SerializedObject so = new SerializedObject(riddleUI);
        so.FindProperty("riddlePanel").objectReferenceValue = riddlePanel;
        so.FindProperty("rewardPanel").objectReferenceValue = rewardPanel;
        so.FindProperty("ghostNameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("riddleText").objectReferenceValue = riddleObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("answerInput").objectReferenceValue = inputObj.GetComponent<TMP_InputField>();
        so.FindProperty("submitButton").objectReferenceValue = submitObj.GetComponent<Button>();
        so.FindProperty("feedbackText").objectReferenceValue = feedbackObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("rewardTypeText").objectReferenceValue = rewardType.GetComponent<TextMeshProUGUI>();
        so.FindProperty("rewardValueText").objectReferenceValue = rewardValue.GetComponent<TextMeshProUGUI>();
        so.FindProperty("closeRewardButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.FindProperty("approachPrompt").objectReferenceValue = promptPanel;
        so.FindProperty("promptText").objectReferenceValue = promptText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();
        
        Selection.activeObject = uiRoot;
        Debug.Log("Riddle UI created");
    }
    
    static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 350);
        
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        
        return panel;
    }
    
    static GameObject CreateText(string name, Transform parent, string text, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 50);
        
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return obj;
    }
    
    static GameObject CreateButton(string name, Transform parent, string label)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f, 1f);
        
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        GameObject textObj = CreateText("Text", obj.transform, label, 20);
        textObj.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
        
        return obj;
    }
    
    static GameObject CreateInputField(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50);
        
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.9f);
        
        TMP_InputField input = obj.AddComponent<TMP_InputField>();
        
        // Text area
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(obj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 5);
        taRect.offsetMax = new Vector2(-10, -5);
        textArea.AddComponent<RectMask2D>();
        
        // Input text
        GameObject inputText = CreateText("Text", textArea.transform, "", 20);
        inputText.GetComponent<TextMeshProUGUI>().color = Color.black;
        inputText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        RectTransform itRect = inputText.GetComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.offsetMin = Vector2.zero;
        itRect.offsetMax = Vector2.zero;
        
        // Placeholder
        GameObject placeholder = CreateText("Placeholder", textArea.transform, "Type answer...", 20);
        placeholder.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        RectTransform phRect = placeholder.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        
        input.textViewport = taRect;
        input.textComponent = inputText.GetComponent<TextMeshProUGUI>();
        input.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
        
        return obj;
    }
}
#endif
