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

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public Image portraitImage;
    public GameObject continuePrompt;
    public Image backgroundPanel;
    
    [Header("UI Styling - Background")]
    public Sprite backgroundSprite;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Vector2 backgroundPadding = new Vector2(50, 30);
    
    [Header("UI Styling - Text")]
    public Color dialogueTextColor = Color.white;
    public Color speakerTextColor = new Color(1f, 0.8f, 0.3f, 1f);
    public Color continueTextColor = new Color(1f, 1f, 1f, 0.7f);
    public int dialogueFontSize = 24;
    public int speakerFontSize = 28;
    
    [Header("UI Styling - Portrait")]
    public Sprite portraitFrameSprite;
    public Color portraitFrameColor = Color.white;
    public float portraitSize = 150f;
    
    [Header("Animation Settings")]
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.2f;
    public bool useFadeAnimations = true;
    
    [Header("Typewriter Settings")]
    public float textSpeed = 0.05f;
    public bool skipOnClick = true;
    public bool typewriterEffect = true;
    
    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioClip defaultTextSound;
    public float pitchVariation = 0.1f;
    
    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode skipKey = KeyCode.Space;
    
    [Header("Dialogue Database")]
    public List<Dialogue> dialogueDatabase = new List<Dialogue>();
    
    // Runtime variables
    private Dialogue currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private CanvasGroup canvasGroup;
    
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
        
        // Initialize UI
        InitializeUI();
        
        if (dialoguePanel != null) 
        {
            canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
        }
        
        
        // Apply UI styling

    }
    
    void InitializeUI()
    {
        // If UI elements aren't assigned, try to find them
        if (dialoguePanel == null)
        {
            dialoguePanel = GameObject.Find("DialoguePanel");
        }
        
        if (dialogueText == null && dialoguePanel != null)
        {
            dialogueText = dialoguePanel.GetComponentInChildren<TextMeshProUGUI>();
        }
        
    }
    
    
    
    void Update()
    {
        if (!isDialogueActive) return;
        
        HandleInput();
    }
    
    void HandleInput()
    {
        if (currentDialogue == null) return;
        
        // Skip typing with skip key
        if (Input.GetKeyDown(skipKey) && isTyping)
        {
            SkipTyping();
            return;
        }
        
        // Advance dialogue
        if (Input.GetKeyDown(advanceKey))
        {
            if (isTyping && skipOnClick)
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
        
        // Stop any active dialogue
        if (isDialogueActive)
        {
            EndDialogue();
        }
        
        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        // Lock player controls if specified
        if (dialogue.lockPlayerMovement && playerPickupController != null)
        {
            LockPlayerControls(true);
        }
        
        // Show UI with animation
        if (useFadeAnimations && canvasGroup != null)
        {
            StartCoroutine(FadeInUI());
        }
        else if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        // Trigger start events
        dialogue.onDialogueStart?.Invoke();
        OnDialogueStart?.Invoke(dialogue);
        
        // Display first line
        DisplayCurrentLine();
    }
    
    IEnumerator FadeInUI()
    {
        dialoguePanel.SetActive(true);
        canvasGroup.alpha = 0;
        
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeInTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1;
    }
    
    IEnumerator FadeOutUI()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeOutTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 0;
        
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
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerNameText.text));
        }
        
        if (portraitImage != null)
        {
            portraitImage.sprite = line.speakerPortrait;
            portraitImage.gameObject.SetActive(line.speakerPortrait != null && currentDialogue.showPortrait);
        }
        
        // Clear text
        if (dialogueText != null) dialogueText.text = "";
        
        // Trigger line start events
        line.onLineStart?.Invoke();
        OnLineStart?.Invoke(line);
        
        // Play voice clip
        if (voiceSource != null && line.voiceClip != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            voiceSource.Play();
        }
        
        // Start typing effect
        if (typewriterEffect && dialogueText != null)
        {
            typingCoroutine = StartCoroutine(TypeText(line.text, line));
        }
        else if (dialogueText != null)
        {
            dialogueText.text = line.text;
            isTyping = false;
            ShowContinuePrompt(line.requirePlayerInput);
            
            // Start auto-advance timer if needed
            if (!line.requirePlayerInput && line.displayTime > 0)
            {
                StartCoroutine(AutoAdvance(line.displayTime));
            }
        }
    }
    
    IEnumerator TypeText(string text, DialogueLine line)
    {
        isTyping = true;
        ShowContinuePrompt(false);
        
        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            
            // Play typing sound
            if (defaultTextSound != null)
            {
                PlayTypingSound();
            }
            
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
        
        // Trigger line end events
        line.onLineEnd?.Invoke();
        OnLineEnd?.Invoke(line);
        
        // Show continue prompt or auto-advance
        ShowContinuePrompt(line.requirePlayerInput);
        
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
            
            // Trigger line end events
            line.onLineEnd?.Invoke();
            OnLineEnd?.Invoke(line);
            
            ShowContinuePrompt(line.requirePlayerInput);
            
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
    
    void ShowContinuePrompt(bool show)
    {
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(show && !isTyping);
        }
    }
    
    void EndDialogue()
    {
        if (currentDialogue != null)
        {
            // Trigger end events
            currentDialogue.onDialogueEnd?.Invoke();
            OnDialogueEnd?.Invoke(currentDialogue);
            
            // Check for chained dialogue
            if (!string.IsNullOrEmpty(currentDialogue.nextDialogueID))
            {
                StartDialogue(currentDialogue.nextDialogueID);
                return;
            }
        }
        
        // Hide UI with animation
        if (useFadeAnimations && canvasGroup != null)
        {
            StartCoroutine(FadeOutUI());
        }
        
        
        // Unlock player controls
        LockPlayerControls(false);
        
        // Reset state
        currentDialogue = null;
        currentLineIndex = 0;
        isDialogueActive = false;
        isTyping = false;
        
        if (voiceSource != null) voiceSource.Stop();
    }
    
    void LockPlayerControls(bool lockControls)
    {
        if (playerPickupController != null)
        {
            // Disable player movement and camera if dialogue locks controls
            if (lockControls)
            {
                // Store current inspection state
                if (playerPickupController.IsInspecting)
                {
                    playerPickupController.ExitInspectMode();
                }
                
                // Disable player scripts
                if (playerPickupController.player_movement_script != null)
                    playerPickupController.player_movement_script.enabled = false;
                if (playerPickupController.player_camera_script != null)
                    playerPickupController.player_camera_script.enabled = false;
            }
            else
            {
                // Re-enable player scripts
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
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        if (backgroundPanel != null)
            backgroundPanel.color = backgroundColor;
    }
    
    public void SetTextColor(Color color)
    {
        dialogueTextColor = color;
        if (dialogueText != null)
            dialogueText.color = dialogueTextColor;
    }
    
    public void SetSpeakerColor(Color color)
    {
        speakerTextColor = color;
        if (speakerNameText != null)
            speakerNameText.color = speakerTextColor;
    }
    
    [ContextMenu("Test Simple Dialogue")]
    void TestSimpleDialogue()
    {
        ShowSimpleMessage("This is a test message with the new UI styling!", 3f);
    }
    
    [ContextMenu("Apply Dark Theme")]
    void ApplyDarkTheme()
    {
        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        dialogueTextColor = Color.white;
        speakerTextColor = new Color(1f, 0.8f, 0.3f, 1f);
        continueTextColor = new Color(1f, 1f, 1f, 0.7f);

    }
    
    [ContextMenu("Apply Parchment Theme")]
    void ApplyParchmentTheme()
    {
        backgroundColor = new Color(0.98f, 0.96f, 0.9f, 0.98f);
        dialogueTextColor = new Color(0.2f, 0.15f, 0.1f, 1f);
        speakerTextColor = new Color(0.5f, 0.2f, 0.1f, 1f);
        continueTextColor = new Color(0.3f, 0.2f, 0.1f, 0.8f);

    }
}