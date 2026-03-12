using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject player;
    public Transform defaultSpawn;
    public bool forceRotation = true;
    
    void Start()
    {
        Debug.Log($"=== PLAYER SPAWNER STARTED in {SceneManager.GetActiveScene().name} ===");
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");
        }
        
        if (player == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }
        
        // Get saved door name and destination
        string spawnDoorName = PlayerPrefs.GetString("SpawnDoorName", "");
        string destinationScene = PlayerPrefs.GetString("DestinationScene", "");
        
        Debug.Log($"Saved door name: '{spawnDoorName}'");
        Debug.Log($"Saved destination: '{destinationScene}'");
        Debug.Log($"Current scene: '{SceneManager.GetActiveScene().name}'");
        
        // Only spawn if we're in the correct destination scene
        if (!string.IsNullOrEmpty(spawnDoorName) && 
            destinationScene == SceneManager.GetActiveScene().name)
        {
            // Look for spawn point named [DoorName]_Spawn
            string spawnPointName = spawnDoorName + "_Spawn";
            Debug.Log($"Looking for spawn point: '{spawnPointName}'");
            
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            
            if (spawnPoint != null)
            {
                // Log spawn point rotation
                Debug.Log($"Spawn point rotation: {spawnPoint.transform.rotation.eulerAngles}");
                
                // Set position
                player.transform.position = spawnPoint.transform.position;
                
                // ===== FIX: Force upright rotation =====
                // Only use the Y rotation from spawn point, keep X and Z at 0
                Vector3 spawnEuler = spawnPoint.transform.rotation.eulerAngles;
                player.transform.rotation = Quaternion.Euler(0, spawnEuler.y, 0);
                Debug.Log($"✅ Player spawned upright with rotation: (0, {spawnEuler.y}, 0)");
            }
            else
            {
                Debug.LogWarning($"⚠️ Spawn point '{spawnPointName}' not found!");
                
                // Try to find the door itself as fallback
                GameObject door = GameObject.Find(spawnDoorName);
                if (door != null)
                {
                    Debug.Log($"Door found: {door.name}, rotation: {door.transform.rotation.eulerAngles}");
                    
                    // Spawn in front of the door
                    Vector3 inFront = door.transform.position + door.transform.forward * 2;
                    player.transform.position = inFront;
                    
                    // ===== FIX: Face into room, stay upright =====
                    // Get door's forward direction but keep player upright
                    Vector3 doorForward = door.transform.forward;
                    doorForward.y = 0; // Flatten to prevent tipping
                    player.transform.rotation = Quaternion.LookRotation(-doorForward);
                    
                    Debug.Log($"⚠️ Spawned in front of door, facing: {player.transform.rotation.eulerAngles}");
                }
            }
        }
        else if (defaultSpawn != null)
        {
            Debug.Log($"Default spawn rotation: {defaultSpawn.rotation.eulerAngles}");
            
            player.transform.position = defaultSpawn.position;
            
            // ===== FIX: Force upright rotation for default spawn too =====
            Vector3 defaultEuler = defaultSpawn.rotation.eulerAngles;
            player.transform.rotation = Quaternion.Euler(0, defaultEuler.y, 0);
            
            Debug.Log($"Using default spawn, player rotation: {player.transform.rotation.eulerAngles}");
        }
        
        // Force update camera if your player has one
        Camera playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.identity; // Reset camera to neutral
        }
        
        // Clear the saved data
        PlayerPrefs.DeleteKey("SpawnDoorName");
        PlayerPrefs.DeleteKey("DestinationScene");
    }
    
    void OnDrawGizmos()
    {
        // Draw all spawn points in the scene
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("_Spawn"))
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(obj.transform.position, 0.3f);
                
                // Draw direction arrow
                Gizmos.color = Color.cyan;
                Vector3 direction = obj.transform.forward * 1;
                Gizmos.DrawRay(obj.transform.position, direction);
            }
        }
    }
}