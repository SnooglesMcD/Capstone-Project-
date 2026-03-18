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
        
        // Start with prism light disabled
        if (prism_light != null)
        {
            prism_light.gameObject.SetActive(false);
            prism_light.enabled = false;
        }
        
        // Start with floorboard disabled (optional)
        if (floor_board != null)
        {
            Collider floorCol = floor_board.GetComponent<Collider>();
            if (floorCol != null) floorCol.enabled = false;
        }
    }

    public void Notify_pedestal_changed(pedestal_controller pedestal)
    {
        if (solved) return;

        if (left.is_correct && center.is_correct && right.is_correct)
        {
            solved = true;
            Debug.Log("PUZZLE SOLVED! Activating prism light...");

            // Activate prism light
            if (prism_light != null)
            {
                prism_light.enabled = true;
                prism_light.gameObject.SetActive(true);
                
                
                Debug.Log("Prism light activated");
            }

            // Enable floorboard for interaction
            if (floor_board != null)
            {
                Collider floorCol = floor_board.GetComponent<Collider>();
                if (floorCol != null)
                {
                    floorCol.enabled = true;
                    Debug.Log("Floorboard enabled for interaction");
                }
                
                // Trigger floorboard reveal animation/effect
                floor_board_controller fbc = floor_board.GetComponent<floor_board_controller>();
                if (fbc != null)
                {
                    //anims
                }
            }
            
            // Activate heart statue
            if (heart_statue != null)
            {
                heart_statue.SetActive(true);
            }
            
            // Play celebration effects
            PlayVictoryEffects();
        }
    }
    
     void PlayVictoryEffects()
    {
        // Play sound
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null && audio.clip != null)
        {
            audio.Play();
        }
        
        // Particle effect
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        
        Debug.Log("Puzzle victory effects played!");
    }
    
    // Helper method for floorboard to check puzzle state
    public bool IsPuzzleSolved()
    {
        return solved;
    }
}