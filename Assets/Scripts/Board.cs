using UnityEngine;

public class Board : MonoBehaviour
{
    // Grid dimensions
    public int gridWidth = 10;
    public int gridHeight = 10;

    // Size of each cell (in Unity units)
    public float cellSize = 1f;

    // 2D array to track block positions
    private Block[,] grid;

    // Debugging visual representation
    public bool showGridGizmos = true;

    void Awake()
    {
        // Initialize the grid array
        grid = new Block[gridWidth, gridHeight];

        // Center the grid
        CenterGridPosition();

        // Debug logging
        Debug.Log($"Board initialized with grid size: {gridWidth}x{gridHeight}");
    }

    void CenterGridPosition()
    {
        // Calculate the total grid width and height
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        // Calculate the offset to center the grid's midpoint
        float offsetX = -(totalWidth / 2f);
        float offsetY = -(totalHeight / 2f);

        // Set the transform position to center the grid
        transform.position = new Vector3(offsetX, offsetY, 0);
    }

    // Method to place a block at a specific grid position
    public void PlaceBlock(Block block, Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            // Remove from previous position if exists
            RemoveBlock(gridPosition);

            // Update grid tracking
            grid[gridPosition.x, gridPosition.y] = block;

            // Update block's world position
            Vector3 worldPosition = GetWorldPositionFromGridPosition(gridPosition);
            block.transform.position = worldPosition;

            Debug.Log($"Placed block at grid position: {gridPosition}, world position: {worldPosition}");
        }
        else
        {
            Debug.LogWarning($"Invalid grid position: {gridPosition}");
        }
    }

    // Remove a block from a specific grid position
    public void RemoveBlock(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            Block blockToRemove = grid[gridPosition.x, gridPosition.y];
            if (blockToRemove != null)
            {
                // Remove from grid tracking
                grid[gridPosition.x, gridPosition.y] = null;
                Debug.Log($"Removed block from grid position: {gridPosition}");
            }
        }
    }

    // Convert grid position to world position
    public Vector3 GetWorldPositionFromGridPosition(Vector2Int gridPosition)
    {
        // Calculate world position based on grid origin and cell size
        float x = transform.position.x + (gridPosition.x * cellSize) + (cellSize / 2f);
        float y = transform.position.y + (gridPosition.y * cellSize) + (cellSize / 2f);
        
        return new Vector3(x, y, 0);
    }

    // Validate grid position
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    // Get block at a specific position
    public Block GetBlockAtPosition(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            return grid[gridPosition.x, gridPosition.y];
        }
        return null;
    }

    // Visualize grid in scene view
    void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        Gizmos.color = Color.white;
        Vector3 gridOrigin = transform.position;

        // Draw vertical lines
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = start + new Vector3(0, gridHeight * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = gridOrigin + new Vector3(0, y * cellSize, 0);
            Vector3 end = start + new Vector3(gridWidth * cellSize, 0, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}