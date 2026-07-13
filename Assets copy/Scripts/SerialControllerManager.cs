using System.Collections;
using UnityEngine;
using System;

public class SerialControllerManager : MonoBehaviour
{
    [Header("Controller Status")]
    public string connectionStatus = "Arduino HID Mode";
    public string lastReceivedData = "Ready";
    
    // Input values
    public float horizontalInput = 0f;
    public bool jumpButton = false;
    public bool interactButton = false;
    public bool attackButton = false;  // Added attack button
    
    // Connection status (for compatibility with other scripts)
    public bool isConnected = true;  // Always true for HID mode
    
    // Events removed - were unused and causing compiler warnings
    
    void Start()
    {
        isConnected = true; // Always connected in HID mode
        StartCoroutine(InputLoop());
    }
    
    IEnumerator InputLoop()
    {
        while (true)
        {
            HandleArduinoHIDInput();
            yield return new WaitForSeconds(0.02f); // 50Hz update rate
        }
    }
    
    void HandleArduinoHIDInput()
    {
        // Read Arduino HID inputs and convert to game inputs
        // Arduino sends keyboard codes that we translate to game actions
        
        // Movement input
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
        else horizontalInput = 0f;
        
        // Button inputs
        jumpButton = Input.GetKey(KeyCode.W);
        interactButton = Input.GetKey(KeyCode.Space);
        attackButton = Input.GetKey(KeyCode.E);
    }
    
    [ContextMenu("Test HID Controls")]
    public void TestHIDControls()
    {
        // Arduino button mapping:
        // Jump Button → W key
        // Left Button → A key  
        // Right Button → D key
        // Interact Button → Space key
        // Attack Button → E key
    }
}