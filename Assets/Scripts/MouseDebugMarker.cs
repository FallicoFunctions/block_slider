using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseDebugMarker : MonoBehaviour
{
    void Update()
    {
        Vector3 mousePosition;
        
        #if ENABLE_INPUT_SYSTEM
        // New Input System
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
        else
        {
            return; // No mouse available
        }
        #else
        // Legacy Input System
        mousePosition = Input.mousePosition;
        #endif
        
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePosition);
        mouseWorldPos.z = 0; // Keep at z=0 for 2D
        transform.position = mouseWorldPos;
    }
}