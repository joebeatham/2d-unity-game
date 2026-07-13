using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR // Not in built games
    using UnityEditor;
#endif

public class RoomTrigger : MonoBehaviour
{
    public string SpawnPointName;   // Set this to the name of spawn point in target scene from a specific door
    #if UNITY_EDITOR
        public SceneAsset SceneAsset; // Allows drag and drop of scenes in inspector
    #endif
    [HideInInspector] public string TargetSceneName; // Holds the scene name as string (don't show in inspector)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBody")) // Only trigger when player touches the trigger
        {
            SpawnPointManager.RoomSpawnPoint = SpawnPointName; // Set the static variable in SpawnPointManager to the desired spawn point name
            SceneManager.LoadScene(TargetSceneName); // Load the target scene using the string name
        }
    }
    
    #if UNITY_EDITOR
        private void OnValidate()
            {
                if (SceneAsset != null) // If a scene is dropped in inspector then save name to TargetSceneName as string
                {
                    TargetSceneName = SceneAsset.name;
                }
            }
    #endif
}
