using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Book UI Settings")]
    public Sprite bookCoverSprite;
    public Color bookBackgroundColor = new Color(0.2f, 0.15f, 0.1f, 1f);
    public Color pageColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    public Color textColor = Color.black;
    public Color buttonColor = new Color(0.2f, 0.15f, 0.1f, 0.9f);
    public Color buttonHoverColor = new Color(0.3f, 0.25f, 0.2f, 1f);
    
    [Header("Text Settings")]
    public int titleFontSize = 48;
    public int buttonFontSize = 32;
    public int minFontSize = 14;
    public float textPaddingPercentage = 0.05f;
    
    // UI References
    private GameObject pauseBookUI;
    private CanvasScaler canvasScaler;
    private CanvasGroup bookCanvasGroup;
    private Image bookBackground;
    private Image leftPage;
    private Image rightPage;
    private TextMeshProUGUI titleText;
    private Button resumeButton;
    private Button quitButton;
    private TextMeshProUGUI resumeButtonText;
    private TextMeshProUGUI quitButtonText;
    
    // State
    private bool is_paused = false;
    public FirstPersonController playerController;
    public PickupController pickupController; // Reference to PickupController
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (is_paused)
                Resume();
            else
                Pause();
        }
    }
    
    void CreateBookUI()
    {
        if (pauseBookUI != null)
        {
            pauseBookUI.SetActive(true);
            return;
        }
        
        // Create Canvas
        pauseBookUI = new GameObject("PauseBookUI");
        Canvas canvas = pauseBookUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Very high to ensure it's on top
        
        pauseBookUI.AddComponent<GraphicRaycaster>();
        
        // Full screen background
        GameObject background = new GameObject("BookBackground");
        background.transform.SetParent(pauseBookUI.transform);
        bookBackground = background.AddComponent<Image>();
        bookBackground.color = bookBackgroundColor;
        RectTransform bgRT = bookBackground.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        
        // Book container that fills the screen
        GameObject bookContainer = new GameObject("BookContainer");
        bookContainer.transform.SetParent(background.transform);
        RectTransform containerRT = bookContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;
        
        // Left page - takes up left half of screen
        GameObject leftPageObj = new GameObject("LeftPage");
        leftPageObj.transform.SetParent(bookContainer.transform);
        leftPage = leftPageObj.AddComponent<Image>();
        leftPage.color = pageColor;
        leftPage.raycastTarget = false;
        RectTransform leftPageRT = leftPage.rectTransform;
        leftPageRT.anchorMin = new Vector2(0, 0);
        leftPageRT.anchorMax = new Vector2(0.5f, 1);
        leftPageRT.offsetMin = new Vector2(20, 20);
        leftPageRT.offsetMax = new Vector2(-10, -20);
        
        // Right page - takes up right half of screen
        GameObject rightPageObj = new GameObject("RightPage");
        rightPageObj.transform.SetParent(bookContainer.transform);
        rightPage = rightPageObj.AddComponent<Image>();
        rightPage.color = pageColor;
        rightPage.raycastTarget = false;
        RectTransform rightPageRT = rightPage.rectTransform;
        rightPageRT.anchorMin = new Vector2(0.5f, 0);
        rightPageRT.anchorMax = Vector2.one;
        rightPageRT.offsetMin = new Vector2(10, 20);
        rightPageRT.offsetMax = new Vector2(-20, -20);
        
        // Title on Left Page
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(leftPageObj.transform);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.color = textColor;
        titleText.fontSize = titleFontSize;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "PAUSED";
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(1, 0.9f);
        titleRT.offsetMin = new Vector2(50, 0);
        titleRT.offsetMax = new Vector2(-50, 0);
        
        // Resume Button on Right Page
        GameObject resumeBtnObj = new GameObject("ResumeButton");
        resumeBtnObj.transform.SetParent(rightPageObj.transform);
        resumeButton = resumeBtnObj.AddComponent<Button>();
        
        Image resumeBg = resumeBtnObj.AddComponent<Image>();
        resumeBg.color = buttonColor;
        
        RectTransform resumeRT = resumeBtnObj.GetComponent<RectTransform>();
        resumeRT.anchorMin = new Vector2(0.2f, 0.6f);
        resumeRT.anchorMax = new Vector2(0.8f, 0.8f);
        resumeRT.offsetMin = Vector2.zero;
        resumeRT.offsetMax = Vector2.zero;
        
        GameObject resumeTextObj = new GameObject("ResumeText");
        resumeTextObj.transform.SetParent(resumeBtnObj.transform);
        resumeButtonText = resumeTextObj.AddComponent<TextMeshProUGUI>();
        resumeButtonText.text = "RESUME (ESC)";
        resumeButtonText.fontSize = buttonFontSize;
        resumeButtonText.alignment = TextAlignmentOptions.Center;
        resumeButtonText.color = Color.white;
        resumeButtonText.fontStyle = FontStyles.Bold;
        
        RectTransform resumeTextRT = resumeButtonText.GetComponent<RectTransform>();
        resumeTextRT.anchorMin = Vector2.zero;
        resumeTextRT.anchorMax = Vector2.one;
        resumeTextRT.offsetMin = Vector2.zero;
        resumeTextRT.offsetMax = Vector2.zero;
        
        resumeButton.onClick.AddListener(Resume);
        
        ColorBlock resumeColors = resumeButton.colors;
        resumeColors.normalColor = buttonColor;
        resumeColors.highlightedColor = buttonHoverColor;
        resumeColors.pressedColor = new Color(0.4f, 0.35f, 0.3f, 1f);
        resumeButton.colors = resumeColors;
        
        // Quit Button on Right Page
        GameObject quitBtnObj = new GameObject("QuitButton");
        quitBtnObj.transform.SetParent(rightPageObj.transform);
        quitButton = quitBtnObj.AddComponent<Button>();
        
        Image quitBg = quitBtnObj.AddComponent<Image>();
        quitBg.color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        
        RectTransform quitRT = quitBtnObj.GetComponent<RectTransform>();
        quitRT.anchorMin = new Vector2(0.2f, 0.3f);
        quitRT.anchorMax = new Vector2(0.8f, 0.5f);
        quitRT.offsetMin = Vector2.zero;
        quitRT.offsetMax = Vector2.zero;
        
        GameObject quitTextObj = new GameObject("QuitText");
        quitTextObj.transform.SetParent(quitBtnObj.transform);
        quitButtonText = quitTextObj.AddComponent<TextMeshProUGUI>();
        quitButtonText.text = "QUIT TO MENU";
        quitButtonText.fontSize = buttonFontSize;
        quitButtonText.alignment = TextAlignmentOptions.Center;
        quitButtonText.color = Color.white;
        quitButtonText.fontStyle = FontStyles.Bold;
        
        RectTransform quitTextRT = quitButtonText.GetComponent<RectTransform>();
        quitTextRT.anchorMin = Vector2.zero;
        quitTextRT.anchorMax = Vector2.one;
        quitTextRT.offsetMin = Vector2.zero;
        quitTextRT.offsetMax = Vector2.zero;
        
        quitButton.onClick.AddListener(QuitGame);
        
        ColorBlock quitColors = quitButton.colors;
        quitColors.normalColor = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        quitColors.highlightedColor = new Color(0.4f, 0.15f, 0.15f, 1f);
        quitColors.pressedColor = new Color(0.5f, 0.2f, 0.2f, 1f);
        quitButton.colors = quitColors;
        
        // Decorative text on Left Page
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(leftPageObj.transform);
        TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
        hintText.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        hintText.fontSize = Mathf.Max(buttonFontSize - 8, minFontSize);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.text = "Heart of the Home";
        hintText.fontStyle = FontStyles.Italic;
        
        RectTransform hintRT = hintText.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0.1f);
        hintRT.anchorMax = new Vector2(1, 0.3f);
        hintRT.offsetMin = new Vector2(50, 0);
        hintRT.offsetMax = new Vector2(-50, 0);
        
        Debug.Log("Full-screen pause menu UI created");
        Debug.Log($"Screen size: {Screen.width}x{Screen.height}");
    }
    
    void Pause()
    {
        if (is_paused) return;
        
        is_paused = true;
        Time.timeScale = 0f;
        
        CreateBookUI();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (playerController != null)
            playerController.enabled = false;
            
        // Notify PickupController that game is paused
        if (pickupController != null)
            pickupController.OnGamePaused();
        
        Debug.Log("Game Paused - Full screen book UI should be visible");
    }
    
    public void Resume()
    {
        if (!is_paused) return;
        
        is_paused = false;
        Time.timeScale = 1f;
        
        if (pauseBookUI != null)
            pauseBookUI.SetActive(false);
            
        // Handle resuming based on whether we're in inspect mode
        HandleResumeState();
        
        Debug.Log("Game Resumed");
    }
    
    void HandleResumeState()
    {
        if (pickupController != null)
        {
            // Let PickupController handle the resume logic
            pickupController.OnGameResumed();
            
            // Check if controls should be enabled (not in inspect mode)
            bool shouldEnableControls = pickupController.ShouldControlsBeEnabled();
            
            if (shouldEnableControls)
            {
                // Normal resume - not inspecting
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
                if (playerController != null)
                    playerController.enabled = true;
            }
            else
            {
                // We're still in inspect mode
                // Don't change cursor or enable player controller
                // PickupController already handles this in OnGameResumed()
            }
        }
        else
        {
            // Fallback: If no pickup controller, use default behavior
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (playerController != null)
                playerController.enabled = true;
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting to main menu...");
        Resume(); // Resume the game first
        SceneManager.LoadScene("Main menu");
    }
    
    void OnDestroy()
    {
        if (pauseBookUI != null) 
            Destroy(pauseBookUI);
    }
}