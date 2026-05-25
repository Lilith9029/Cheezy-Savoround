using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggablePlate : MonoBehaviour
{
    [Tooltip("How close (in world units) the plate needs to be to a cell to snap")]
    [SerializeField] private float snapDistance = 1.5f;

    private bool isDragging = false;
    private Vector3 originalPosition;
    private GridManager gridManager;
    private MergeManager mergeManager;
    private Camera mainCamera;
    private Plane dragPlane;

    private void Start()
    {
        mainCamera = Camera.main;
        gridManager = FindFirstObjectByType<GridManager>();
        mergeManager = FindFirstObjectByType<MergeManager>();

        if (mainCamera == null) Debug.LogError("[DraggablePlate] No Camera tagged 'MainCamera'!");
        if (gridManager == null) Debug.LogError("[DraggablePlate] GridManager not found in scene!");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryPickUp();
        if (isDragging && Input.GetMouseButton(0)) Drag();
        if (isDragging && Input.GetMouseButtonUp(0)) Drop();
    }

    private void TryPickUp()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        if (col.Raycast(ray, out RaycastHit _, 500f))
        {
            Debug.Log($"[INPUT] PICK: {gameObject.name}");
            isDragging = true;
            originalPosition = transform.position;
            dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        }
    }

    private void Drag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float dist))
        {
            Vector3 worldPoint = ray.GetPoint(dist);
            transform.position = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
        }
    }

    private void Drop()
    {
        isDragging = false;
        
        Cell nearestCell = gridManager.GetNearestFreeCell(transform.position, snapDistance);

        if (nearestCell != null)
        {
            Debug.Log($"[INPUT] DROP: {gameObject.name} -> VALID: Cell({nearestCell.x}, {nearestCell.z})");
            transform.position = new Vector3(
                nearestCell.transform.position.x,
                originalPosition.y,
                nearestCell.transform.position.z
            );
            nearestCell.currentPlate = this.gameObject;
            
            // Trigger merge check
            if (mergeManager != null) mergeManager.CheckMerges(nearestCell);
        }
        else
        {
            Debug.Log($"[INPUT] DROP: {gameObject.name} -> INVALID: Returning to Original");
            transform.position = originalPosition;
        }
    }
}
