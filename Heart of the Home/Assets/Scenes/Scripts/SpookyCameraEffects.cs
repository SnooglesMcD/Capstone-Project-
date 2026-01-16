
using UnityEngine;

public class SpookyCameraEffects : MonoBehaviour
{
    [Header("Breathing Effect")]
    public float breathIntensity = 0.05f;
    public float breathSpeed = 0.5f;
    
    [Header("Dark Adaptation")]
    public float minExposure = 0.8f;
    public float maxExposure = 1.2f;
    public float adaptationSpeed = 0.1f;
    
    private Vector3 originalPosition;
    private float breathOffset;
    private float currentExposure;
    
    void Start()
    {
        originalPosition = transform.localPosition;
        breathOffset = Random.Range(0f, 100f);
        currentExposure = minExposure;
    }
    
    void Update()
    {
        // Subtle camera breathing
        float breath = Mathf.PerlinNoise(Time.time * breathSpeed, breathOffset);
        Vector3 breathOffsetVec = new Vector3(
            (breath - 0.5f) * breathIntensity,
            (Mathf.PerlinNoise(breathOffset, Time.time * breathSpeed) - 0.5f) * breathIntensity * 0.5f,
            0
        );
        transform.localPosition = originalPosition + breathOffsetVec;
        
        // Simulate dark adaptation
        float targetExposure = IsLookingAtDarkArea() ? maxExposure : minExposure;
        currentExposure = Mathf.Lerp(currentExposure, targetExposure, adaptationSpeed * Time.deltaTime);
        
        
    }
    
    bool IsLookingAtDarkArea()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
        {
            // Check if hit area is dark (by tag, layer, or renderer darkness)
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                // Simple check based on material color brightness
                Color matColor = rend.material.color;
                float brightness = (matColor.r + matColor.g + matColor.b) / 3f;
                return brightness < 0.2f;
            }
        }
        return false;
    }
}