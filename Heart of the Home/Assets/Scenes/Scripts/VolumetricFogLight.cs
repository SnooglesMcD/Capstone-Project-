using UnityEngine;

[RequireComponent(typeof(Light))]
public class VolumetricFogLight : MonoBehaviour
{
    public GameObject volumetricPrefab; // Assign a particle system or custom shader volume
    public float fogDensity = 0.1f;
    public Color fogColor = new Color(0.8f, 0.7f, 0.6f, 0.1f);
    
    private Material fogMaterial;
    private Light linkedLight;
    
    void Start()
    {
        linkedLight = GetComponent<Light>();
        
        // Create volumetric effect
        GameObject fogVolume = new GameObject("VolumetricFog_" + gameObject.name);
        fogVolume.transform.SetParent(transform);
        fogVolume.transform.localPosition = Vector3.zero;
        
        // Add particle system for simple volumetric effect
        ParticleSystem ps = fogVolume.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = fogColor;
        main.startSize = 0.1f;
        main.startLifetime = 5f;
        main.maxParticles = 1000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = ps.emission;
        emission.rateOverTime = 50f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = linkedLight.range * 0.5f;
        
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
    }
}