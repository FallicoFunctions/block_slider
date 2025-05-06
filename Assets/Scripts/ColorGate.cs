using UnityEngine;

public class ColorGate : MonoBehaviour
{
    public Block.BlockColor gateColor;
    public Board board;
    public Vector2Int gridPosition;
    public string direction; // "top", "bottom", "left", "right"

    // Visual properties
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        // Set the color based on gate color
        switch (gateColor)
        {
            case Block.BlockColor.Red:
                spriteRenderer.color = Color.red;
                break;
            case Block.BlockColor.Green:
                spriteRenderer.color = Color.green;
                break;
            case Block.BlockColor.Blue:
                spriteRenderer.color = Color.blue;
                break;
            case Block.BlockColor.Yellow:
                spriteRenderer.color = Color.yellow;
                break;
        }
    }

    // Check if the block can exit through this gate
    public bool CanExitThroughGate(Block block)
    {
        if (block == null)
        {
            return false;
        }
        // Check if the colors match
        return block.color == gateColor;
    }

    // Process a block exiting
    public void ProcessBlockExit(GameObject blockObj)
    {
        if (blockObj == null)
        {
            return;
        }
        Block block = blockObj.GetComponent<Block>();
        if (block != null && CanExitThroughGate(block))
        {
            // Remove the block from the board using the new method
            board.RemoveBlock(gridPosition);

            // Animate the block moving out through the gate
            // This is just a simple implementation - you'll want to enhance this
            block.MoveTo(transform.position);
            Destroy(blockObj, 0.5f); // Destroy after a short delay to allow for animation
            Debug.Log("Block exited through " + direction + " gate!");
        }
    }
}