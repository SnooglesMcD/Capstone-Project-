using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DynamicKeypad : MonoBehaviour
{
    [Header("Settings")]
    public int requiredCodeLength = 4;
    public string correctCode = "1407"; // Default code, will be overridden by safe
    
    [Header("Appearance")]
    public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color buttonNormalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color buttonHighlightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color buttonPressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color displayNormalColor = Color.white;
    public Color displaySuccessColor = Color.green;
    public Color displayErrorColor = Color.red;
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    
    [Header("Font Settings")]
    public TMP_FontAsset fontAsset;
    public int titleFontSize = 24;
    public int displayFontSize = 36;
    public int buttonFontSize = 24;
    public int statusFontSize = 18;
    
    // UI Elements (created at runtime)
    private Canvas keypadCanvas;
    private GameObject keypadPanel;
    private TextMeshProUGUI displayText;
    private TextMeshProUGUI statusText;
    private List<Button> numberButtons = new List<Button>();
    private Button clearButton;
    private Button enterButton;
    private Button closeButton; // Added this
    
    // State
    private string currentInput = "";
    private bool isActive = false;
    private SafeController targetSafe;
    private AudioSource audioSource;
    private Coroutine flashCoroutine;
    
    // Animation
    private Vector3 originalPanelScale;
    private Image panelBackground;
    
    void Start()
    {
        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Create the UI
        CreateKeypadUI();
        
        // Hide initially
        keypadCanvas.gameObject.SetActive(false);
        
        Debug.Log("Dynamic Keypad initialized - UI created in code");
    }
    
    void Update()
    {
        if (!isActive) return;
        
        HandleKeyboardInput();
        
        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseKeypad();
        }
    }
    
    void CreateKeypadUI()
    {
        
        CreateCanvas();
       
        CreateMainPanel();
        
        CreateTitle();
       
        CreateDisplay();
        
        CreateStatusText();
        
        CreateKeypadGrid();
        
        CreateCloseButton();
    }
    
    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("KeypadCanvas");
        keypadCanvas = canvasObj.AddComponent<Canvas>();
        keypadCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        keypadCanvas.sortingOrder = 1000; // High priority
        
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // Fixed this line
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
        
        // Add GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Make it a child of this object
        canvasObj.transform.SetParent(transform);
    }
    
    void CreateMainPanel()
    {
        keypadPanel = new GameObject("KeypadPanel");
        keypadPanel.transform.SetParent(keypadCanvas.transform);
        
        // Add Image component for background
        panelBackground = keypadPanel.AddComponent<Image>();
        panelBackground.color = panelColor;
        
        RectTransform rect = keypadPanel.GetComponent<RectTransform>();
        
        // Center the panel
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
        
        // Set size that adapts to screen
        float baseWidth = 850f;
        float baseHeight = 950f;
        
        // Scale based on screen height
        float screenHeight = Screen.height;
        float scaleFactor = Mathf.Clamp(screenHeight / 1080f, 0.7f, 1.3f);
        
        rect.sizeDelta = new Vector2(baseWidth * scaleFactor, baseHeight * scaleFactor);
        rect.anchoredPosition = Vector2.zero; // Center position
        
        // Add layout
        VerticalLayoutGroup layout = keypadPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 20 * scaleFactor; // Scale spacing too
        layout.childAlignment = TextAnchor.MiddleCenter; // Center children
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Add ContentSizeFitter to adapt to content
        ContentSizeFitter sizeFitter = keypadPanel.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        originalPanelScale = rect.localScale;
    }
    
    void CreateTitle()
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(keypadPanel.transform);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ENTER SAFE CODE";
        titleText.fontSize = titleFontSize;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            titleText.font = fontAsset;
        }
        
        // Set size
        RectTransform rect = titleObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);
    }
    
    void CreateDisplay()
    {
        // Create display background
        GameObject displayPanel = new GameObject("DisplayPanel");
        displayPanel.transform.SetParent(keypadPanel.transform);
        
        Image bg = displayPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        // Add layout
        HorizontalLayoutGroup layout = displayPanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        
        // Create display text
        GameObject displayTextObj = new GameObject("DisplayText");
        displayTextObj.transform.SetParent(displayPanel.transform);
        
        displayText = displayTextObj.AddComponent<TextMeshProUGUI>();
        displayText.text = "─ ─ ─ ─";
        displayText.fontSize = displayFontSize;
        displayText.color = displayNormalColor;
        displayText.alignment = TextAlignmentOptions.Center;
        displayText.characterSpacing = 20;
        
        if (fontAsset != null)
        {
            displayText.font = fontAsset;
        }
        
        // Set sizes
        RectTransform panelRect = displayPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 80);
        
        RectTransform textRect = displayTextObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(360, 60);
    }
    
    void CreateStatusText()
    {
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(keypadPanel.transform);
        
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "ENTER 4-DIGIT CODE";
        statusText.fontSize = statusFontSize;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            statusText.font = fontAsset;
        }
        
        RectTransform rect = statusObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 30);
    }
    
    void CreateKeypadGrid()
    {
        // Create grid container
        GameObject gridObj = new GameObject("KeypadGrid");
        gridObj.transform.SetParent(keypadPanel.transform);
        
        // Add Grid Layout
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 100);
        grid.spacing = new Vector2(20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // Set size
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(400, 360);
        
        // Create number buttons 1-9
        for (int i = 1; i <= 9; i++)
        {
            CreateNumberButton(gridObj.transform, i);
        }
        
        // Create bottom row (0, Clear, Enter)
        CreateNumberButton(gridObj.transform, 0);
        CreateClearButton(gridObj.transform);
        CreateEnterButton(gridObj.transform);
    }
    
    void CreateNumberButton(Transform parent, int number)
    {
        GameObject buttonObj = new GameObject($"Btn{number}");
        buttonObj.transform.SetParent(parent);
        
        // Add Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonNormalColor;
        
        // Add Button
        Button button = buttonObj.AddComponent<Button>();
        
        // Set button colors
        ColorBlock colors = button.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHighlightColor;
        button.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = number.ToString();
        buttonText.fontSize = buttonFontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            buttonText.font = fontAsset;
        }
        
        // Center text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        button.onClick.AddListener(() => AddDigit(number));
        
        // Store reference
        numberButtons.Add(button);
    }
    
    void CreateClearButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("BtnClear");
        buttonObj.transform.SetParent(parent);
        
        // Add Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.4f, 0.2f, 1f);
        
        // Add Button
        clearButton = buttonObj.AddComponent<Button>();
        
        // Set button colors
        ColorBlock colors = clearButton.colors;
        colors.normalColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.5f, 0.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.3f, 0.1f, 1f);
        clearButton.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "C";
        buttonText.fontSize = buttonFontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            buttonText.font = fontAsset;
        }
        
        // Center text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        clearButton.onClick.AddListener(ClearInput);
    }
    
    void CreateEnterButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("BtnEnter");
        buttonObj.transform.SetParent(parent);
        
        // Add Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        
        // Add Button
        enterButton = buttonObj.AddComponent<Button>();
        
        // Set button colors
        ColorBlock colors = enterButton.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.1f, 1f);
        enterButton.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "✓";
        buttonText.fontSize = buttonFontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            buttonText.font = fontAsset;
        }
        
        // Center text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        enterButton.onClick.AddListener(SubmitCode);
    }
    
    void CreateCloseButton()
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(keypadPanel.transform);
        
        // Position in top-right corner
        RectTransform closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        closeRect.sizeDelta = new Vector2(30, 30);
        
        // Add Image
        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        // Add Button
        closeButton = closeObj.AddComponent<Button>();
        
        // Set button colors
        ColorBlock colors = closeButton.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        closeButton.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeObj.transform);
        
        TextMeshProUGUI closeText = textObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 18;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;
        
        if (fontAsset != null)
        {
            closeText.font = fontAsset;
        }
        
        // Center text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        closeButton.onClick.AddListener(CloseKeypad);
    }
    
    void HandleKeyboardInput()
    {
        // Number keys 0-9
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                AddDigit(i);
                return;
            }
        }
        
        // Enter key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitCode();
            return;
        }
        
        // Backspace/Clear
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ClearInput();
            return;
        }
    }
    
    public void ShowKeypad(SafeController safe)
    {
        if (isActive || safe == null || safe.IsUnlocked()) return;
        
        targetSafe = safe;
        correctCode = safe.correctCode.ToString("D4");
        isActive = true;
        
        // Reset input
        currentInput = "";
        UpdateDisplay();
        
        // Show UI
        keypadCanvas.gameObject.SetActive(true);
        
        // Ensure panel is centered and sized correctly
        AdaptToScreenSize();
        
        // Animation
        StartCoroutine(AnimateOpen());
        
        // Disable player controls
        DisablePlayerControls(true);
        
        // Set cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log($"Keypad activated for safe (Code: {correctCode})");
    }
    
    // ADDED THIS METHOD - Was missing!
    public void CloseKeypad()
    {
        if (!isActive) return;
        
        isActive = false;
        targetSafe = null;
        
        // Hide UI
        keypadCanvas.gameObject.SetActive(false);
        
        // Re-enable player controls
        DisablePlayerControls(false);
        
        // Reset cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("Keypad closed");
    }
    
    void AddDigit(int digit)
    {
        if (currentInput.Length >= requiredCodeLength) return;
        
        currentInput += digit.ToString();
        PlaySound(buttonClickSound);
        UpdateDisplay();
        
        // Auto-submit when full
        if (currentInput.Length == requiredCodeLength)
        {
            SubmitCode();
        }
    }
    
    void ClearInput()
    {
        if (currentInput.Length == 0) return;
        
        currentInput = "";
        PlaySound(buttonClickSound);
        UpdateDisplay();
    }
    
    void SubmitCode()
    {
        if (currentInput.Length != requiredCodeLength || targetSafe == null) return;
        
        if (currentInput == correctCode)
        {
            OnCorrectCode();
        }
        else
        {
            OnIncorrectCode();
        }
    }
    
    void OnCorrectCode()
    {
        PlaySound(correctSound);
        
        // Visual feedback
        statusText.text = "CODE ACCEPTED";
        statusText.color = displaySuccessColor;
        displayText.color = displaySuccessColor;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashPanel(displaySuccessColor));
        
        // Unlock the safe
        targetSafe.UnlockSafe();
        
        // Notify puzzle manager
        OfficePuzzleManager puzzleManager = FindObjectOfType<OfficePuzzleManager>();
        if (puzzleManager != null)
        {
            puzzleManager.OnSafeOpened(targetSafe.correctCode);
        }
        
        // Close after delay
        StartCoroutine(CloseAfterDelay(1.5f));
        
        Debug.Log($"Correct code entered: {currentInput}");
    }
    
    void OnIncorrectCode()
    {
        PlaySound(incorrectSound);
        
        // Visual feedback
        statusText.text = "INCORRECT CODE";
        statusText.color = displayErrorColor;
        displayText.color = displayErrorColor;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashPanel(displayErrorColor));
        
        // Clear input after delay
        StartCoroutine(ClearAfterDelay(1f));
        
        Debug.Log($"Incorrect code entered: {currentInput}");
    }
    
    void UpdateDisplay()
    {
        if (displayText != null)
        {
            // Build display string with bullets for entered digits
            string display = "";
            for (int i = 0; i < currentInput.Length; i++)
            {
                display += "●";
            }
            for (int i = currentInput.Length; i < requiredCodeLength; i++)
            {
                display += "─";
            }
            
            // Add spacing between characters
            displayText.text = string.Join(" ", display.ToCharArray());
            displayText.color = displayNormalColor;
        }
        
        // Update status text
        if (statusText != null && currentInput.Length == 0)
        {
            statusText.text = $"ENTER {requiredCodeLength}-DIGIT CODE";
            statusText.color = Color.white;
        }
    }
    
    void AdaptToScreenSize()
    {
        if (keypadPanel == null) return;
        
        RectTransform rect = keypadPanel.GetComponent<RectTransform>();
        if (rect == null) return;
        
        // Get current screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float screenRatio = screenHeight / 1080f; // Base on 1080p height
        
        // Calculate adaptive size
        float minWidth = 850;
        float maxWidth = 900f;
        float minHeight = 950;
        float maxHeight = 1000f;
        
        // Base size with screen adaptation
        float targetWidth = Mathf.Clamp(850f * screenRatio, minWidth, maxWidth);
        float targetHeight = Mathf.Clamp(950f * screenRatio, minHeight, maxHeight);
        
        // Apply size
        rect.sizeDelta = new Vector2(targetWidth, targetHeight);
        
        // Ensure centered position
        rect.anchoredPosition = Vector2.zero;
        
        // Adjust font sizes if needed
        if (displayText != null)
        {
            displayText.fontSize = Mathf.RoundToInt(36f * screenRatio);
        }
        
        if (statusText != null)
        {
            statusText.fontSize = Mathf.RoundToInt(18f * screenRatio);
        }
        
        // Adjust button font sizes
        foreach (Button btn in numberButtons)
        {
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = Mathf.RoundToInt(24f * screenRatio);
            }
        }
        
        // Adjust close button if it exists
        if (closeButton != null)
        {
            TextMeshProUGUI closeText = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (closeText != null)
            {
                closeText.fontSize = Mathf.RoundToInt(18f * screenRatio);
            }
        }
    }
    
    IEnumerator AnimateOpen()
    {
        float duration = 0.2f;
        Vector3 startScale = originalPanelScale * 0.5f;
        keypadPanel.transform.localScale = startScale;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            keypadPanel.transform.localScale = Vector3.Lerp(startScale, originalPanelScale, progress);
            yield return null;
        }
        
        keypadPanel.transform.localScale = originalPanelScale;
    }
    
    IEnumerator FlashPanel(Color flashColor)
    {
        if (panelBackground == null) yield break;
        
        Color originalColor = panelBackground.color;
        float flashDuration = 0.3f;
        
        // Flash to target color
        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            panelBackground.color = Color.Lerp(originalColor, flashColor, t / flashDuration);
            yield return null;
        }
        
        // Return to original
        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            panelBackground.color = Color.Lerp(flashColor, originalColor, t / flashDuration);
            yield return null;
        }
        
        panelBackground.color = originalColor;
        flashCoroutine = null;
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseKeypad();
    }
    
    IEnumerator ClearAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearInput();
        
        if (statusText != null)
        {
            statusText.text = $"ENTER {requiredCodeLength}-DIGIT CODE";
            statusText.color = Color.white;
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void DisablePlayerControls(bool disable)
    {
        // Find player controller
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        if (playerController != null)
        {
            playerController.enabled = !disable;
        }
        
        // Find pickup controller
        PickupController pickupController = FindObjectOfType<PickupController>();
        if (pickupController != null)
        {
            pickupController.enabled = !disable;
        }
    }
    
    public bool IsActive()
    {
        return isActive;
    }
    
    void OnDestroy()
    {
        // Clean up
        if (keypadCanvas != null)
        {
            Destroy(keypadCanvas.gameObject);
        }
    }
}