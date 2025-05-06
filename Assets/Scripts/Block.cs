using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    // Enum to define block colors
    public enum BlockColor
    {
        Red,
        Green,
        Blue,
        Yellow
    }

    // The color of this specific block
    public BlockColor color;

    // Reference to the board for movement validation
    private Board board;
    
    // Reference to the block's visual representation
    [SerializeField] private GameObject blockVisual;
    
    // The pivot grid position of the block (used as reference for the entire shape)
    public Vector2Int PivotGridPosition { get; private set; }
    
    // Collection of cells that make up this block's shape (in LOCAL coordinates relative to pivot)
    // For example, a 2x1 horizontal block might have cells at (0,0) and (1,0)
    [SerializeField] private List<Vector2Int> shape = new List<Vector2Int>();
    
    // Current rotation of the block (0, 90, 180, 270 degrees)
    private int rotationDegrees = 0;
    
    // Visual components
    private List<SpriteRenderer> cellRenderers = new List<SpriteRenderer>();

    // For drag and snap behavior
    private Vector3 dragStartPosition;
    private Vector2Int dragStartGridPosition;

    void Awake()
    {
        // If no shape defined, default to a 1x1 block
        if (shape.Count == 0)
        {
            shape.Add(new Vector2Int(0, 0));
        }
    }

    void Start()
    {
        // Create visual representation based on shape
        GenerateVisuals();
    }
    
    // Generate the visual representation of the block based on its shape
    private void GenerateVisuals()
    {
        // Clear any existing visual components
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cellRenderers.Clear();
        
        // Get the sprite renderer on this object to use its properties
        SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
        
        // Create a visual element for each cell in the shape
        foreach (Vector2Int cell in shape)
        {
            GameObject cellObj = new GameObject($"Cell_{cell.x}_{cell.y}");
            cellObj.transform.SetParent(transform);
            
            // Position the cell relative to the pivot
            cellObj.transform.localPosition = new Vector3(cell.x * board.cellSize, cell.y * board.cellSize, 0);
            
            // Add sprite renderer
            SpriteRenderer renderer = cellObj.AddComponent<SpriteRenderer>();
            
            // Copy sprite and color from the main renderer
            if (mainRenderer != null)
            {
                renderer.sprite = mainRenderer.sprite;
                renderer.color = mainRenderer.color;
            }
            
            cellRenderers.Add(renderer);
        }
        
        // Disable the main renderer since we're using child renderers
        if (mainRenderer != null)
        {
            mainRenderer.enabled = false;
        }
    }

    // Initialize the block with a board reference and starting position
    public void Initialize(Board boardReference, Vector2Int startPosition, List<Vector2Int> blockShape = null)
    {
        board = boardReference;
        
        // If a shape is provided, use it
        if (blockShape != null && blockShape.Count > 0)
        {
            shape = new List<Vector2Int>(blockShape);
        }
        
        // Set the initial position
        PivotGridPosition = startPosition;
        
        // Generate the visuals now that we have all the properties set
        GenerateVisuals();
        
        // Update position on the board
        UpdateBoardPosition();
    }
    
    // Update the position of all cells on the board
    private void UpdateBoardPosition()
    {
        // First, remove all cells from their previous positions
        RemoveFromBoard();
        
        // Then place all cells at their new positions
        PlaceOnBoard();
        
        // Update visual position
        UpdateVisualPosition();
    }
    
    // Remove all cells from the board
    public void RemoveFromBoard()
    {
        // Get all world positions this block occupies
        List<Vector2Int> occupiedPositions = GetOccupiedGridPositions();
        
        // Remove each cell from the board
        foreach (Vector2Int pos in occupiedPositions)
        {
            if (board.IsValidGridPosition(pos) && board.GetBlockAtPosition(pos) == this)
            {
                board.RemoveBlock(pos);
            }
        }
    }
    
    // Place all cells on the board
    private void PlaceOnBoard()
    {
        // Get all world positions this block will occupy
        List<Vector2Int> occupiedPositions = GetOccupiedGridPositions();
        
        // Place each cell on the board
        foreach (Vector2Int pos in occupiedPositions)
        {
            if (board.IsValidGridPosition(pos))
            {
                board.PlaceBlock(this, pos);
            }
            else
            {
                Debug.LogWarning($"Attempted to place block at invalid position: {pos}");
            }
        }
    }
    
    // Update the visual position of the block based on the pivot grid position
    private void UpdateVisualPosition()
    {
        Vector3 worldPosition = board.GetWorldPositionFromGridPosition(PivotGridPosition);
        transform.position = worldPosition;
    }

    // Get all grid positions occupied by this block
    public List<Vector2Int> GetOccupiedGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        foreach (Vector2Int relativePos in shape)
        {
            // Apply rotation to the relative position
            Vector2Int rotatedPos = RotatePosition(relativePos, rotationDegrees);
            
            // Calculate world grid position
            Vector2Int worldPos = PivotGridPosition + rotatedPos;
            positions.Add(worldPos);
        }
        
        return positions;
    }
    
    // Apply rotation to a relative position
    private Vector2Int RotatePosition(Vector2Int pos, int degrees)
    {
        // Normalize degrees to 0, 90, 180, or 270
        int normalizedDegrees = ((degrees % 360) + 360) % 360;
        
        switch (normalizedDegrees)
        {
            case 0: // No rotation
                return pos;
            case 90: // 90 degrees clockwise
                return new Vector2Int(-pos.y, pos.x);
            case 180: // 180 degrees
                return new Vector2Int(-pos.x, -pos.y);
            case 270: // 270 degrees clockwise (90 degrees counter-clockwise)
                return new Vector2Int(pos.y, -pos.x);
            default:
                return pos;
        }
    }
    
    // Start dragging this block
    public void StartDrag(Vector3 worldPosition)
    {
        dragStartPosition = transform.position;
        dragStartGridPosition = PivotGridPosition;
        RemoveFromBoard();
    }
    
    // Try to move the block to a new grid position
    public bool TryMove(Vector2Int newPivotPosition)
    {
        // Store original position in case we need to revert
        Vector2Int originalPosition = PivotGridPosition;
        
        // Temporarily remove this block from the board
        RemoveFromBoard();
        
        // Update the pivot position
        PivotGridPosition = newPivotPosition;
        
        // Check if the new position is valid
        if (!IsValidPosition())
        {
            // If not valid, revert to original position
            PivotGridPosition = originalPosition;
            PlaceOnBoard();
            return false;
        }
        
        // If valid, update the board and visual position
        PlaceOnBoard();
        UpdateVisualPosition();
        
        return true;
    }
    
    // Check if the current position is valid (all cells are on valid, empty grid positions)
    public bool IsValidPosition()
    {
        List<Vector2Int> positions = GetOccupiedGridPositions();
        
        foreach (Vector2Int pos in positions)
        {
            // Check if position is on the grid
            if (!board.IsValidGridPosition(pos))
            {
                return false;
            }
            
            // Check if position is empty or occupied by this block
            Block blockAtPosition = board.GetBlockAtPosition(pos);
            if (blockAtPosition != null && blockAtPosition != this)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Try to rotate the block
    public bool TryRotate(bool clockwise = true)
    {
        // Store original rotation in case we need to revert
        int originalRotation = rotationDegrees;
        
        // Temporarily remove this block from the board
        RemoveFromBoard();
        
        // Update rotation
        rotationDegrees += clockwise ? 90 : -90;
        
        // Normalize rotation to 0-359
        rotationDegrees = ((rotationDegrees % 360) + 360) % 360;
        
        // Check if the new rotation is valid
        if (!IsValidPosition())
        {
            // If not valid, revert to original rotation
            rotationDegrees = originalRotation;
            PlaceOnBoard();
            return false;
        }
        
        // If valid, update the board position and apply visual rotation
        PlaceOnBoard();
        transform.rotation = Quaternion.Euler(0, 0, rotationDegrees);
        
        return true;
    }
    
    // Method to allow free movement during dragging
    public void SetDragPosition(Vector3 worldPosition)
    {
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }
    
    // Snap to the nearest valid grid position after dragging
    public bool SnapToGrid(Vector3 worldPosition)
    {
        // Calculate the grid position for the pivot based on the current visual position
        Vector2Int targetGridPos = WorldToGridPosition(worldPosition);
        Debug.Log($"Current visual position: {worldPosition}, mapped to grid: {targetGridPos}");
        
        // Try the target position first (where the player released the block)
        if (TryPositionAt(targetGridPos))
        {
            Debug.Log($"Successfully snapped to target grid position: {targetGridPos}");
            return true;
        }
        
        // If the target position doesn't work, try adjacent positions in a specific order
        // First prioritize horizontal movement (left/right), then vertical (up/down)
        Vector2Int[] prioritizedOffsets = new Vector2Int[]
        {
            // First try original position
            Vector2Int.zero,
            
            // Then try horizontal offsets (left/right)
            new Vector2Int(1, 0),    // right
            new Vector2Int(-1, 0),   // left
            
            // Then try vertical offsets (up/down)
            new Vector2Int(0, 1),    // up
            new Vector2Int(0, -1),   // down
            
            // Then try diagonal offsets
            new Vector2Int(1, 1),    // up-right
            new Vector2Int(-1, 1),   // up-left
            new Vector2Int(1, -1),   // down-right
            new Vector2Int(-1, -1)   // down-left
        };
        
        // Try each position in our priority order
        foreach (Vector2Int offset in prioritizedOffsets)
        {
            Vector2Int testPos = targetGridPos + offset;
            if (TryPositionAt(testPos))
            {
                Debug.Log($"Successfully snapped to offset position: {testPos}");
                return true;
            }
        }
        
        // If still no valid position, try the original position we were dragged from
        if (TryPositionAt(PivotGridPosition))
        {
            Debug.Log($"Falling back to original position: {PivotGridPosition}");
            return true;
        }
        
        // If all attempts failed, return false
        return false;
    }
    
    // Helper method to try positioning the block at a specific grid position
    private bool TryPositionAt(Vector2Int gridPos)
    {
        // Store current grid position
        Vector2Int originalPosition = PivotGridPosition;
        
        // Update pivot grid position
        PivotGridPosition = gridPos;
        
        // Check if position is valid
        if (IsValidPosition())
        {
            // If valid, update the board and visual position
            PlaceOnBoard();
            UpdateVisualPosition();
            return true;
        }
        else
        {
            // Revert to original position
            PivotGridPosition = originalPosition;
            return false;
        }
    }
    
    // Convert world position to grid position
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // Calculate relative position from the board's origin
        float relativeX = worldPos.x - board.transform.position.x;
        float relativeY = worldPos.y - board.transform.position.y;
        
        // Convert to grid coordinates
        int gridX = Mathf.RoundToInt(relativeX / board.cellSize);
        int gridY = Mathf.RoundToInt(relativeY / board.cellSize);
        
        return new Vector2Int(gridX, gridY);
    }
    
    // Define preset shapes for blocks
    public static List<Vector2Int> GetShapeCoordinates(string shapeName)
    {
        List<Vector2Int> shapeCoords = new List<Vector2Int>();
        
        switch (shapeName.ToLower())
        {
            case "single":
                // Single 1x1 square
                shapeCoords.Add(new Vector2Int(0, 0));
                break;
                
            case "horizontal2":
                // 2x1 horizontal rectangle
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(1, 0));
                break;
                
            case "vertical2":
                // 1x2 vertical rectangle
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(0, 1));
                break;
                
            case "square2x2":
                // 2x2 square
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(1, 0));
                shapeCoords.Add(new Vector2Int(0, 1));
                shapeCoords.Add(new Vector2Int(1, 1));
                break;
                
            case "lshape":
                // L-shape
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(0, 1));
                shapeCoords.Add(new Vector2Int(1, 0));
                break;
                
            case "tshape":
                // T-shape
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(-1, 0));
                shapeCoords.Add(new Vector2Int(1, 0));
                shapeCoords.Add(new Vector2Int(0, 1));
                break;
                
            case "zigzag":
                // S/Z shape
                shapeCoords.Add(new Vector2Int(0, 0));
                shapeCoords.Add(new Vector2Int(1, 0));
                shapeCoords.Add(new Vector2Int(1, 1));
                shapeCoords.Add(new Vector2Int(2, 1));
                break;
                
            default:
                // Default to a single block
                shapeCoords.Add(new Vector2Int(0, 0));
                break;
        }
        
        return shapeCoords;
    }

    // Method to move the block to a specific world position
    public void MoveTo(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }

    // Method to show visual feedback when in invalid position
    public void SetInvalidPlacementVisual(bool isInvalid)
    {
        // Visual feedback - red tint when invalid, normal color when valid
        foreach (SpriteRenderer renderer in cellRenderers)
        {
            if (renderer == null) continue;
            
            if (isInvalid)
            {
                // Add red tint and lower opacity to indicate invalid position
                renderer.color = new Color(1.0f, 0.0f, 0.0f, 0.8f);
            }
            else
            {
                // Restore normal color
                switch (color)
                {
                    case BlockColor.Red:
                        renderer.color = Color.red;
                        break;
                    case BlockColor.Green:
                        renderer.color = Color.green;
                        break;
                    case BlockColor.Blue:
                        renderer.color = Color.blue;
                        break;
                    case BlockColor.Yellow:
                        renderer.color = Color.yellow;
                        break;
                }
            }
        }
    }

    // Method to show visual feedback when dragging
    public void SetDragVisualState(bool isDragging)
    {
        // Visual feedback - slightly transparent when dragging
        foreach (SpriteRenderer renderer in cellRenderers)
        {
            if (renderer == null) continue;
            
            Color currentColor = renderer.color;
            currentColor.a = isDragging ? 0.7f : 1.0f;
            renderer.color = currentColor;
        }
    }
}