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
        if (pickup_controller_is_inspecting() && IsHoldingBook())
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

        // nsure UI closes
        if (is_reading && (!pickup_controller_is_inspecting() || !IsHoldingBook()))
        {
            CloseBook();
        }
    }

    bool pickup_controller_is_inspecting()
    {
        // Access the is_inspecting flag from your PickupController
        return (bool)pickup_controller.GetType().GetField("is_inspecting", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(pickup_controller);
    }

    bool IsHoldingBook()
    {
        var held_field = pickup_controller.GetType().GetField("held_object", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        GameObject held_object = (GameObject)held_field?.GetValue(pickup_controller);

        return held_object != null && held_object.CompareTag(book_tag);
    }

    void OpenBook()
    {
        is_reading = true;

        if (book_panel != null)
            book_panel.SetActive(true);

        // Get current book and its text
        var held_field = pickup_controller.GetType().GetField("held_object",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        GameObject held_object = (GameObject)held_field?.GetValue(pickup_controller);

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
