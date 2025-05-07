using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputHandler : MonoBehaviour
{
    public Board board; // Reference to the Board script

    [SerializeField] 
    private Camera mainCamera; // Use SerializeField to make private field visible in Inspector

    // Currently selected block
    private Block selectedBlock = null;

    // Dragging variables
    private Vector3 dragOffset;
    private Vector2Int originalGridPosition;
    private bool isDragging = false;
    
    // Hit detection parameters - add this to make block selection more precise
    [SerializeField] private float hitRadius = 0.3f; // Reduced from default value
    
    // Rotation input
    private bool rotateRequested = false;

    // Input system references
    private Mouse mouse;
    private Touchscreen touchscreen;
    private Keyboard keyboard;

    // Drag parameters
    public float dragThreshold = 0.1f;
    
    // For double tap/click detection
    private float lastClickTime;
    private float doubleClickTime = 0.3f; // Time window for double click/tap
    private Vector2 lastClickPosition;
    private float doubleClickDistanceThreshold = 50f; // Distance threshold for double tap

    void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            
            // Log a warning if still no camera is found
            if (mainCamera == null)
            {
                Debug.LogError("No Main Camera found! Please assign a camera in the Inspector.");
            }
        }

        // Initialize input devices
        mouse = Mouse.current;
        touchscreen = Touchscreen.current;
        keyboard = Keyboard.current;
    }

    void Update()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
        #else
        HandleTouchInput();
        #endif
        
        // Handle rotation input
        HandleRotationInput();
    }

    void HandleMouseInput()
    {
        if (mouse == null) return;

        // Mouse input for editor and standalone
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Check for double click
            float clickTime = Time.time;
            if (clickTime - lastClickTime < doubleClickTime)
            {
                HandleDoubleClick(mouse.position.ReadValue());
            }
            else
            {
                StartDrag(mouse.position.ReadValue());
            }
            
            lastClickTime = clickTime;
            lastClickPosition = mouse.position.ReadValue();
        }
        else if (mouse.leftButton.isPressed && isDragging && selectedBlock != null)
        {
            UpdateDrag(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame && isDragging && selectedBlock != null)
        {
            EndDrag(mouse.position.ReadValue());
        }
    }

    void HandleTouchInput()
    {
        if (touchscreen == null) return;

        // Touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch primaryTouch = Input.GetTouch(0);

            switch (primaryTouch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    // Check for double tap
                    if (Time.time - lastClickTime < doubleClickTime && 
                        Vector2.Distance(primaryTouch.position, lastClickPosition) < doubleClickDistanceThreshold)
                    {
                        HandleDoubleClick(primaryTouch.position);
                    }
                    else
                    {
                        StartDrag(primaryTouch.position);
                    }
                    
                    lastClickTime = Time.time;
                    lastClickPosition = primaryTouch.position;
                    break;
                    
                case UnityEngine.TouchPhase.Moved:
                    if (isDragging && selectedBlock != null)
                    {
                        UpdateDrag(primaryTouch.position);
                    }
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                    if (isDragging && selectedBlock != null)
                    {
                        EndDrag(primaryTouch.position);
                    }
                    break;
            }
            
            // Handle multi-touch rotation
            if (Input.touchCount >= 2)
            {
                HandleTouchRotation();
            }
        }
    }
    
    void HandleRotationInput()
    {
        // Keyboard rotation (for editor/standalone)
        if (keyboard != null && selectedBlock != null)
        {
            // Rotate clockwise with R key
            if (keyboard.rKey.wasPressedThisFrame)
            {
                selectedBlock.TryRotate(true);
            }
            // Rotate counter-clockwise with E key
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                selectedBlock.TryRotate(false);
            }
        }
    }
    
    void HandleTouchRotation()
    {
        // Implement touch-based rotation (two-finger rotate gesture)
        // This is a simplified version; a more sophisticated gesture system might be needed
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);
        
        // Simple implementation: if second touch just began, rotate the block
        if (touch2.phase == UnityEngine.TouchPhase.Began && selectedBlock != null)
        {
            selectedBlock.TryRotate(true);
        }
    }
    
    void HandleDoubleClick(Vector2 screenPos)
    {
        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Try to find a block at the exact clicked position
        Block block = FindBlockAtExactPosition(worldPosition);
        
        if (block != null)
        {
            // Double click/tap triggers rotation
            block.TryRotate(true);
        }
    }
    
    // Find a block at the exact position - only returns a block if directly clicked on it
    private Block FindBlockAtExactPosition(Vector3 worldPos)
    {
        // Convert world position to grid position
        Vector2Int gridPos = GetGridPosition(worldPos);
        
        // Check if there's a block at this exact position
        Block block = board.GetBlockAtPosition(gridPos);
        if (block != null)
        {
            return block;
        }
        
        return null;
    }

    void StartDrag(Vector2 screenPos)
    {
        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Get the grid position
        Vector2Int gridPosition = GetGridPosition(worldPosition);
        Debug.Log($"StartDrag at grid position: {gridPosition}");
        
        // Try to find block at this position
        selectedBlock = board.GetBlockAtPosition(gridPosition);
        
        if (selectedBlock != null)
        {
            // Store original position for potential revert
            originalGridPosition = selectedBlock.PivotGridPosition;
            
            // Calculate the exact offset between the click point and the pivot
            Vector3 pivotWorldPos = board.GetWorldPositionFromGridPosition(selectedBlock.PivotGridPosition);
            dragOffset = worldPosition - pivotWorldPos;
            
            // Store the click position (not the drag offset) in the block
            selectedBlock.DragOffset = dragOffset;
            
            // Temporarily remove block from grid during dragging
            selectedBlock.RemoveFromBoard();
            
            // Set visual feedback for dragging
            selectedBlock.SetDragVisualState(true);
            
            isDragging = true;
            Debug.Log($"Started dragging block at {selectedBlock.PivotGridPosition}, click offset: {dragOffset}");
        }
        else
        {
            Debug.Log($"No block found at grid position {gridPosition}");
        }
    }
    void UpdateDrag(Vector2 screenPos)
    {
        if (selectedBlock == null || !isDragging) return;

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Apply offset to get the desired pivot position
        Vector3 targetPivotPosition = worldPosition + dragOffset;
        
        // Update block's visual position for smooth dragging
        selectedBlock.SetDragPosition(targetPivotPosition);
        
        // Calculate grid position for preview snapping
        Vector2Int targetGridPos = GetGridPosition(targetPivotPosition);
        
        // Only attempt to move the block if we're in a different grid cell
        if (targetGridPos != selectedBlock.PivotGridPosition)
        {
            // Test if this would be a valid position
            Vector2Int originalPos = selectedBlock.PivotGridPosition;
            
            // Try the new position
            bool canMove = selectedBlock.TryMove(targetGridPos);
            
            // Visual feedback for invalid placement
            selectedBlock.SetInvalidPlacementVisual(!canMove);
            
            // If move failed, revert position but keep visual at drag position
            if (!canMove)
            {
                selectedBlock.TryMove(originalPos);
            }
        }
    }

    void EndDrag(Vector2 screenPos)
    {
        if (selectedBlock == null || !isDragging) return;

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Apply offset to get the final pivot position
        Vector3 finalPosition = worldPosition + dragOffset;
        Debug.Log($"EndDrag at position: {finalPosition}, attempting to snap");
        
        // Snap to grid
        bool success = selectedBlock.SnapToGrid(finalPosition);
        
        // If snapping failed, revert to original position
        if (!success)
        {
            Debug.Log($"Snapping failed, reverting to original position: {originalGridPosition}");
            selectedBlock.TryMove(originalGridPosition);
        }
        
        // Reset visual effects
        selectedBlock.SetDragVisualState(false);
        selectedBlock.SetInvalidPlacementVisual(false);
        
        // Reset dragging state
        isDragging = false;
        selectedBlock = null;
        
        Debug.Log("Drag completed");
    }

    Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // Adjust for grid origin
        float gridOriginX = board.transform.position.x;
        float gridOriginY = board.transform.position.y;
        
        // Get cell size
        float cellSize = board.cellSize;

        // Calculate relative position from grid origin
        float relativeX = worldPosition.x - gridOriginX;
        float relativeY = worldPosition.y - gridOriginY;
        
        // Calculate grid coordinates with rounding
        int gridX = Mathf.RoundToInt(relativeX / cellSize);
        int gridY = Mathf.RoundToInt(relativeY / cellSize);
        
        // Clamp to valid grid range
        gridX = Mathf.Clamp(gridX, 0, board.gridWidth - 1);
        gridY = Mathf.Clamp(gridY, 0, board.gridHeight - 1);
        
        return new Vector2Int(gridX, gridY);
    }
}