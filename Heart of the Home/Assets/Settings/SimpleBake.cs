using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimpleBake : MonoBehaviour
{
    #if UNITY_EDITOR
    [ContextMenu("Quick Bake")]
    void QuickBake()
    {
        // Set everything up
        Lightmapping.bakedGI = true;
        Lightmapping.realtimeGI = false;
        
        // Mark static
        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.GetComponent<Renderer>())
                obj.isStatic = true;
        }
        
        // Bake
        Lightmapping.Bake();
    }
    #endif
}