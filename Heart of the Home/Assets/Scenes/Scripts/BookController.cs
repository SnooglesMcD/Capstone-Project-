using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class BookController : MonoBehaviour
{
    [Header("Book Reading Settings")]
    public string bookDialogueId = "book_read";
    public bool canBeRead = true;
    
    [Header("Book UI - Responsive")]
    public GameObject bookUIPrefab;
    public Sprite bookCoverSprite;
    public Color bookBackgroundColor = new Color(0.95f, 0.92f, 0.8f, 1f);
    public Color pageColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    public Color textColor = Color.black;
    
    [Header("Page Settings")]
    public int maxCharsPerPage = 500;
    public string pageTurnSoundId = "page_turn";
    public string bookOpenSoundId = "book_open";
    public string bookCloseSoundId = "book_close";
    
    [Header("Responsive Settings")]
    [Range(0.5f, 0.9f)]
    public float screenWidthPercentage = 0.8f;
    [Range(0.5f, 0.9f)]
    public float screenHeightPercentage = 0.8f;
    public float minBookWidth = 600f;
    public float minBookHeight = 400f;
    public float maxBookWidth = 1200f;
    public float maxBookHeight = 800f;
    
    [Header("Text Settings")]
    public int baseFontSize = 18;
    public int minFontSize = 14;
    public int maxFontSize = 28;
    public float textPaddingPercentage = 0.05f;
    
    [Header("Visual Effects")]
    public float openCloseSpeed = 0.5f;
    public float pageTurnSpeed = 0.3f;
    public ParticleSystem pageTurnParticles;
    
    [Header("Holding Detection")]
    public bool requireHolding = true;
    
    [Header("Fallback Content")]
    [TextArea(5, 20)]
    public string fallbackBookContent = "This is a mysterious book you found. Its pages are filled with handwritten notes and drawings. The ink is faded in places, making some words difficult to read. You sense there might be important clues hidden within these pages...";
    
    // UI References
    private GameObject bookUI;
    private CanvasScaler canvasScaler;
    private CanvasGroup bookCanvasGroup;
    private Image bookBackground;
    private Image leftPage;
    private Image rightPage;
    private TextMeshProUGUI leftText;
    private TextMeshProUGUI rightText;
    private TextMeshProUGUI pageNumberText;
    private Button nextPageButton;
    private Button prevPageButton;
    private Button closeButton;
    private TextMeshProUGUI prevButtonText;
    private TextMeshProUGUI nextButtonText;
    
    // Responsive values
    private float currentBookWidth;
    private float currentBookHeight;
    private float currentFontSize;
    private float currentPadding;
    
    // Book state
    public bool isBeingRead = false;
    private bool isBeingHeld = false;
    private PickupController pickupController;
    private string[] pages;
    private int currentPage = 0;
    private Coroutine openCloseCoroutine;
    private Coroutine pageTurnCoroutine;
    
    // Audio
    private AudioSource audioSource;
    
    void Start()
    {
        pickupController = FindObjectOfType<PickupController>();
        if (pickupController == null)
        {
            Debug.LogWarning("No PickupController found in scene.");
        }
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        Debug.Log($"BookController initialized for {gameObject.name}");
    }
    
    void Update()
    {
        UpdateHoldingStatus();
        
        if (Input.GetKeyDown(KeyCode.X) && !isBeingRead && canBeRead && IsBeingHeld())
        {
            StartReading();
        }
        
        if (isBeingRead && Input.GetKeyDown(KeyCode.Escape))
        {
            StopReading();
        }
        
        if (isBeingRead)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PreviousPage();
            }
            
            if (bookUI != null && bookUI.activeInHierarchy)
            {
                UpdateUILayout();
            }
        }
    }
    
    void UpdateHoldingStatus()
    {
        if (pickupController == null) 
        {
            pickupController = FindObjectOfType<PickupController>();
            return;
        }
        
        isBeingHeld = (pickupController.HeldObject == gameObject);
    }
    
    bool IsBeingHeld()
    {
        if (!requireHolding) return true;
        if (pickupController == null) return false;
        return pickupController.HeldObject == gameObject;
    }
    
    public void StartReading()
    {
        if (isBeingRead || !canBeRead) return;
        
        if (requireHolding && !IsBeingHeld())
        {
            Debug.Log($"Cannot read {gameObject.name} - not being held.");
            return;
        }
        
        isBeingRead = true;
        Debug.Log($"Opening {gameObject.name} to read");
        
        CalculateResponsiveDimensions();
        CreateBookUI();
        LoadBookContent();
        PlaySound(bookOpenSoundId);
        DisablePlayerControls(true);
        
        if (openCloseCoroutine != null) StopCoroutine(openCloseCoroutine);
        openCloseCoroutine = StartCoroutine(AnimateBookOpen());
        
        UpdateReadingPrompt(true);
    }
    
    public void StopReading()
    {
        if (!isBeingRead) return;
        
        isBeingRead = false;
        Debug.Log($"Closing {gameObject.name}");
        
        PlaySound(bookCloseSoundId);
        
        if (openCloseCoroutine != null) StopCoroutine(openCloseCoroutine);
        openCloseCoroutine = StartCoroutine(AnimateBookClose());
        
        StartCoroutine(ReenableControlsAfterDelay(openCloseSpeed));
        UpdateReadingPrompt(false);
    }
    
    void CalculateResponsiveDimensions()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        currentBookWidth = screenWidth * screenWidthPercentage;
        currentBookHeight = screenHeight * screenHeightPercentage;
        
        currentBookWidth = Mathf.Clamp(currentBookWidth, minBookWidth, maxBookWidth);
        currentBookHeight = Mathf.Clamp(currentBookHeight, minBookHeight, maxBookHeight);
        
        float screenRatio = screenHeight / 1080f;
        currentFontSize = Mathf.RoundToInt(baseFontSize * screenRatio);
        currentFontSize = Mathf.Clamp(currentFontSize, minFontSize, maxFontSize);
        
        currentPadding = currentBookWidth * textPaddingPercentage;
        
        Debug.Log($"Responsive dimensions: Book={currentBookWidth}x{currentBookHeight}, Font={currentFontSize}, Padding={currentPadding}");
    }
    
    void UpdateUILayout()
    {
        if (bookUI == null) return;
        
        CalculateResponsiveDimensions();
        
        RectTransform containerRT = bookUI.transform.Find("BookBackground/BookContainer")?.GetComponent<RectTransform>();
        if (containerRT != null)
        {
            containerRT.sizeDelta = new Vector2(currentBookWidth, currentBookHeight);
        }
        
        if (leftText != null) leftText.fontSize = currentFontSize;
        if (rightText != null) rightText.fontSize = currentFontSize;
        if (pageNumberText != null) pageNumberText.fontSize = Mathf.Max(currentFontSize - 4, minFontSize);
        if (prevButtonText != null) prevButtonText.fontSize = Mathf.Max(currentFontSize - 2, minFontSize);
        if (nextButtonText != null) nextButtonText.fontSize = Mathf.Max(currentFontSize - 2, minFontSize);
        
        // Update text padding using RectTransform
        float padding = currentPadding;
        if (leftText != null)
        {
            RectTransform leftTextRT = leftText.GetComponent<RectTransform>();
            if (leftTextRT != null)
            {
                leftTextRT.offsetMin = new Vector2(padding, padding * 1.5f);
                leftTextRT.offsetMax = new Vector2(-padding, -padding * 0.5f);
            }
        }
        
        if (rightText != null)
        {
            RectTransform rightTextRT = rightText.GetComponent<RectTransform>();
            if (rightTextRT != null)
            {
                rightTextRT.offsetMin = new Vector2(padding, padding * 1.5f);
                rightTextRT.offsetMax = new Vector2(-padding, -padding * 0.5f);
            }
        }
        
        float buttonWidth = Mathf.Max(80f, currentBookWidth * 0.08f);
        float buttonHeight = Mathf.Max(100f, currentBookHeight * 0.15f);
        
        if (prevPageButton != null)
        {
            RectTransform prevRT = prevPageButton.GetComponent<RectTransform>();
            if (prevRT != null)
            {
                prevRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                prevRT.anchoredPosition = new Vector2(-buttonWidth * 0.2f, 0);
            }
        }
        
        if (nextPageButton != null)
        {
            RectTransform nextRT = nextPageButton.GetComponent<RectTransform>();
            if (nextRT != null)
            {
                nextRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                nextRT.anchoredPosition = new Vector2(buttonWidth * 0.2f, 0);
            }
        }
        
        if (closeButton != null)
        {
            RectTransform closeRT = closeButton.GetComponent<RectTransform>();
            if (closeRT != null)
            {
                float closeSize = Mathf.Max(60f, currentBookHeight * 0.08f);
                closeRT.sizeDelta = new Vector2(closeSize, closeSize);
            }
        }
    }
    
    void CreateBookUI()
    {
        if (bookUI != null)
        {
            bookUI.SetActive(true);
            UpdateUILayout();
            UpdatePageDisplay();
            return;
        }
        
        bookUI = new GameObject("BookUI");
        Canvas canvas = bookUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        canvasScaler = bookUI.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        
        bookCanvasGroup = bookUI.AddComponent<CanvasGroup>();
        bookCanvasGroup.alpha = 0;
        
        bookUI.AddComponent<GraphicRaycaster>();
        
        GameObject background = new GameObject("BookBackground");
        background.transform.SetParent(bookUI.transform);
        bookBackground = background.AddComponent<Image>();
        bookBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRT = bookBackground.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        
        GameObject bookContainer = new GameObject("BookContainer");
        bookContainer.transform.SetParent(background.transform);
        RectTransform containerRT = bookContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.5f);
        containerRT.anchorMax = new Vector2(0.5f, 0.5f);
        containerRT.pivot = new Vector2(0.5f, 0.5f);
        containerRT.sizeDelta = new Vector2(currentBookWidth, currentBookHeight);
        containerRT.anchoredPosition = Vector2.zero;
        
        GameObject bookObj = new GameObject("Book");
        bookObj.transform.SetParent(bookContainer.transform);
        Image bookImage = bookObj.AddComponent<Image>();
        bookImage.color = bookBackgroundColor;
        RectTransform bookRT = bookImage.rectTransform;
        bookRT.anchorMin = Vector2.zero;
        bookRT.anchorMax = Vector2.one;
        bookRT.offsetMin = Vector2.zero;
        bookRT.offsetMax = Vector2.zero;
        
        if (bookCoverSprite != null)
        {
            bookImage.sprite = bookCoverSprite;
            bookImage.type = Image.Type.Sliced;
            bookImage.pixelsPerUnitMultiplier = 0.5f;
        }
        
        GameObject leftPageObj = new GameObject("LeftPage");
        leftPageObj.transform.SetParent(bookObj.transform);
        leftPage = leftPageObj.AddComponent<Image>();
        leftPage.color = pageColor;
        RectTransform leftPageRT = leftPage.rectTransform;
        leftPageRT.anchorMin = new Vector2(0, 0);
        leftPageRT.anchorMax = new Vector2(0.5f, 1);
        leftPageRT.offsetMin = new Vector2(20, 20);
        leftPageRT.offsetMax = new Vector2(-10, -20);
        
        GameObject rightPageObj = new GameObject("RightPage");
        rightPageObj.transform.SetParent(bookObj.transform);
        rightPage = rightPageObj.AddComponent<Image>();
        rightPage.color = pageColor;
        RectTransform rightPageRT = rightPage.rectTransform;
        rightPageRT.anchorMin = new Vector2(0.5f, 0);
        rightPageRT.anchorMax = Vector2.one;
        rightPageRT.offsetMin = new Vector2(10, 20);
        rightPageRT.offsetMax = new Vector2(-20, -20);
        
        // Left text with RectTransform padding (FIXED)
        GameObject leftTextObj = new GameObject("LeftText");
        leftTextObj.transform.SetParent(leftPageObj.transform);
        leftText = leftTextObj.AddComponent<TextMeshProUGUI>();
        leftText.color = textColor;
        leftText.fontSize = currentFontSize;
        leftText.alignment = TextAlignmentOptions.TopLeft;
        leftText.enableWordWrapping = true;
        leftText.overflowMode = TextOverflowModes.Overflow;
        
        RectTransform leftTextRT = leftText.GetComponent<RectTransform>();
        leftTextRT.anchorMin = Vector2.zero;
        leftTextRT.anchorMax = Vector2.one;
        leftTextRT.offsetMin = new Vector2(currentPadding, currentPadding * 1.5f);
        leftTextRT.offsetMax = new Vector2(-currentPadding, -currentPadding * 0.5f);
        
        // Right text with RectTransform padding (FIXED)
        GameObject rightTextObj = new GameObject("RightText");
        rightTextObj.transform.SetParent(rightPageObj.transform);
        rightText = rightTextObj.AddComponent<TextMeshProUGUI>();
        rightText.color = textColor;
        rightText.fontSize = currentFontSize;
        rightText.alignment = TextAlignmentOptions.TopLeft;
        rightText.enableWordWrapping = true;
        rightText.overflowMode = TextOverflowModes.Overflow;
        
        RectTransform rightTextRT = rightText.GetComponent<RectTransform>();
        rightTextRT.anchorMin = Vector2.zero;
        rightTextRT.anchorMax = Vector2.one;
        rightTextRT.offsetMin = new Vector2(currentPadding, currentPadding * 1.5f);
        rightTextRT.offsetMax = new Vector2(-currentPadding, -currentPadding * 0.5f);
        
        GameObject pageNumObj = new GameObject("PageNumber");
        pageNumObj.transform.SetParent(bookObj.transform);
        pageNumberText = pageNumObj.AddComponent<TextMeshProUGUI>();
        pageNumberText.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        pageNumberText.fontSize = Mathf.Max(currentFontSize - 4, minFontSize);
        pageNumberText.alignment = TextAlignmentOptions.Center;
        pageNumberText.text = "Page 1";
        RectTransform pageNumRT = pageNumberText.GetComponent<RectTransform>();
        pageNumRT.anchorMin = new Vector2(0, 0);
        pageNumRT.anchorMax = new Vector2(1, 0);
        pageNumRT.pivot = new Vector2(0.5f, 0);
        pageNumRT.sizeDelta = new Vector2(currentBookWidth * 0.3f, currentBookHeight * 0.06f);
        pageNumRT.anchoredPosition = new Vector2(0, currentBookHeight * 0.02f);
        
        CreateNavigationButtons(bookContainer);
        CreateCloseButton(bookContainer);
    }
    
    void CreateNavigationButtons(GameObject container)
    {
        float buttonWidth = Mathf.Max(80f, currentBookWidth * 0.08f);
        float buttonHeight = Mathf.Max(100f, currentBookHeight * 0.15f);
        
        GameObject prevBtnObj = new GameObject("PrevPageButton");
        prevBtnObj.transform.SetParent(container.transform);
        prevPageButton = prevBtnObj.AddComponent<Button>();
        
        Image prevBg = prevBtnObj.AddComponent<Image>();
        prevBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        RectTransform prevRT = prevBtnObj.GetComponent<RectTransform>();
        prevRT.anchorMin = new Vector2(0, 0.5f);
        prevRT.anchorMax = new Vector2(0, 0.5f);
        prevRT.pivot = new Vector2(1, 0.5f);
        prevRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        prevRT.anchoredPosition = new Vector2(-buttonWidth * 0.2f, 0);
        
        GameObject prevArrow = new GameObject("PrevArrow");
        prevArrow.transform.SetParent(prevBtnObj.transform);
        prevButtonText = prevArrow.AddComponent<TextMeshProUGUI>();
        prevButtonText.text = "←\nPrevious (A)";
        prevButtonText.fontSize = Mathf.Max(currentFontSize - 2, minFontSize);
        prevButtonText.alignment = TextAlignmentOptions.Center;
        prevButtonText.color = Color.white;
        RectTransform prevArrowRT = prevButtonText.GetComponent<RectTransform>();
        prevArrowRT.anchorMin = Vector2.zero;
        prevArrowRT.anchorMax = Vector2.one;
        prevArrowRT.offsetMin = new Vector2(5, 5);
        prevArrowRT.offsetMax = new Vector2(-5, -5);
        
        prevPageButton.onClick.AddListener(PreviousPage);
        
        ColorBlock prevColors = prevPageButton.colors;
        prevColors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        prevColors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        prevColors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        prevPageButton.colors = prevColors;
        
        GameObject nextBtnObj = new GameObject("NextPageButton");
        nextBtnObj.transform.SetParent(container.transform);
        nextPageButton = nextBtnObj.AddComponent<Button>();
        
        Image nextBg = nextBtnObj.AddComponent<Image>();
        nextBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        RectTransform nextRT = nextBtnObj.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(1, 0.5f);
        nextRT.anchorMax = new Vector2(1, 0.5f);
        nextRT.pivot = new Vector2(0, 0.5f);
        nextRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        nextRT.anchoredPosition = new Vector2(buttonWidth * 0.2f, 0);
        
        GameObject nextArrow = new GameObject("NextArrow");
        nextArrow.transform.SetParent(nextBtnObj.transform);
        nextButtonText = nextArrow.AddComponent<TextMeshProUGUI>();
        nextButtonText.text = "→\nNext (D)";
        nextButtonText.fontSize = Mathf.Max(currentFontSize - 2, minFontSize);
        nextButtonText.alignment = TextAlignmentOptions.Center;
        nextButtonText.color = Color.white;
        RectTransform nextArrowRT = nextButtonText.GetComponent<RectTransform>();
        nextArrowRT.anchorMin = Vector2.zero;
        nextArrowRT.anchorMax = Vector2.one;
        nextArrowRT.offsetMin = new Vector2(5, 5);
        nextArrowRT.offsetMax = new Vector2(-5, -5);
        
        nextPageButton.onClick.AddListener(NextPage);
        
        ColorBlock nextColors = nextPageButton.colors;
        nextColors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        nextColors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        nextColors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        nextPageButton.colors = nextColors;
    }
    
    void CreateCloseButton(GameObject container)
    {
        float closeSize = Mathf.Max(60f, currentBookHeight * 0.08f);
        
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(container.transform);
        closeButton = closeBtnObj.AddComponent<Button>();
        
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        
        RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.sizeDelta = new Vector2(closeSize, closeSize);
        closeRT.anchoredPosition = new Vector2(-closeSize * 0.3f, -closeSize * 0.3f);
        
        GameObject closeX = new GameObject("X");
        closeX.transform.SetParent(closeBtnObj.transform);
        TextMeshProUGUI closeXText = closeX.AddComponent<TextMeshProUGUI>();
        closeXText.text = "X";
        closeXText.fontSize = Mathf.Max(currentFontSize - 4, minFontSize);
        closeXText.alignment = TextAlignmentOptions.Center;
        closeXText.color = Color.white;
        RectTransform closeXRT = closeXText.GetComponent<RectTransform>();
        closeXRT.anchorMin = Vector2.zero;
        closeXRT.anchorMax = Vector2.one;
        closeXRT.offsetMin = Vector2.zero;
        closeXRT.offsetMax = Vector2.zero;
        
        closeButton.onClick.AddListener(StopReading);
        
        ColorBlock closeColors = closeButton.colors;
        closeColors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        closeColors.highlightedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        closeColors.pressedColor = new Color(1f, 0.4f, 0.4f, 1f);
        closeButton.colors = closeColors;
    }
    
    void LoadBookContent()
    {
        string bookContent = GetBookContent();
        pages = SplitIntoPages(bookContent);
        currentPage = 0;
        UpdatePageDisplay();
    }
    
    string GetBookContent()
    {
        BookContent bookContentComponent = GetComponent<BookContent>();
        if (bookContentComponent != null && !string.IsNullOrEmpty(bookContentComponent.text))
        {
            return bookContentComponent.text;
        }
        
        if (!string.IsNullOrEmpty(fallbackBookContent))
        {
            return fallbackBookContent;
        }
        
        return $"The Journal of {gameObject.name}\n\nThis is a mysterious book you found. " +
               "Its pages are filled with handwritten notes and drawings. " +
               "The ink is faded in places, making some words difficult to read. " +
               "You sense there might be important clues hidden within these pages...";
    }
    
    string[] SplitIntoPages(string content)
    {
        if (string.IsNullOrEmpty(content))
            return new string[] { "This book appears to be blank..." };
        
        Debug.Log($"Original content length: {content.Length} characters");
        
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");
        
        List<string> pages = new List<string>();
        StringBuilder currentPage = new StringBuilder();
        string[] paragraphs = content.Split('\n');
        
        foreach (string paragraph in paragraphs)
        {
            string trimmedPara = paragraph.Trim();
            if (string.IsNullOrEmpty(trimmedPara))
            {
                if (currentPage.Length > 0 && !currentPage.ToString().EndsWith("\n\n"))
                {
                    currentPage.Append("\n\n");
                }
                continue;
            }
            
            // Split long paragraphs into lines
            if (trimmedPara.Length > maxCharsPerPage * 0.8f)
            {
                string[] longLines = SplitLongParagraph(trimmedPara, maxCharsPerPage);
                foreach (string line in longLines)
                {
                    ProcessLine(line, ref currentPage, pages);
                }
            }
            else
            {
                ProcessLine(trimmedPara, ref currentPage, pages);
            }
        }
        
        if (currentPage.Length > 0)
        {
            pages.Add(currentPage.ToString().Trim());
        }
        
        Debug.Log($"Split content into {pages.Count} pages");
        for (int i = 0; i < pages.Count; i++)
        {
            Debug.Log($"Page {i + 1}: {pages[i].Length} chars");
        }
        
        return pages.ToArray();
    }
    
    void ProcessLine(string line, ref StringBuilder currentPage, List<string> pages)
    {
        if (currentPage.Length + line.Length + 2 <= maxCharsPerPage)
        {
            if (currentPage.Length > 0)
            {
                currentPage.Append("\n");
            }
            currentPage.Append(line);
        }
        else
        {
            if (currentPage.Length > 0)
            {
                pages.Add(currentPage.ToString().Trim());
                currentPage.Clear();
            }
            
            if (line.Length > maxCharsPerPage)
            {
                string[] splitLines = SplitLongParagraph(line, maxCharsPerPage);
                for (int i = 0; i < splitLines.Length; i++)
                {
                    if (i > 0)
                    {
                        pages.Add(currentPage.ToString().Trim());
                        currentPage.Clear();
                    }
                    currentPage.Append(splitLines[i]);
                }
            }
            else
            {
                currentPage.Append(line);
            }
        }
    }
    
    string[] SplitLongParagraph(string paragraph, int maxLength)
    {
        List<string> lines = new List<string>();
        string[] words = paragraph.Split(' ');
        StringBuilder currentLine = new StringBuilder();
        
        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 <= maxLength)
            {
                if (currentLine.Length > 0)
                {
                    currentLine.Append(" ");
                }
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                
                if (word.Length > maxLength)
                {
                    // Split very long word
                    for (int i = 0; i < word.Length; i += maxLength - 3)
                    {
                        int length = Mathf.Min(maxLength - 3, word.Length - i);
                        string part = word.Substring(i, length);
                        if (i + length < word.Length)
                        {
                            part += "-";
                        }
                        lines.Add(part);
                    }
                }
                else
                {
                    currentLine.Append(word);
                }
            }
        }
        
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }
        
        return lines.ToArray();
    }
    
    void UpdatePageDisplay()
    {
        if (pages == null || pages.Length == 0) 
        {
            if (leftText != null) leftText.text = "No content";
            if (rightText != null) rightText.text = "";
            if (pageNumberText != null) pageNumberText.text = "Page 0 of 0";
            return;
        }
        
        int leftPageIndex = currentPage * 2;
        int rightPageIndex = leftPageIndex + 1;
        
        string leftContent = leftPageIndex < pages.Length ? pages[leftPageIndex] : "";
        string rightContent = rightPageIndex < pages.Length ? pages[rightPageIndex] : "";
        
        // Debug logging
        Debug.Log($"Displaying Page {currentPage + 1}:");
        Debug.Log($"Left page ({leftPageIndex + 1}): {leftContent.Length} chars");
        Debug.Log($"Right page ({rightPageIndex + 1}): {rightContent.Length} chars");
        
        if (leftText != null) leftText.text = leftContent;
        if (rightText != null) rightText.text = rightContent;
        
        int totalPages = Mathf.CeilToInt(pages.Length / 2f);
        int currentSpread = currentPage + 1;
        if (pageNumberText != null) pageNumberText.text = $"Page {currentSpread} of {totalPages}";
        
        bool hasPrevPage = currentPage > 0;
        bool hasNextPage = (currentPage * 2 + 2) < pages.Length;
        
        if (prevPageButton != null) prevPageButton.interactable = hasPrevPage;
        if (nextPageButton != null) nextPageButton.interactable = hasNextPage;
        
        if (prevButtonText != null)
        {
            prevButtonText.color = hasPrevPage ? Color.white : new Color(1, 1, 1, 0.5f);
            prevButtonText.text = hasPrevPage ? "←\nPrevious (A)" : "←\n(Start)";
        }
        
        if (nextButtonText != null)
        {
            nextButtonText.color = hasNextPage ? Color.white : new Color(1, 1, 1, 0.5f);
            nextButtonText.text = hasNextPage ? "→\nNext (D)" : "→\n(End)";
        }
        
        if (prevPageButton != null)
        {
            Image prevImage = prevPageButton.GetComponent<Image>();
            if (prevImage != null)
            {
                Color prevColor = prevImage.color;
                prevColor.a = hasPrevPage ? 0.9f : 0.5f;
                prevImage.color = prevColor;
            }
        }
        
        if (nextPageButton != null)
        {
            Image nextImage = nextPageButton.GetComponent<Image>();
            if (nextImage != null)
            {
                Color nextColor = nextImage.color;
                nextColor.a = hasNextPage ? 0.9f : 0.5f;
                nextImage.color = nextColor;
            }
        }
    }
    
    public void NextPage()
    {
        if (pages == null || (currentPage * 2 + 2) >= pages.Length) return;
        
        currentPage++;
        PlayPageTurnEffect(true);
        UpdatePageDisplay();
        PlaySound(pageTurnSoundId);
    }
    
    public void PreviousPage()
    {
        if (currentPage <= 0) return;
        
        currentPage--;
        PlayPageTurnEffect(false);
        UpdatePageDisplay();
        PlaySound(pageTurnSoundId);
    }
    
    void PlayPageTurnEffect(bool forward)
    {
        if (pageTurnParticles != null)
        {
            pageTurnParticles.Play();
        }
        
        if (pageTurnCoroutine != null) StopCoroutine(pageTurnCoroutine);
        pageTurnCoroutine = StartCoroutine(PageTurnAnimation(forward));
    }
    
    IEnumerator PageTurnAnimation(bool forward)
    {
        float duration = pageTurnSpeed / 2;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1, 0.3f, t / duration);
            if (leftText != null) leftText.alpha = alpha;
            if (rightText != null) rightText.alpha = alpha;
            yield return null;
        }
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0.3f, 1, t / duration);
            if (leftText != null) leftText.alpha = alpha;
            if (rightText != null) rightText.alpha = alpha;
            yield return null;
        }
        
        if (leftText != null) leftText.alpha = 1;
        if (rightText != null) rightText.alpha = 1;
        pageTurnCoroutine = null;
    }
    
    IEnumerator AnimateBookOpen()
    {
        float duration = openCloseSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            if (bookCanvasGroup != null) bookCanvasGroup.alpha = progress;
            
            if (bookUI != null && bookUI.transform.childCount > 0)
            {
                Transform background = bookUI.transform.GetChild(0);
                if (background.childCount > 0)
                {
                    Transform bookContainer = background.GetChild(0);
                    bookContainer.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
                }
            }
            
            yield return null;
        }
        
        if (bookCanvasGroup != null) bookCanvasGroup.alpha = 1;
        openCloseCoroutine = null;
    }
    
    IEnumerator AnimateBookClose()
    {
        float duration = openCloseSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 1 - progress;
            
            if (bookUI != null && bookUI.transform.childCount > 0)
            {
                Transform background = bookUI.transform.GetChild(0);
                if (background.childCount > 0)
                {
                    Transform bookContainer = background.GetChild(0);
                    bookContainer.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);
                }
            }
            
            yield return null;
        }
        
        if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0;
        if (bookUI != null) bookUI.SetActive(false);
        openCloseCoroutine = null;
    }
    
    void DisablePlayerControls(bool disable)
    {
        if (pickupController == null) return;
        
        pickupController.enabled = !disable;
        
        if (pickupController.player_movement_script != null)
            pickupController.player_movement_script.enabled = !disable;
        
        if (pickupController.player_camera_script != null)
            pickupController.player_camera_script.enabled = !disable;
        
        Cursor.lockState = disable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = disable;
    }
    
    IEnumerator ReenableControlsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisablePlayerControls(false);
    }
    
    void UpdateReadingPrompt(bool reading)
    {
        if (pickupController != null && pickupController.interaction_text != null)
        {
            if (reading)
            {
                pickupController.interaction_text.text = "Press [ESC] to Close | [←→] or [A/D] to Turn Pages";
            }
            else if (IsBeingHeld())
            {
                pickupController.interaction_text.text = "Press [X] to Read Book";
            }
        }
    }
    
    void PlaySound(string soundId)
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
    
    public void OnBookPickedUp()
    {
        isBeingHeld = true;
        UpdateReadingPrompt(false);
    }
    
    public void OnBookDropped()
    {
        isBeingHeld = false;
        if (isBeingRead) StopReading();
    }
    
    void OnDestroy()
    {
        if (bookUI != null) Destroy(bookUI);
        if (isBeingRead) DisablePlayerControls(false);
    }
}