using UnityEngine;

public class floor_board_controller : MonoBehaviour
{
    public GameObject key_prefab;
    private bool opened = false;

    public void OnInteract()
    {
        if (opened) return;
        opened = true;

        Instantiate(key_prefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);

        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("open");
    }
}
