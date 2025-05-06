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
        else if (mouse.leftButton.isPressed && isDragging)
        {
            UpdateDrag(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame && isDragging)
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
                    if (isDragging)
                    {
                        UpdateDrag(primaryTouch.position);
                    }
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                    if (isDragging)
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
        
        // Get the grid position
        Vector2Int gridPosition = GetGridPosition(worldPosition);
        
        // Get the block at this position
        Block block = board.GetBlockAtPosition(gridPosition);
        
        if (block != null)
        {
            // Double click/tap triggers rotation
            block.TryRotate(true);
        }
    }

    void StartDrag(Vector2 screenPos)
    {
        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Get the grid position
        Vector2Int gridPosition = GetGridPosition(worldPosition);
        
        // Try to find block at this position
        selectedBlock = board.GetBlockAtPosition(gridPosition);
        
        if (selectedBlock != null)
        {
            // Store original position for potential revert
            originalGridPosition = selectedBlock.PivotGridPosition;
            
            // Calculate offset between touch point and block pivot
            Vector3 pivotWorldPos = board.GetWorldPositionFromGridPosition(selectedBlock.PivotGridPosition);
            dragOffset = pivotWorldPos - worldPosition;
            dragOffset.z = 0;
            
            // Temporarily remove block from grid during dragging
            List<Vector2Int> occupiedPositions = selectedBlock.GetOccupiedGridPositions();
            foreach (Vector2Int pos in occupiedPositions)
            {
                if (board.IsValidGridPosition(pos) && board.GetBlockAtPosition(pos) == selectedBlock)
                {
                    board.RemoveBlock(pos);
                }
            }
            
            isDragging = true;
            Debug.Log($"Started dragging block at {gridPosition}");
        }
    }

    void UpdateDrag(Vector2 screenPos)
    {
        if (selectedBlock == null || !isDragging) return;

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Apply offset to get the pivot position
        Vector3 targetPivotPosition = worldPosition + dragOffset;
        
        // Update block's visual position during drag (free-form movement)
        selectedBlock.SetDragPosition(targetPivotPosition);
        
        // Check if this position would be valid
        Vector2Int nearestGridPos = GetGridPosition(targetPivotPosition);
        
        // Save original position
        Vector2Int originalPos = selectedBlock.PivotGridPosition;
        
        // Do a clean validation test
        // First ensure the block is completely removed from the board
        selectedBlock.RemoveFromBoard();
        
        // Test if the new position would be valid
        bool positionValid = false;
        
        // We need to temporarily set the position to test validity
        // But we use TryMove which handles all the logic internally
        if (selectedBlock.TryMove(nearestGridPos))
        {
            positionValid = true;
            // Move back to original position
            selectedBlock.TryMove(originalPos);
        }
        else
        {
            // If invalid, make sure we're back at the original position
            selectedBlock.TryMove(originalPos);
        }
        
        // Keep visual position at drag point
        selectedBlock.SetDragPosition(targetPivotPosition);
        
        // Debug the position validity
        Debug.Log($"Position at {nearestGridPos} is {(positionValid ? "valid" : "invalid")}");
        
        // Set visual feedback for invalid position
        if (!positionValid)
        {
            selectedBlock.SetInvalidPlacementVisual(true);
        }
        else
        {
            selectedBlock.SetInvalidPlacementVisual(false);
        }
    }

    void EndDrag(Vector2 screenPos)
    {
        if (selectedBlock == null || !isDragging) return;

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0;
        
        // Apply offset to get the pivot position
        Vector3 targetPivotPosition = worldPosition + dragOffset;
        
        // Snap to grid and check if position is valid
        bool success = selectedBlock.SnapToGrid(targetPivotPosition);
        
        // If snapping failed, revert to original position
        if (!success)
        {
            selectedBlock.TryMove(originalGridPosition);
        }
        
        // Reset visual effects
        selectedBlock.SetDragVisualState(false);
        selectedBlock.SetInvalidPlacementVisual(false);
        
        isDragging = false;
        selectedBlock = null;
        Debug.Log("Drag completed");
    }

    Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // Adjust these values based on your grid's actual setup
        float gridOriginX = board.transform.position.x;
        float gridOriginY = board.transform.position.y;
        
        // Adjust for grid cell size (assuming square cells)
        float cellSize = board.cellSize;

        // Calculate grid coordinates
        int x = Mathf.FloorToInt((worldPosition.x - gridOriginX) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOriginY) / cellSize);

        // Clamp to ensure within grid bounds
        x = Mathf.Clamp(x, 0, board.gridWidth - 1);
        y = Mathf.Clamp(y, 0, board.gridHeight - 1);

        return new Vector2Int(x, y);
    }
}