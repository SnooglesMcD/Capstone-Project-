using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 10)]
    public string text;
    public string speakerName;
    public Sprite speakerPortrait;
    public AudioClip voiceClip;
    public float displayTime = 3f;
    public bool requirePlayerInput = true;
    
    public UnityEvent onLineStart;
    public UnityEvent onLineEnd;
}

[System.Serializable]
public class Dialogue
{
    public string dialogueID;
    public List<DialogueLine> lines;
    public bool canBeInterrupted = false;
    public bool lockPlayerMovement = true;
    public bool showSpeakerName = true;
    public bool showPortrait = true;
    
    public string nextDialogueID;
    public bool requiresItem;
    public string requiredItemID;
    
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Positioning")]
    public DialogueBoxPosition boxPosition = DialogueBoxPosition.BottomCenter;
    public enum DialogueBoxPosition
    {
        BottomCenter,
        BottomLeft,
        BottomRight,
        TopCenter,
        TopLeft,
        TopRight,
        MiddleCenter
    }
    
    [Header("UI References")]
    private GameObject dialogueContainer;
    private GameObject dialogueBox;
    private Image dialogueBackground;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI speakerNameText;
    private Image portraitImage;
    private GameObject continueIndicator;
    private GameObject closeHint;
    private GameObject victorNameTag;
    private CanvasScaler canvasScaler;
    
    [Header("Screen Adaptation")]
    [Range(0.7f, 0.95f)]
    public float screenWidthPercentage = 0.85f;
    [Range(0.15f, 0.4f)]
    public float screenHeightPercentage = 0.25f;
    public float minBoxWidth = 600f;
    public float minBoxHeight = 150f;
    public float maxBoxWidth = 1200f;
    public float maxBoxHeight = 300f;
    public float marginFromScreenEdge = 30f;
    
    [Header("Dialogue Box Settings")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public float borderThickness = 2f;
    public float cornerRadius = 10f;
    public Vector2 boxPadding = new Vector2(20, 15);
    
    [Header("Text Settings")]
    public Color dialogueTextColor = Color.white;
    public Color speakerTextColor = new Color(1f, 0.8f, 0.3f, 1f);
    public Color continueIndicatorColor = Color.yellow;
    public int baseDialogueFontSize = 22;
    public int baseSpeakerFontSize = 20;
    public int minFontSize = 16;
    public int maxFontSize = 28;
    public float textSpeed = 0.05f;
    public bool typewriterEffect = true;
    
    [Header("Portrait Settings")]
    [Range(0.05f, 0.2f)]
    public float portraitWidthPercentage = 0.12f;
    public float minPortraitSize = 60f;
    public float maxPortraitSize = 120f;
    public Vector2 portraitOffset = new Vector2(-10, 0);
    public Color portraitBorderColor = Color.white;
    
    [Header("Speaker Name Settings")]
    public Vector2 speakerNameOffset = new Vector2(0, 25);
    public bool showSpeakerBackground = true;
    public Color speakerBackgroundColor = new Color(0f, 0f, 0f, 0.7f);
    
    [Header("Victor Name Tag")]
    public bool showVictorNameTag = true;
    public string victorName = "Victor";
    public Color victorNameTagColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
    public Color victorNameTextColor = Color.white;
    public Vector2 victorNameTagOffset = new Vector2(15, -15);
    
    [Header("Close Hint")]
    public bool showCloseHint = true;
    public string closeHintText = "[R] to close";
    public Color closeHintColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
    public Vector2 closeHintOffset = new Vector2(-15, 15);
    
    [Header("Continue Indicator")]
    public string continueText = "▶";
    public float continueBlinkSpeed = 1f;
    public bool showContinueIndicator = true;
    
    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode skipKey = KeyCode.Space;
    public KeyCode closeKey = KeyCode.Escape;
    
    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioClip defaultTextSound;
    public float pitchVariation = 0.1f;
    
    [Header("Dialogue Database")]
    public List<Dialogue> dialogueDatabase = new List<Dialogue>();
    
    // Runtime variables
    private Dialogue currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine blinkCoroutine;
    
    // Adaptive values
    private float currentBoxWidth;
    private float currentBoxHeight;
    private float currentDialogueFontSize;
    private float currentSpeakerFontSize;
    private float currentPortraitSize;
    
    private PickupController playerPickupController;
    
    // Callbacks
    public event Action<Dialogue> OnDialogueStart;
    public event Action<Dialogue> OnDialogueEnd;
    public event Action<DialogueLine> OnLineStart;
    public event Action<DialogueLine> OnLineEnd;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Find player components
        playerPickupController = FindObjectOfType<PickupController>();
        
        // Create UI
        CreateDialogueUI();
        
        // Hide UI initially
        if (dialogueContainer != null)
            dialogueContainer.SetActive(false);
    }
    
