using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppearOnTrigger : MonoBehaviour
{
    // Temporary class to make things appear because later im gonna add an actual rope
    public GameObject[] objectsToAppear; // Objects that appear when triggered

    private void Start()
    {
        foreach (var obj in objectsToAppear) // Initially hide all objects
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBody")) // If player detected
        {
            foreach (var obj in objectsToAppear) // Objects appear
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }
}