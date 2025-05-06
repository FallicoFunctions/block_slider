using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public Board board;
    
    // References to color-specific prefabs
    public GameObject redBlockPrefab;
    public GameObject greenBlockPrefab;
    public GameObject blueBlockPrefab;
    public GameObject yellowBlockPrefab;

    // Default fallback prefab (can be removed if no longer needed)
    public GameObject blockPrefab;
    
    // Sprite to use for block cells (fallback if not set in prefab)
    public Sprite blockCellSprite;
    
    void Start()
    {
        // Debug logging to verify references
        Debug.Log("LevelLoader Start method called");
        
        if (board == null)
        {
            Debug.LogError("Board reference is missing!");
            return;
        }

        if (blockPrefab == null)
        {
            Debug.LogError("Block prefab is missing!");
            return;
        }

        LoadLevel(1); // Load the first level
    }

    // Load a specific level by number
    public void LoadLevel(int levelNumber)
    {
        Debug.Log($"Loading level {levelNumber}");
        
        // Clear any existing blocks
        ClearExistingBlocks();
        
        // Load the appropriate level
        switch (levelNumber)
        {
            case 1:
                LoadLevel1();
                break;
            case 2:
                LoadLevel2();
                break;
            // Add more levels as needed
            default:
                LoadLevel1(); // Default to level 1
                break;
        }
    }
    
    // Clear any existing blocks from the board
    private void ClearExistingBlocks()
    {
        // Find all blocks in the scene
        Block[] existingBlocks = FindObjectsOfType<Block>();
        
        // Destroy each block
        foreach (Block block in existingBlocks)
        {
            Destroy(block.gameObject);
        }
        
        // Reset the board grid
        for (int x = 0; x < board.gridWidth; x++)
        {
            for (int y = 0; y < board.gridHeight; y++)
            {
                board.RemoveBlock(new Vector2Int(x, y));
            }
        }
    }
    
    // Level 1 configuration
    private void LoadLevel1()
    {
        // Create blocks with various shapes and colors
        CreateBlock(Block.BlockColor.Red, new Vector2Int(1, 1), "single");
        CreateBlock(Block.BlockColor.Green, new Vector2Int(3, 2), "horizontal2");
        CreateBlock(Block.BlockColor.Blue, new Vector2Int(5, 3), "vertical2");
        CreateBlock(Block.BlockColor.Yellow, new Vector2Int(7, 4), "lshape");
        CreateBlock(Block.BlockColor.Red, new Vector2Int(1, 6), "tshape");
        CreateBlock(Block.BlockColor.Green, new Vector2Int(5, 7), "square2x2");
        CreateBlock(Block.BlockColor.Blue, new Vector2Int(8, 8), "zigzag");
    }
    
    // Level 2 configuration
    private void LoadLevel2()
    {
        // Create a more challenging level with different block arrangements
        CreateBlock(Block.BlockColor.Red, new Vector2Int(0, 0), "square2x2");
        CreateBlock(Block.BlockColor.Green, new Vector2Int(3, 1), "tshape");
        CreateBlock(Block.BlockColor.Blue, new Vector2Int(7, 2), "lshape");
        CreateBlock(Block.BlockColor.Yellow, new Vector2Int(2, 5), "zigzag");
        CreateBlock(Block.BlockColor.Red, new Vector2Int(6, 6), "horizontal2");
        CreateBlock(Block.BlockColor.Green, new Vector2Int(8, 8), "vertical2");
    }
    
    // Custom method to create blocks with custom shapes
    public void CreateCustomBlock(Block.BlockColor color, Vector2Int position, List<Vector2Int> customShape)
    {
        // Instantiate from prefab
        GameObject blockObject = Instantiate(blockPrefab, board.transform);
        
        // Get or add Block component
        Block block = blockObject.GetComponent<Block>();
        if (block == null)
        {
            block = blockObject.AddComponent<Block>();
        }
        
        // Set block properties
        block.color = color;
        
        // Initialize the block with custom shape
        block.Initialize(board, position, customShape);
        
        // Name the block for easier debugging
        blockObject.name = $"{color}Block_Custom";
        
        Debug.Log($"Created custom {color} block at position {position}");
    }
    
    // Example method to create a block with a custom shape at runtime
    public void CreateCustomShapeExample()
    {
        // Define a custom U-shape
        List<Vector2Int> uShape = new List<Vector2Int>
        {
            new Vector2Int(0, 0), // Bottom left
            new Vector2Int(1, 0), // Bottom middle
            new Vector2Int(2, 0), // Bottom right
            new Vector2Int(0, 1), // Middle left
            new Vector2Int(0, 2), // Top left
            new Vector2Int(1, 2), // Top middle
            new Vector2Int(2, 2), // Top right
            new Vector2Int(2, 1)  // Middle right
        };
        
        // Create the custom U-shaped block
        CreateCustomBlock(Block.BlockColor.Blue, new Vector2Int(3, 3), uShape);
    }

    private void CreateBlock(Block.BlockColor color, Vector2Int position, string shapeName)
    {
        // Select the appropriate prefab based on color
        GameObject prefabToUse;
        
        switch (color)
        {
            case Block.BlockColor.Red:
                prefabToUse = redBlockPrefab;
                break;
            case Block.BlockColor.Green:
                prefabToUse = greenBlockPrefab;
                break;
            case Block.BlockColor.Blue:
                prefabToUse = blueBlockPrefab;
                break;
            case Block.BlockColor.Yellow:
                prefabToUse = yellowBlockPrefab;
                break;
            default:
                prefabToUse = blockPrefab; // Fallback
                break;
        }
        
        // Instantiate from the selected prefab
        GameObject blockObject = Instantiate(prefabToUse, board.transform);
        
        // Get Block component
        Block block = blockObject.GetComponent<Block>();
        if (block == null)
        {
            block = blockObject.AddComponent<Block>();
        }
        
        // Set block properties
        block.color = color;
        
        // Get shape coordinates based on shape name
        List<Vector2Int> shapeCoords = Block.GetShapeCoordinates(shapeName);
        
        // Initialize the block
        block.Initialize(board, position, shapeCoords);
        
        // Name the block for easier debugging
        blockObject.name = $"{color}Block_{shapeName}";
        
        Debug.Log($"Created {color} block with shape '{shapeName}' at position {position}");
    }
}