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
        // Try to find existing keypad in scene
        keypadInstance = FindObjectOfType<DynamicKeypad>();
        
        // If not found, create one
        if (keypadInstance == null)
        {
            // Check if we have a prefab to instantiate
            if (keypadPrefab != null)
            {
                keypadInstance = Instantiate(keypadPrefab);
                Debug.Log("Instantiated DynamicKeypad from prefab");
            }
            else
            {
                // Create a new GameObject with the component
                GameObject keypadObj = new GameObject("DynamicKeypad");
                keypadInstance = keypadObj.AddComponent<DynamicKeypad>();
                
                // Make it persistent or parent it appropriately
                DontDestroyOnLoad(keypadObj);
                
                Debug.Log("Created DynamicKeypad at runtime");
            }
        }
    }

    public void ShowKeypad()
    {
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
                // Fallback to console input
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
}