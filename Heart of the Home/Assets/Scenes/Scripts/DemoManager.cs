using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DemoManager : MonoBehaviour
{
    public static DemoManager Instance;
    
    [Header("Demo Settings")]
    public string mainMenuSceneName = "Main Menu";
    public string firstSceneName = "Basement";
    
    [Header("UI Settings")]
    public int fontSizeTitle = 48;
    public int fontSizeMessage = 24;
    public int fontSizeButton = 20;
    public Color panelColor = new Color(0, 0, 0, 0.95f);
    public Color buttonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color buttonHoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color textColor = Color.white;
    
    [Header("Office Progress")]
    public bool officeDoorUnlocked = false; // Tracks if Office exit door is unlocked
    
    [Header("Testing")]
    public bool testMode = false;
    public bool simulateDoorUnlocked = true;
    
    private GameObject demoEndPanel;
    private Canvas canvas;
    private Font defaultFont;
    
    // Track button states
    private bool isProcessingButton = false;
    
    void Awake()
    {
        Debug.Log("🎮 DemoManager Awake - Checking instance");
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            #if UNITY_EDITOR
            if (testMode) Debug.Log("🧪 TEST MODE ENABLED in Editor");
            #endif
            
            Debug.Log("✅ DemoManager instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("❌ Duplicate DemoManager destroyed");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        Debug.Log($"🎮 DemoManager Start - Current scene: {SceneManager.GetActiveScene().name}");
        
        // Load a default font
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            Debug.LogWarning("LegacyRuntime.ttf not found, trying Arial...");
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        CreateDemoEndUI();
        
        // Make sure screen is hidden at start
        HideDemoEndScreen();
        
        // Test mode auto-unlock
        if (testMode && SceneManager.GetActiveScene().name == "Office")
        {
            Invoke("TestUnlockDoor", 1f);
        }
    }
    
    void TestUnlockDoor()
    {
        officeDoorUnlocked = true;
        Debug.Log("🧪 TEST MODE: Office door auto-unlocked");
    }
    
    void CreateDemoEndUI()
    {
        Debug.Log("🎮 Creating Demo End UI...");
        
        // Check if canvas already exists
        GameObject existingCanvas = GameObject.Find("DemoEndCanvas");
        if (existingCanvas != null)
        {
            Destroy(existingCanvas);
        }
        
        // Create Canvas
        GameObject canvasGO = new GameObject("DemoEndCanvas");
        canvasGO.transform.SetParent(transform);
        
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        // Add Canvas Scaler for responsive UI
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add Graphic Raycaster for button clicks
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create the main panel
        demoEndPanel = CreatePanel(canvasGO, "DemoEndPanel", panelColor);
        
        // Ensure panel is a child of canvas
        demoEndPanel.transform.SetParent(canvasGO.transform, false);
        
        // Initially hidden
        demoEndPanel.SetActive(false);
        
        // Add title text
        CreateText(demoEndPanel, "TitleText", "DEMO COMPLETE", 
                   new Vector2(0, 200), fontSizeTitle, TextAnchor.MiddleCenter, textColor);
        
        // Add message text
        string message = "Thank you for playing The Heart of the Home Demo!\n\n" +
                        "You've completed all available content.\n" +
                        "Thank you for your support!";
        
        CreateText(demoEndPanel, "MessageText", message, 
                   new Vector2(0, 0), fontSizeMessage, TextAnchor.MiddleCenter, textColor);
        
        // Add buttons
        CreateButton(demoEndPanel, "MainMenuButton", "MAIN MENU", 
                     new Vector2(0, -150), () => HandleButtonPress(ReturnToMainMenu));
        
        CreateButton(demoEndPanel, "RestartButton", "RESTART DEMO", 
                     new Vector2(0, -220), () => HandleButtonPress(RestartDemo));
        
        CreateButton(demoEndPanel, "QuitButton", "QUIT GAME", 
                     new Vector2(0, -290), () => HandleButtonPress(QuitGame));
        
        Debug.Log("✅ Demo end UI created successfully");
    }
    
    GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        
        // Add Image component for background
        Image image = panel.AddComponent<Image>();
        image.color = color;
        
        // Make it stretch to full screen
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        return panel;
    }
    
    void CreateText(GameObject parent, string name, string content, Vector2 position, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);
        
        // Add Text component
        Text text = textGO.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.font = defaultFont;
        
        // Add outline or shadow for better visibility
        text.supportRichText = true;
        
        // Set size and position
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 200);
        rect.anchoredPosition = position;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
    
    void CreateButton(GameObject parent, string name, string buttonText, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        // Create button GameObject
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);
        
        // Add Button component
        Button button = buttonGO.AddComponent<Button>();
        
        // Add Image for button background
        Image image = buttonGO.AddComponent<Image>();
        image.color = buttonColor;
        image.raycastTarget = true;
        
        // Set button size and position
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(300, 60);
        buttonRect.anchoredPosition = position;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Create text for button
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        Text text = textGO.AddComponent<Text>();
        text.text = buttonText;
        text.fontSize = fontSizeButton;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = defaultFont;
        
        // Set text size and position - make it fill the button
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        button.onClick.AddListener(action);
        
        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = buttonColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }
    
    // Handle button presses and disable buttons
    void HandleButtonPress(UnityEngine.Events.UnityAction action)
    {
        // Prevent multiple button presses
        if (isProcessingButton) return;
        isProcessingButton = true;
        
        // Disable all buttons in the panel
        if (demoEndPanel != null)
        {
            Button[] buttons = demoEndPanel.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.interactable = false;
                
                // Optional: Change button color to gray when disabled
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }
            Debug.Log("All buttons disabled");
        }
        
        // Hide the end screen immediately
        HideDemoEndScreen();
        
        // Execute the action
        action.Invoke();
    }
    
    // Called when Office door is unlocked
    public void OfficeDoorUnlocked()
    {
        officeDoorUnlocked = true;
        Debug.Log("🚪 Office door unlocked - ready for demo end");
    }
    
    // Called when player tries to leave Office
    public bool TryLeaveOffice()
    {
        Debug.Log($"🚪 TryLeaveOffice called - Current scene: {SceneManager.GetActiveScene().name}, Door unlocked: {officeDoorUnlocked}");
        
        // Check if we're in Office and the door has been unlocked
        if (SceneManager.GetActiveScene().name == "Office" && officeDoorUnlocked)
        {
            Debug.Log("🎮 Conditions met - Showing demo end screen");
            ShowDemoEndScreen();
            return false;
        }
        
        Debug.Log("🚪 Conditions not met - Allowing normal door exit");
        return true;
    }
    
    public void ShowDemoEndScreen()
    {
        Debug.Log("🎮 ShowDemoEndScreen called");
        
        if (demoEndPanel != null)
        {
            demoEndPanel.SetActive(true);
            
            // Reset button states
            isProcessingButton = false;
            
            // Make sure all buttons are enabled when screen shows
            Button[] buttons = demoEndPanel.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.interactable = true;
                
                // Reset button color
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = buttonColor;
                }
            }
            
            // Make sure canvas is active and on top
            if (canvas != null)
            {
                canvas.sortingOrder = 999;
                canvas.gameObject.SetActive(true);
            }
            
            // Pause the game
            Time.timeScale = 0f;
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("✅ Demo end screen displayed with active buttons");
        }
        else
        {
            Debug.LogError("❌ demoEndPanel is null! Attempting to recreate UI...");
            CreateDemoEndUI();
            
            if (demoEndPanel != null)
            {
                demoEndPanel.SetActive(true);
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
    
    // Hide the end screen
    public void HideDemoEndScreen()
    {
        Debug.Log("🎮 Hiding demo end screen");
        
        if (demoEndPanel != null)
        {
            demoEndPanel.SetActive(false);
        }
        
        // Resume time if it was paused
        Time.timeScale = 1f;
        
        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // Button functions
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu - Button pressed");
        
        Time.timeScale = 1f;
        ResetDemoState();
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void RestartDemo()
    {
        Debug.Log("Restarting Demo from Basement - Button pressed");
        
        Time.timeScale = 1f;
        ResetDemoState();
        SceneManager.LoadScene(firstSceneName);
    }
    
    public void QuitGame()
    {
        Debug.Log("Game Quit - End of Demo - Button pressed");
        
        Time.timeScale = 1f;
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    void ResetDemoState()
    {
        officeDoorUnlocked = false;
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎮 Scene loaded: {scene.name}");
        
        // Always hide the end screen when a new scene loads
        HideDemoEndScreen();
        
        if (testMode && scene.name == "Office")
        {
            Invoke("TestUnlockDoor", 1f);
        }
    }
}