    void CalculateResponsiveDimensions()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Calculate box size based on screen percentage
        currentBoxWidth = screenWidth * screenWidthPercentage;
        currentBoxHeight = screenHeight * screenHeightPercentage;
        
        // Clamp to min/max values
        currentBoxWidth = Mathf.Clamp(currentBoxWidth, minBoxWidth, maxBoxWidth);
        currentBoxHeight = Mathf.Clamp(currentBoxHeight, minBoxHeight, maxBoxHeight);
        
        // Calculate font sizes based on screen height (using 1080p as baseline)
        float screenRatio = screenHeight / 1080f;
        currentDialogueFontSize = Mathf.RoundToInt(baseDialogueFontSize * screenRatio);
        currentSpeakerFontSize = Mathf.RoundToInt(baseSpeakerFontSize * screenRatio);
        
        currentDialogueFontSize = Mathf.Clamp(currentDialogueFontSize, minFontSize, maxFontSize);
        currentSpeakerFontSize = Mathf.Clamp(currentSpeakerFontSize, minFontSize, maxFontSize - 2);
        
        // Calculate portrait size based on box height
        currentPortraitSize = currentBoxHeight * portraitWidthPercentage;
        currentPortraitSize = Mathf.Clamp(currentPortraitSize, minPortraitSize, maxPortraitSize);
        
