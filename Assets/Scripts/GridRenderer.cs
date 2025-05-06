using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public Board board;
    public Color gridLineColor = Color.gray;
    public float lineThickness = 0.05f;

    void Start()
    {
        // Ensure we have a reference to the board
        if (board == null)
        {
            board = GetComponent<Board>();
        }

        // Create grid lines
        CreateGridLines();
    }

    void CreateGridLines()
    {
        // Vertical lines
        for (int x = 0; x <= board.gridWidth; x++)
        {
            CreateLine(
                new Vector3(transform.position.x + x * board.cellSize, transform.position.y, 0),
                new Vector3(transform.position.x + x * board.cellSize, transform.position.y + board.gridHeight * board.cellSize, 0)
            );
        }

        // Horizontal lines
        for (int y = 0; y <= board.gridHeight; y++)
        {
            CreateLine(
                new Vector3(transform.position.x, transform.position.y + y * board.cellSize, 0),
                new Vector3(transform.position.x + board.gridWidth * board.cellSize, transform.position.y + y * board.cellSize, 0)
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(transform);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.startColor = gridLineColor;
        lineRenderer.endColor = gridLineColor;
        
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}