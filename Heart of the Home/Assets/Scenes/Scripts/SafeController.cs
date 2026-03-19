// Scripts/Puzzle/SafeController.cs
using UnityEngine;

public class SafeController : MonoBehaviour
{
    [Header("Settings")]
    public int correctCode = 1407;
    public AudioClip unlockSound;
    public AudioClip lockSound;
    public AudioClip errorSound;
    public AudioClip buttonSound;
    
    [Header("Models")]
    public GameObject closedSafeModel;  // Assign your closed safe model
    public GameObject openSafeModel;    // Assign your open safe model
    
    [Header("Visuals")]
    public Material lockedMat;
    public Material unlockedMat;
    public Light statusLight;
    public GameObject encodedNote;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onUnlocked;
    public UnityEngine.Events.UnityEvent onLocked;

    [Header("Keypad Settings")]
    public DynamicKeypad keypadPrefab;
    private DynamicKeypad keypadInstance;
    
    private bool isUnlocked = false;
    private string currentCode = "";
    private Renderer safeRenderer;
    private OfficePuzzleManager puzzleManager;
    
    void Start()
    {
        safeRenderer = GetComponent<Renderer>();
        puzzleManager = FindObjectOfType<OfficePuzzleManager>();
        
        // Make sure only the correct model is active at start
        SetModelState(false); // false = closed
        
        SetLockedState();
        
        if (encodedNote != null)
        {
            encodedNote.SetActive(false);
        }

        // Create or find keypad
        InitializeKeypad();
    }

    void InitializeKeypad()
    {
        keypadInstance = FindObjectOfType<DynamicKeypad>();
        
        if (keypadInstance == null)
        {
            if (keypadPrefab != null)
            {
                keypadInstance = Instantiate(keypadPrefab);
                Debug.Log("Instantiated DynamicKeypad from prefab");
            }
            else
            {
                GameObject keypadObj = new GameObject("DynamicKeypad");
                keypadInstance = keypadObj.AddComponent<DynamicKeypad>();
                DontDestroyOnLoad(keypadObj);
                Debug.Log("Created DynamicKeypad at runtime");
            }
        }
    }

    void SetModelState(bool open)
    {
        if (closedSafeModel != null)
            closedSafeModel.SetActive(!open);
        
        if (openSafeModel != null)
            openSafeModel.SetActive(open);
        
        Debug.Log($"Safe model set to: {(open ? "OPEN" : "CLOSED")}");
    }

    public void ShowKeypad()
    {
        if (isUnlocked)
        {
            Debug.Log("Safe is already unlocked");
            return;
        }
        
        if (keypadInstance != null)
        {
            keypadInstance.ShowKeypad(this);
            Debug.Log("Showing keypad for safe");
        }
        else
        {
            Debug.LogWarning("Keypad not initialized - recreating...");
            InitializeKeypad();
            
            if (keypadInstance != null)
            {
                keypadInstance.ShowKeypad(this);
            }
            else
            {
                Debug.LogError("Failed to create keypad!");
                Debug.Log("Enter safe code using number keys (1407)");
            }
        }
    }

    
    public void AddDigit(int digit)
    {
        if (isUnlocked) return;
        
        if (currentCode.Length < 4)
        {
            currentCode += digit.ToString();
            PlaySound(buttonSound);
            Debug.Log($"Safe code: {currentCode}");
            
            if (currentCode.Length == 4)
            {
                CheckCode();
            }
        }
    }
    
    public void ClearCode()
    {
        if (isUnlocked) return;
        
        currentCode = "";
        PlaySound(buttonSound);
        Debug.Log("Safe code cleared");
    }
    
    void CheckCode()
    {
        if (int.TryParse(currentCode, out int enteredCode))
        {
            if (enteredCode == correctCode)
            {
                UnlockSafe();
            }
            else
            {
                WrongCode();
            }
        }
        else
        {
            WrongCode();
        }
    }
    
    public void UnlockSafe()
    {
        isUnlocked = true;
        Debug.Log($"SAFE UNLOCKED! Code: {currentCode}");
        
        // SWAP MODELS - Show open safe
        SetModelState(true); // true = open
        
        // Visual feedback
        if (safeRenderer != null && unlockedMat != null)
        {
            safeRenderer.material = unlockedMat;
        }
        
        if (statusLight != null)
        {
            statusLight.color = Color.green;
        }
        
        // Play sound
        PlaySound(unlockSound);
        
        // Reveal encoded note
        if (encodedNote != null)
        {
            encodedNote.SetActive(true);
            Debug.Log("Encoded note revealed in safe");
        }
        
        // Notify puzzle manager
        if (puzzleManager != null)
        {
            puzzleManager.OnSafeOpened(correctCode);
        }
        
        onUnlocked?.Invoke();
        
        // Clear code for next time
        currentCode = "";

        // Close keypad if it's open
        if (keypadInstance != null && keypadInstance.IsActive())
        {
            keypadInstance.CloseKeypad();
        }
    }
    
    void WrongCode()
    {
        Debug.Log($"WRONG CODE: {currentCode}");
        PlaySound(errorSound);
        currentCode = "";
    }
    
    
    void SetLockedState()
    {
        if (safeRenderer != null && lockedMat != null)
        {
            safeRenderer.material = lockedMat;
        }
        
        if (statusLight != null)
        {
            statusLight.color = Color.red;
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.5f);
        }
    }
    
    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    // Called when player looks away or closes keypad
    public void OnKeypadClosed()
    {
        Debug.Log("Keypad closed for safe");
        currentCode = ""; // Clear any partial code
    }

    // Optional: Add animation effect when opening
    public void PlayOpenAnimation()
    {
        // If your models have animators, you could trigger animations here
        // Or add a simple scale effect:
        if (openSafeModel != null)
        {
            StartCoroutine(AnimateOpen());
        }
    }

    System.Collections.IEnumerator AnimateOpen()
    {
        Vector3 originalScale = openSafeModel.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        // Quick pop effect
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            openSafeModel.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            openSafeModel.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        openSafeModel.transform.localScale = originalScale;
    }
}