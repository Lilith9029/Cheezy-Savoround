using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private string configFileName = "GridConfig";
    
    private void Start()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        // Load JSON config from Resources
        TextAsset configAsset = Resources.Load<TextAsset>(configFileName);
        if (configAsset == null)
        {
            Debug.LogError($"Could not find configuration file: {configFileName} in Resources");
            return;
        }

        GridData data = JsonUtility.FromJson<GridData>(configAsset.text);
        if (data == null)
        {
            Debug.LogError("Failed to parse GridData from JSON.");
            return;
        }

        GenerateGrid(data);
    }

    private Cell[,] gridCells;

    private void GenerateGrid(GridData data)
    {
        gridCells = new Cell[data.width, data.height];
        
        // Center the grid
        Vector3 offset = new Vector3(
            (data.width - 1) * data.spacing / 2f,
            0,
            (data.height - 1) * data.spacing / 2f
        );

        for (int x = 0; x < data.width; x++)
        {
            for (int z = 0; z < data.height; z++)
            {
                Vector3 localPos = new Vector3(x * data.spacing, 0, z * data.spacing) - offset;
                
                // Create an empty object for each cell (Detection only)
                GameObject cellObj = new GameObject($"Cell_{x}_{z}");
                cellObj.transform.SetParent(this.transform);
                cellObj.transform.localPosition = localPos; // Use LOCAL position so Gizmos match
                
                // Add BoxCollider for Raycasting (NOT a trigger - must be solid for Raycast)
                BoxCollider collider = cellObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(data.spacing, 0.1f, data.spacing);
                collider.isTrigger = false;
                
                // Add Cell component
                Cell cell = cellObj.AddComponent<Cell>();
                cell.Initialize(x, z);
                gridCells[x, z] = cell;
                
                // Set Layer for Raycasting (Layer 6: Grid)
                cellObj.layer = 6; 
            }
        }
    }

    private void OnDrawGizmos()
    {
        TextAsset configAsset = Resources.Load<TextAsset>(configFileName);
        if (configAsset == null) return;
        GridData data = JsonUtility.FromJson<GridData>(configAsset.text);
        if (data == null) return;

        // Draw in LOCAL space of GridManager - always matches cell positions
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;

        float s = data.spacing;
        float halfS = s / 2f;
        Vector3 offset = new Vector3(
            (data.width - 1) * s / 2f,
            0,
            (data.height - 1) * s / 2f
        );

        // Vertical lines (along Z axis)
        for (int x = 0; x <= data.width; x++)
        {
            float lx = x * s - halfS - offset.x;
            Vector3 start = new Vector3(lx, 0, -halfS - offset.z);
            Vector3 end   = new Vector3(lx, 0, data.height * s - halfS - offset.z);
            Gizmos.DrawLine(start, end);
        }

        // Horizontal lines (along X axis)
        for (int z = 0; z <= data.height; z++)
        {
            float lz = z * s - halfS - offset.z;
            Vector3 start = new Vector3(-halfS - offset.x, 0, lz);
            Vector3 end   = new Vector3(data.width * s - halfS - offset.x, 0, lz);
            Gizmos.DrawLine(start, end);
        }

        // Draw a yellow dot at the center of each cell for verification
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        for (int x = 0; x < data.width; x++)
            for (int z = 0; z < data.height; z++)
            {
                Vector3 center = new Vector3(x * s, 0, z * s) - offset;
                Gizmos.DrawSphere(center, 0.05f);
            }

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;
    }

    public Cell GetCell(int x, int z)
    {
        if (x >= 0 && x < gridCells.GetLength(0) && z >= 0 && z < gridCells.GetLength(1))
        {
            return gridCells[x, z];
        }
        return null;
    }

    public System.Collections.Generic.List<Cell> GetNeighbors(int x, int z)
    {
        System.Collections.Generic.List<Cell> neighbors = new System.Collections.Generic.List<Cell>();
        int[,] dirs = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };

        for (int i = 0; i < 4; i++)
        {
            Cell neighbor = GetCell(x + dirs[i, 0], z + dirs[i, 1]);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    public System.Collections.Generic.List<Cell> GetMatchingNeighbors(int x, int z, string type)
    {
        System.Collections.Generic.List<Cell> matches = new System.Collections.Generic.List<Cell>();
        foreach (Cell neighbor in GetNeighbors(x, z))
        {
            if (neighbor.IsOccupied)
            {
                PizzaPlate plate = neighbor.currentPlate.GetComponent<PizzaPlate>();
                if (plate != null && plate.pizzaType == type)
                {
                    matches.Add(neighbor);
                }
            }
        }
        return matches;
    }
    public Cell GetNearestCell(Vector3 worldPos, float maxDistance)
    {
        Cell nearest = null;
        float bestDist = maxDistance;

        foreach (Cell cell in gridCells)
        {
            if (cell == null) continue;

            // Compare on XZ plane only (ignore Y)
            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(cell.transform.position.x, cell.transform.position.z)
            );

            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = cell;
            }
        }

        return nearest;
    }
}
