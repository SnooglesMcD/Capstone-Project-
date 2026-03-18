using UnityEngine;

public class FootstepController : MonoBehaviour
{
    public AudioSource stepAudio;
    public float stepDelay = 0.5f;

    float timer;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        if (isMoving)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                stepAudio.Play();
                timer = stepDelay;
            }
        }
        else
        {
            timer = 0f;
        }
    }
}