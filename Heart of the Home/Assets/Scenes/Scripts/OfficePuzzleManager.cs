// Scripts/OfficePuzzleManager.cs
using UnityEngine;

public class OfficePuzzleManager : MonoBehaviour
{
    public void OnSafeOpened(int code)
    {
        Debug.Log($"Safe opened with code: {code}");
    }
    
    public void OnCalendarDateSelected(int date)
    {
        Debug.Log($"Calendar date selected: {date}");
    }
    
    public void OnCryptographyBookRead()
    {
        Debug.Log("Cryptography book read");
    }
    
    public void OnSilasClueDiscovered(string clueType)
    {
        Debug.Log($"Silas clue discovered: {clueType}");
    }
    
    public void OnSafeRelocked()
    {
        Debug.Log("Safe relocked");
    }
}