using UnityEngine;
using TMPro;

public class BookReader : MonoBehaviour
{
    [Header("References")]
    public PickupController pickup_controller;  
    public GameObject book_panel;               // UI panel that displays book content
    public TextMeshProUGUI book_text;           // Text UI element inside panel
    public KeyCode read_key = KeyCode.X;        // Key used to toggle reading (same as inspect)
    public string book_tag = "Book";            // Tag for book objects

    private bool is_reading = false;

    void Start()
    {
        if (book_panel != null)
            book_panel.SetActive(false);
    }

    void Update()
    {
        if (pickup_controller == null || book_panel == null)
            return;

        // Only allow reading while inspecting a held book
        if (pickup_controller.IsInspecting && IsHoldingBook())
        {
            if (Input.GetKeyDown(read_key))
            {
                // Toggle reading mode
                if (!is_reading)
                    OpenBook();
                else
                    CloseBook();
            }
        }

        // If you drop or stop inspecting, ensure UI closes
        if (is_reading && (!pickup_controller.IsInspecting || !IsHoldingBook()))
        {
            CloseBook();
        }
    }

    bool IsHoldingBook()
    {
       
        GameObject held_object = pickup_controller.HeldObject;
        return held_object != null && held_object.CompareTag(book_tag);
    }

    void OpenBook()
    {
        is_reading = true;

        if (book_panel != null)
            book_panel.SetActive(true);

        GameObject held_object = pickup_controller.HeldObject;
        if (held_object != null)
        {
            BookContent content = held_object.GetComponent<BookContent>();
            if (book_text != null)
                book_text.text = content != null ? content.text : "The pages are blank.";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseBook()
    {
        is_reading = false;

        if (book_panel != null)
            book_panel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