        Debug.Log($"Responsive dimensions: Box={currentBoxWidth}x{currentBoxHeight}, " +
                  $"DialogueFont={currentDialogueFontSize}, SpeakerFont={currentSpeakerFontSize}, " +
                  $"Portrait={currentPortraitSize}");
    }
    
    void CreateDialogueUI()
    {
        // Calculate responsive dimensions first
        CalculateResponsiveDimensions();
        
        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Add CanvasScaler for responsive UI
        if (canvas.GetComponent<CanvasScaler>() == null)
        {
            canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }
        else
        {
            canvasScaler = canvas.GetComponent<CanvasScaler>();
        }
        
        // Create dialogue container
        dialogueContainer = new GameObject("DialogueContainer");
        dialogueContainer.transform.SetParent(canvas.transform);
        RectTransform containerRT = dialogueContainer.AddComponent<RectTransform>();
        SetContainerPosition(containerRT);
        
        // Create dialogue box
        dialogueBox = new GameObject("DialogueBox");
        dialogueBox.transform.SetParent(dialogueContainer.transform);
        dialogueBackground = dialogueBox.AddComponent<Image>();
        dialogueBackground.color = backgroundColor;
        dialogueBackground.type = Image.Type.Sliced;
        
        // Set box size and position
        RectTransform boxRT = dialogueBox.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(currentBoxWidth, currentBoxHeight);
        boxRT.anchoredPosition = Vector2.zero;
        
        // Create portrait
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(dialogueBox.transform);
        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        
        RectTransform portraitRT = portraitImage.GetComponent<RectTransform>();
        portraitRT.anchorMin = new Vector2(0, 0.5f);
        portraitRT.anchorMax = new Vector2(0, 0.5f);
        portraitRT.pivot = new Vector2(0, 0.5f);
        portraitRT.sizeDelta = new Vector2(currentPortraitSize, currentPortraitSize);
        portraitRT.anchoredPosition = new Vector2(boxPadding.x + portraitOffset.x, portraitOffset.y);
        
        // Create speaker name background
        GameObject speakerBgObj = new GameObject("SpeakerBackground");
        speakerBgObj.transform.SetParent(dialogueBox.transform);
        Image speakerBg = speakerBgObj.AddComponent<Image>();
        speakerBg.color = speakerBackgroundColor;
        
        RectTransform speakerBgRT = speakerBg.GetComponent<RectTransform>();
        speakerBgRT.anchorMin = new Vector2(0, 1);
        speakerBgRT.anchorMax = new Vector2(0.4f, 1);
        speakerBgRT.pivot = new Vector2(0, 1);
        float speakerBgWidth = Mathf.Min(currentBoxWidth * 0.4f, 250f * (Screen.width / 1920f));
        speakerBgRT.sizeDelta = new Vector2(speakerBgWidth, 40f * (Screen.height / 1080f));
        speakerBgRT.anchoredPosition = new Vector2(boxPadding.x, -boxPadding.y);
        speakerBgObj.SetActive(showSpeakerBackground);
        
        // Create speaker name text
        GameObject speakerObj = new GameObject("SpeakerName");
        speakerObj.transform.SetParent(speakerBgObj.transform);
        speakerNameText = speakerObj.AddComponent<TextMeshProUGUI>();
        speakerNameText.color = speakerTextColor;
        speakerNameText.fontSize = currentSpeakerFontSize;
        speakerNameText.alignment = TextAlignmentOptions.Left;
        speakerNameText.fontStyle = FontStyles.Bold;
        
        RectTransform speakerRT = speakerNameText.GetComponent<RectTransform>();
        speakerRT.anchorMin = Vector2.zero;
        speakerRT.anchorMax = Vector2.one;
        speakerRT.offsetMin = new Vector2(10f * (Screen.width / 1920f), 0);
        speakerRT.offsetMax = new Vector2(-10f * (Screen.width / 1920f), 0);
        
        // Create dialogue text area
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialogueBox.transform);
        dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.color = dialogueTextColor;
        dialogueText.fontSize = currentDialogueFontSize;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
        
        RectTransform textRT = dialogueText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        float textLeftPadding = boxPadding.x + currentPortraitSize + 10f * (Screen.width / 1920f);
        textRT.offsetMin = new Vector2(textLeftPadding, boxPadding.y + 10f * (Screen.height / 1080f));
        textRT.offsetMax = new Vector2(-boxPadding.x, -boxPadding.y);
        
        // Create Victor name tag
        if (showVictorNameTag)
        {
            CreateVictorNameTag();
        }
        
        // Create close hint
        if (showCloseHint)
        {
            CreateCloseHint();
        }
        
        // Create continue indicator
        GameObject continueObj = new GameObject("ContinueIndicator");
        continueObj.transform.SetParent(dialogueBox.transform);
        continueIndicator = continueObj;
        
        TextMeshProUGUI continueTextComp = continueObj.AddComponent<TextMeshProUGUI>();
        continueTextComp.text = continueText;
        continueTextComp.color = continueIndicatorColor;
        continueTextComp.fontSize = Mathf.RoundToInt(currentDialogueFontSize * 1.1f);
        continueTextComp.alignment = TextAlignmentOptions.BottomRight;
        
        RectTransform continueRT = continueTextComp.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(1, 0);
        continueRT.anchorMax = new Vector2(1, 0);
        continueRT.pivot = new Vector2(1, 0);
        float indicatorSize = 30f * (Screen.height / 1080f);
        continueRT.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        continueRT.anchoredPosition = new Vector2(-10f * (Screen.width / 1920f), 10f * (Screen.height / 1080f));
        
        continueIndicator.SetActive(false);
        
        Debug.Log("Adaptive RPG-style dialogue UI created with Victor name tag");
    }
    
    void CreateVictorNameTag()
    {
        // Create Victor name tag background
        GameObject victorTagObj = new GameObject("VictorNameTag");
        victorTagObj.transform.SetParent(dialogueBox.transform);
        victorNameTag = victorTagObj;
        
        Image victorBg = victorTagObj.AddComponent<Image>();
        victorBg.color = victorNameTagColor;
        victorBg.type = Image.Type.Sliced;
        
        // Set size and position (top-left corner of dialogue box)
        RectTransform victorRT = victorTagObj.GetComponent<RectTransform>();
        victorRT.anchorMin = new Vector2(0, 1);
        victorRT.anchorMax = new Vector2(0, 1);
        victorRT.pivot = new Vector2(0, 1);
        
        float tagWidth = 100f * (Screen.width / 1920f);
        float tagHeight = 35f * (Screen.height / 1080f);
        victorRT.sizeDelta = new Vector2(tagWidth, tagHeight);
        victorRT.anchoredPosition = victorNameTagOffset;
        
        // Create Victor name text
        GameObject victorTextObj = new GameObject("VictorText");
        victorTextObj.transform.SetParent(victorTagObj.transform);
        TextMeshProUGUI victorText = victorTextObj.AddComponent<TextMeshProUGUI>();
        victorText.text = victorName;
        victorText.color = victorNameTextColor;
        victorText.fontSize = Mathf.RoundToInt(currentSpeakerFontSize * 0.9f);
        victorText.alignment = TextAlignmentOptions.Center;
        victorText.fontStyle = FontStyles.Bold;
        
        RectTransform victorTextRT = victorText.GetComponent<RectTransform>();
        victorTextRT.anchorMin = Vector2.zero;
        victorTextRT.anchorMax = Vector2.one;
        victorTextRT.offsetMin = new Vector2(5, 0);
        victorTextRT.offsetMax = new Vector2(-5, 0);
    }
    
    void CreateCloseHint()
    {
        // Create close hint text
        GameObject closeHintObj = new GameObject("CloseHint");
        closeHintObj.transform.SetParent(dialogueBox.transform);
        closeHint = closeHintObj;
        
        TextMeshProUGUI closeHintTextComp = closeHintObj.AddComponent<TextMeshProUGUI>();
        closeHintTextComp.text = closeHintText;
        closeHintTextComp.color = closeHintColor;
        closeHintTextComp.fontSize = Mathf.RoundToInt(currentDialogueFontSize * 0.8f);
        closeHintTextComp.alignment = TextAlignmentOptions.TopRight;
        closeHintTextComp.fontStyle = FontStyles.Italic;
        
        // Set position (top-right corner of dialogue box)
        RectTransform closeHintRT = closeHintTextComp.GetComponent<RectTransform>();
        closeHintRT.anchorMin = new Vector2(1, 1);
        closeHintRT.anchorMax = new Vector2(1, 1);
        closeHintRT.pivot = new Vector2(1, 1);
        
        float hintWidth = 120f * (Screen.width / 1920f);
        float hintHeight = 25f * (Screen.height / 1080f);
        closeHintRT.sizeDelta = new Vector2(hintWidth, hintHeight);
        closeHintRT.anchoredPosition = closeHintOffset;
    }
    
    void SetContainerPosition(RectTransform containerRT)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float marginX = marginFromScreenEdge * (screenWidth / 1920f);
        float marginY = marginFromScreenEdge * (screenHeight / 1080f);
        
        switch (boxPosition)
        {
            case DialogueBoxPosition.BottomCenter:
                containerRT.anchorMin = new Vector2(0.5f, 0);
                containerRT.anchorMax = new Vector2(0.5f, 0);
                containerRT.pivot = new Vector2(0.5f, 0);
                containerRT.anchoredPosition = new Vector2(0, marginY);
                break;
                
            case DialogueBoxPosition.BottomLeft:
                containerRT.anchorMin = new Vector2(0, 0);
                containerRT.anchorMax = new Vector2(0, 0);
                containerRT.pivot = new Vector2(0, 0);
                containerRT.anchoredPosition = new Vector2(marginX, marginY);
                break;
                
            case DialogueBoxPosition.BottomRight:
                containerRT.anchorMin = new Vector2(1, 0);
                containerRT.anchorMax = new Vector2(1, 0);
                containerRT.pivot = new Vector2(1, 0);
                containerRT.anchoredPosition = new Vector2(-marginX, marginY);
                break;
                
            case DialogueBoxPosition.TopCenter:
                containerRT.anchorMin = new Vector2(0.5f, 1);
                containerRT.anchorMax = new Vector2(0.5f, 1);
                containerRT.pivot = new Vector2(0.5f, 1);
                containerRT.anchoredPosition = new Vector2(0, -marginY);
                break;
                
            case DialogueBoxPosition.TopLeft:
                containerRT.anchorMin = new Vector2(0, 1);
                containerRT.anchorMax = new Vector2(0, 1);
                containerRT.pivot = new Vector2(0, 1);
                containerRT.anchoredPosition = new Vector2(marginX, -marginY);
                break;
                
            case DialogueBoxPosition.TopRight:
                containerRT.anchorMin = new Vector2(1, 1);
                containerRT.anchorMax = new Vector2(1, 1);
                containerRT.pivot = new Vector2(1, 1);
                containerRT.anchoredPosition = new Vector2(-marginX, -marginY);
                break;
                
            case DialogueBoxPosition.MiddleCenter:
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.pivot = new Vector2(0.5f, 0.5f);
                containerRT.anchoredPosition = Vector2.zero;
                break;
        }
    }
    
    void UpdateUILayout()
    {
        if (dialogueContainer == null || dialogueBox == null) return;
        
        // Recalculate responsive dimensions
        CalculateResponsiveDimensions();
        
        // Update box size
        RectTransform boxRT = dialogueBox.GetComponent<RectTransform>();
        if (boxRT != null)
        {
            boxRT.sizeDelta = new Vector2(currentBoxWidth, currentBoxHeight);
        }
        
        // Update fonts
        if (dialogueText != null) dialogueText.fontSize = currentDialogueFontSize;
        if (speakerNameText != null) speakerNameText.fontSize = currentSpeakerFontSize;
        
        // Update portrait size
        if (portraitImage != null)
        {
            RectTransform portraitRT = portraitImage.GetComponent<RectTransform>();
            if (portraitRT != null)
            {
                portraitRT.sizeDelta = new Vector2(currentPortraitSize, currentPortraitSize);
            }
        }
        
        // Update text area padding
        if (dialogueText != null)
        {
            RectTransform textRT = dialogueText.GetComponent<RectTransform>();
            if (textRT != null)
            {
                float textLeftPadding = boxPadding.x + currentPortraitSize + 10f * (Screen.width / 1920f);
                textRT.offsetMin = new Vector2(textLeftPadding, boxPadding.y + 10f * (Screen.height / 1080f));
                textRT.offsetMax = new Vector2(-boxPadding.x, -boxPadding.y);
            }
        }
        
        // Update continue indicator
        if (continueIndicator != null)
        {
            TextMeshProUGUI continueTextComp = continueIndicator.GetComponent<TextMeshProUGUI>();
            if (continueTextComp != null)
            {
                continueTextComp.fontSize = Mathf.RoundToInt(currentDialogueFontSize * 1.1f);
                
                RectTransform continueRT = continueTextComp.GetComponent<RectTransform>();
                if (continueRT != null)
                {
                    float indicatorSize = 30f * (Screen.height / 1080f);
                    continueRT.sizeDelta = new Vector2(indicatorSize, indicatorSize);
                    continueRT.anchoredPosition = new Vector2(-10f * (Screen.width / 1920f), 10f * (Screen.height / 1080f));
                }
            }
        }
        
        // Update speaker background
        Transform speakerBg = dialogueBox.transform.Find("SpeakerBackground");
        if (speakerBg != null)
        {
            RectTransform speakerBgRT = speakerBg.GetComponent<RectTransform>();
            if (speakerBgRT != null)
            {
                float speakerBgWidth = Mathf.Min(currentBoxWidth * 0.4f, 250f * (Screen.width / 1920f));
                speakerBgRT.sizeDelta = new Vector2(speakerBgWidth, 40f * (Screen.height / 1080f));
                speakerBgRT.anchoredPosition = new Vector2(boxPadding.x, -boxPadding.y);
            }
        }
        
        // Update Victor name tag
        if (victorNameTag != null)
        {
            RectTransform victorRT = victorNameTag.GetComponent<RectTransform>();
            if (victorRT != null)
            {
                float tagWidth = 100f * (Screen.width / 1920f);
                float tagHeight = 35f * (Screen.height / 1080f);
                victorRT.sizeDelta = new Vector2(tagWidth, tagHeight);
                victorRT.anchoredPosition = victorNameTagOffset;
            }
            
            // Update Victor text font size
            TextMeshProUGUI victorText = victorNameTag.GetComponentInChildren<TextMeshProUGUI>();
            if (victorText != null)
            {
                victorText.fontSize = Mathf.RoundToInt(currentSpeakerFontSize * 0.9f);
            }
        }
        
        // Update close hint
        if (closeHint != null)
        {
            TextMeshProUGUI closeHintTextComp = closeHint.GetComponent<TextMeshProUGUI>();
            if (closeHintTextComp != null)
            {
                closeHintTextComp.fontSize = Mathf.RoundToInt(currentDialogueFontSize * 0.8f);
                
                RectTransform closeHintRT = closeHintTextComp.GetComponent<RectTransform>();
                if (closeHintRT != null)
                {
                    float hintWidth = 120f * (Screen.width / 1920f);
                    float hintHeight = 25f * (Screen.height / 1080f);
                    closeHintRT.sizeDelta = new Vector2(hintWidth, hintHeight);
                    closeHintRT.anchoredPosition = closeHintOffset;
                }
            }
        }
        
        // Update container position
        RectTransform containerRT = dialogueContainer.GetComponent<RectTransform>();
        SetContainerPosition(containerRT);
    }
    
    void Update()
    {
        if (!isDialogueActive) return;
        
        HandleInput();
        
        
        if (dialogueContainer != null && dialogueContainer.activeInHierarchy)
        {
            UpdateUILayout();
        }
    }
    
    void HandleInput()
    {
        if (currentDialogue == null) return;
        
        // Check for close key
        if (Input.GetKeyDown(closeKey))
        {
            ForceEndDialogue();
            return;
        }
        
        // Skip typing with skip key
        if (Input.GetKeyDown(skipKey) && isTyping)
        {
            SkipTyping();
            return;
        }
        
        // Advance dialogue with advance key
        if (Input.GetKeyDown(advanceKey))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                AdvanceDialogue();
            }
        }
    }
    
    public void StartDialogue(string dialogueID)
    {
        Dialogue dialogue = GetDialogueByID(dialogueID);
        if (dialogue != null)
        {
            StartDialogue(dialogue);
        }
        else
        {
            Debug.LogWarning($"Dialogue with ID '{dialogueID}' not found!");
        }
    }
    
    public void StartDialogue(Dialogue dialogue)
    {
        if (isDialogueActive && !dialogue.canBeInterrupted)
        {
            Debug.Log("Dialogue already active and cannot be interrupted.");
            return;
        }
        
        if (isDialogueActive)
        {
            EndDialogue();
        }
        
        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        if (dialogue.lockPlayerMovement && playerPickupController != null)
        {
            LockPlayerControls(true);
        }
        
        if (dialogueContainer == null)
        {
            CreateDialogueUI();
        }
        
        if (dialogueContainer != null)
        {
            UpdateUILayout();
            dialogueContainer.SetActive(true);
            
            // Show/hide Victor name tag based on settings
            if (victorNameTag != null)
                victorNameTag.SetActive(showVictorNameTag);
            
            // Show/hide close hint based on settings
            if (closeHint != null)
                closeHint.SetActive(showCloseHint);
        }
        
        dialogue.onDialogueStart?.Invoke();
        OnDialogueStart?.Invoke(dialogue);
        
        DisplayCurrentLine();
    }
    
    void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count) 
        {
            EndDialogue();
            return;
        }
        
        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        // Update UI
        if (speakerNameText != null)
        {
            speakerNameText.text = currentDialogue.showSpeakerName ? line.speakerName : "";
            if (speakerNameText.transform.parent != null)
                speakerNameText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(speakerNameText.text));
        }
        
        if (portraitImage != null)
        {
            portraitImage.sprite = line.speakerPortrait;
            portraitImage.gameObject.SetActive(line.speakerPortrait != null && currentDialogue.showPortrait);
        }
        
        if (dialogueText != null) dialogueText.text = "";
        
        line.onLineStart?.Invoke();
        OnLineStart?.Invoke(line);
        
        if (voiceSource != null && line.voiceClip != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            voiceSource.Play();
        }
        
        if (typewriterEffect && dialogueText != null)
        {
            typingCoroutine = StartCoroutine(TypeText(line.text, line));
        }
        else if (dialogueText != null)
        {
            dialogueText.text = line.text;
            isTyping = false;
            ShowContinueIndicator(line.requirePlayerInput);
            
            if (!line.requirePlayerInput && line.displayTime > 0)
            {
                StartCoroutine(AutoAdvance(line.displayTime));
            }
        }
    }
    
    IEnumerator TypeText(string text, DialogueLine line)
    {
        isTyping = true;
        ShowContinueIndicator(false);
        
        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            
            if (defaultTextSound != null)
            {
                PlayTypingSound();
            }
            
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
        
        line.onLineEnd?.Invoke();
        OnLineEnd?.Invoke(line);
        
        ShowContinueIndicator(line.requirePlayerInput);
        
        if (!line.requirePlayerInput && line.displayTime > 0)
        {
            StartCoroutine(AutoAdvance(line.displayTime));
        }
    }
    
    void PlayTypingSound()
    {
        if (voiceSource != null && defaultTextSound != null)
        {
            voiceSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            voiceSource.PlayOneShot(defaultTextSound);
        }
    }
    
    IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdvanceDialogue();
    }
    
    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        if (currentDialogue != null && currentLineIndex < currentDialogue.lines.Count)
        {
            DialogueLine line = currentDialogue.lines[currentLineIndex];
            if (dialogueText != null)
            {
                dialogueText.text = line.text;
            }
            
            isTyping = false;
            
            line.onLineEnd?.Invoke();
            OnLineEnd?.Invoke(line);
            
            ShowContinueIndicator(line.requirePlayerInput);
            
            if (!line.requirePlayerInput && line.displayTime > 0)
            {
                StartCoroutine(AutoAdvance(line.displayTime));
            }
        }
    }
    
    void AdvanceDialogue()
    {
        currentLineIndex++;
        
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentLine();
        }
    }
    
    void ShowContinueIndicator(bool show)
    {
        if (continueIndicator != null && showContinueIndicator)
        {
            continueIndicator.SetActive(show && !isTyping);
            
            if (show && !isTyping)
            {
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkContinueIndicator());
            }
            else if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
        }
    }
    
    IEnumerator BlinkContinueIndicator()
    {
        TextMeshProUGUI indicatorText = continueIndicator.GetComponent<TextMeshProUGUI>();
        Color originalColor = indicatorText.color;
        
        while (true)
        {
            float alpha = Mathf.PingPong(Time.time * continueBlinkSpeed, 1f);
            indicatorText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }
    
    void EndDialogue()
    {
        if (currentDialogue != null)
        {
            currentDialogue.onDialogueEnd?.Invoke();
            OnDialogueEnd?.Invoke(currentDialogue);
            
            if (!string.IsNullOrEmpty(currentDialogue.nextDialogueID))
            {
                StartDialogue(currentDialogue.nextDialogueID);
                return;
            }
        }
        
        if (dialogueContainer != null)
        {
            dialogueContainer.SetActive(false);
        }
        
        LockPlayerControls(false);
        
        currentDialogue = null;
        currentLineIndex = 0;
        isDialogueActive = false;
        isTyping = false;
        
        if (voiceSource != null) voiceSource.Stop();
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
    }
    
    void LockPlayerControls(bool lockControls)
    {
        if (playerPickupController != null)
        {
            if (lockControls)
            {
                if (playerPickupController.IsInspecting)
                {
                    playerPickupController.ExitInspectMode();
                }
                
                if (playerPickupController.player_movement_script != null)
                    playerPickupController.player_movement_script.enabled = false;
                if (playerPickupController.player_camera_script != null)
                    playerPickupController.player_camera_script.enabled = false;
            }
            else
            {
                if (playerPickupController.player_movement_script != null)
                    playerPickupController.player_movement_script.enabled = true;
                if (playerPickupController.player_camera_script != null)
                    playerPickupController.player_camera_script.enabled = true;
            }
        }
    }
    
    Dialogue GetDialogueByID(string dialogueID)
    {
        return dialogueDatabase.Find(d => d.dialogueID == dialogueID);
    }
    
    public void AddDialogueToDatabase(Dialogue dialogue)
    {
        if (!dialogueDatabase.Contains(dialogue))
        {
            dialogueDatabase.Add(dialogue);
        }
    }
    
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    public void ForceEndDialogue()
    {
        Debug.Log("Dialogue force closed by player");
        EndDialogue();
    }
    
    // Quick dialogue methods
    public void ShowSimpleMessage(string message, float displayTime = 3f)
    {
        Dialogue quickDialogue = new Dialogue
        {
            dialogueID = "quick_message_" + Time.time,
            lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    text = message,
                    speakerName = "",
                    displayTime = displayTime,
                    requirePlayerInput = false
                }
            },
            lockPlayerMovement = false,
            canBeInterrupted = true
        };
        
        StartDialogue(quickDialogue);
    }
    
    public void ShowItemDescription(string itemName, string description)
    {
        Dialogue itemDialogue = new Dialogue
        {
            dialogueID = "item_description_" + Time.time,
            lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    text = $"<b>{itemName}</b>\n\n{description}",
                    speakerName = "",
                    displayTime = 5f,
                    requirePlayerInput = false
                }
            },
            lockPlayerMovement = false,
            canBeInterrupted = true
        };
        
        StartDialogue(itemDialogue);
    }
    
    // UI customization methods
    public void UpdateUIStyling()
    {
        if (dialogueBackground != null)
            dialogueBackground.color = backgroundColor;
        
        if (dialogueText != null)
            dialogueText.color = dialogueTextColor;
        
        if (speakerNameText != null)
            speakerNameText.color = speakerTextColor;
        
        if (continueIndicator != null)
        {
            TextMeshProUGUI indicatorText = continueIndicator.GetComponent<TextMeshProUGUI>();
            if (indicatorText != null)
                indicatorText.color = continueIndicatorColor;
        }
        
        if (victorNameTag != null)
        {
            Image victorBg = victorNameTag.GetComponent<Image>();
            if (victorBg != null)
                victorBg.color = victorNameTagColor;
            
            TextMeshProUGUI victorText = victorNameTag.GetComponentInChildren<TextMeshProUGUI>();
            if (victorText != null)
                victorText.color = victorNameTextColor;
        }
        
        if (closeHint != null)
        {
            TextMeshProUGUI closeHintTextComp = closeHint.GetComponent<TextMeshProUGUI>();
            if (closeHintTextComp != null)
                closeHintTextComp.color = closeHintColor;
        }
    }
    
    [ContextMenu("Test Simple Dialogue")]
    void TestSimpleDialogue()
    {
        ShowSimpleMessage("This is a test message with Victor's name tag and close hint!", 3f);
    }
    
    [ContextMenu("Toggle Victor Name Tag")]
    void ToggleVictorNameTag()
    {
        showVictorNameTag = !showVictorNameTag;
        if (victorNameTag != null)
            victorNameTag.SetActive(showVictorNameTag);
        Debug.Log($"Victor name tag: {showVictorNameTag}");
    }
    
    [ContextMenu("Toggle Close Hint")]
    void ToggleCloseHint()
    {
        showCloseHint = !showCloseHint;
        if (closeHint != null)
            closeHint.SetActive(showCloseHint);
        Debug.Log($"Close hint: {showCloseHint}");
    }
    
    [ContextMenu("Apply Dark Theme")]
    void ApplyDarkTheme()
    {
        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        dialogueTextColor = Color.white;
        speakerTextColor = new Color(1f, 0.8f, 0.3f, 1f);
        continueIndicatorColor = Color.yellow;
        victorNameTagColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
        closeHintColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        UpdateUIStyling();
    }
    
    [ContextMenu("Apply Parchment Theme")]
    void ApplyParchmentTheme()
    {
        backgroundColor = new Color(0.98f, 0.96f, 0.9f, 0.98f);
        dialogueTextColor = new Color(0.2f, 0.15f, 0.1f, 1f);
        speakerTextColor = new Color(0.5f, 0.2f, 0.1f, 1f);
        continueIndicatorColor = new Color(0.3f, 0.2f, 0.1f, 1f);
        victorNameTagColor = new Color(0.6f, 0.4f, 0.2f, 0.9f);
        closeHintColor = new Color(0.4f, 0.3f, 0.2f, 0.8f);
        UpdateUIStyling();
    }
    
    // Screen size adaptation
    void OnRectTransformDimensionsChange()
    {
        // This gets called when the screen size changes
        if (isDialogueActive && dialogueContainer != null && dialogueContainer.activeInHierarchy)
        {
            UpdateUILayout();
        }
    }
    
    void OnDestroy()
    {
        if (dialogueContainer != null)
            Destroy(dialogueContainer);
    }
}