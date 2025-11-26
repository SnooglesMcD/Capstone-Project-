using UnityEngine;

public class floor_board_controller : MonoBehaviour
{
    public GameObject key_prefab;
    private bool opened = false;

    public void OnInteract()
    {
        if (opened) return;
        opened = true;

        key_prefab.gameObject.SetActive(true);
    }
}
