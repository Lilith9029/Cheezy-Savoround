using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private string configFileName = "GridConfig";

    [Header("Grid Tile Visuals")]
    [Tooltip("First tile prefab (checkerboard pattern A)")]
    [SerializeField] private GameObject tilePrefabA;
    [Tooltip("Second tile prefab (checkerboard pattern B)")]
    [SerializeField] private GameObject tilePrefabB;
    [Tooltip("Y offset of tile relative to cell center")]
    [SerializeField] private float tileYOffset = 0f;

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
            return;
        }

        GridData data = JsonUtility.FromJson<GridData>(configAsset.text);
        if (data == null)
        {
            return;
        }

        GenerateGrid(data);
    }

    private Cell[,] gridCells;
    private Cell[] flatCells;

    public Cell[] AllCells => flatCells;

    private void GenerateGrid(GridData data)
    {
        gridCells = new Cell[data.width, data.height];
        System.Collections.Generic.List<Cell> cellList = new System.Collections.Generic.List<Cell>();
        
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
                cellList.Add(cell);

                // Set Layer for Raycasting (Layer 6: Grid)
                cellObj.layer = 6;

                // Spawn checkerboard tile at cell center
                SpawnTile(cell, localPos, x, z);
            }
        }
        flatCells = cellList.ToArray();

        // Pre-populate neighbors for all cells to achieve zero allocation lookups
        for (int x = 0; x < data.width; x++)
        {
            for (int z = 0; z < data.height; z++)
            {
                Cell cell = gridCells[x, z];
                if (cell != null)
                {
                    cell.neighbors.Clear();
                    int[,] dirs = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };
                    for (int i = 0; i < 4; i++)
                    {
                        Cell neighbor = GetCell(x + dirs[i, 0], z + dirs[i, 1]);
                        if (neighbor != null)
                        {
                            cell.neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }
    }

    private void SpawnTile(Cell cell, Vector3 localPos, int x, int z)
    {
        // Pick prefab based on checkerboard pattern
        bool isA = (x + z) % 2 == 0;
        GameObject prefab = isA ? tilePrefabA : tilePrefabB;

        if (prefab == null) return;

        Vector3 tilePos = localPos + new Vector3(0f, tileYOffset, 0f);
        GameObject tile = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, this.transform);
        tile.transform.localPosition = tilePos;
        tile.name = $"Tile_{x}_{z}";

        // Register tile into Cell so it can be lifted/reset by GhostPlatePreview
        cell.RegisterTile(tile);
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
        if (gridCells != null && x >= 0 && x < gridCells.GetLength(0) && z >= 0 && z < gridCells.GetLength(1))
        {
            return gridCells[x, z];
        }
        return null;
    }

    private static readonly System.Collections.Generic.List<Cell> _emptyNeighbors = new System.Collections.Generic.List<Cell>();

    public System.Collections.Generic.List<Cell> GetNeighbors(int x, int z)
    {
        Cell cell = GetCell(x, z);
        return cell != null ? cell.neighbors : _emptyNeighbors;
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

        if (flatCells != null)
        {
            foreach (Cell cell in flatCells)
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
        }

        return nearest;
    }

    /// <summary>
    /// Destroys all active pizza plates on the grid.
    /// </summary>
    public void ClearAllPlates()
    {
        if (gridCells == null) return;
        foreach (Cell cell in gridCells)
        {
            if (cell != null && cell.IsOccupied)
            {
                Destroy(cell.currentPlate);
                cell.currentPlate = null;
            }
        }
    }
}
