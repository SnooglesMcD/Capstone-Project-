using UnityEngine;

public class puzzle_manager : MonoBehaviour
{
    public static puzzle_manager instance;

    public pedestal_controller left;
    public pedestal_controller center;
    public pedestal_controller right;

    public Light prism_light;
    public GameObject heart_statue;
    public GameObject floor_board;

    private bool solved = false;

    void Awake()
    {
        instance = this;
    }

    public void Notify_pedestal_changed(pedestal_controller pedestal)
    {
        if (solved) return;

        if (left.is_correct && center.is_correct && right.is_correct)
        {
            solved = true;

            if (prism_light != null)
                prism_light.enabled = true;

            if (heart_statue != null)
            {
                var anim = heart_statue.GetComponent<Animator>();
                if (anim != null)
                    anim.SetTrigger("react");
            }

            floor_board.GetComponent<Collider>().enabled = true;
        }
    }
}
