using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggablePlate : MonoBehaviour
{
    [Tooltip("How close (in world units) the plate needs to be to a cell to snap")]
    [SerializeField] private float snapDistance = 1.5f;

    [HideInInspector] public HoldSlot parentSlot; // Reference to the slot this plate was spawned in

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
        // Allow pickups during CheckingCombo and Animating for asynchronous fast-paced gameplay!
        bool locked = GameManager.Instance != null
                   && (GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Menu);

        // Exception: GameOver cancels all interaction immediately
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
        {
            if (isDragging) CancelDrag();
            return;
        }

        if (!locked)
        {
            if (Input.GetMouseButtonDown(0)) TryPickUp();
        }

        if (isDragging && Input.GetMouseButton(0)) Drag();
        if (isDragging && Input.GetMouseButtonUp(0)) Drop();
    }

    /// <summary>Cancels a drag in progress without snap — returns to original position silently.</summary>
    private void CancelDrag()
    {
        isDragging = false;
        if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();
        transform.position = originalPosition;
    }

    private void TryPickUp()
    {
        // If this plate is already placed on the grid (no longer in a hold slot), it cannot be picked up
        if (parentSlot == null) return;

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
            
            // Check for nearest cell for snap preview
            Cell nearestCell = gridManager.GetNearestCell(transform.position, snapDistance);
            if (nearestCell != null && !nearestCell.IsOccupied)
            {
                if (GhostPlatePreview.Instance != null)
                {
                    // Show ghost at the height of the current plate
                    Vector3 previewPos = new Vector3(nearestCell.transform.position.x, originalPosition.y, nearestCell.transform.position.z);
                    GhostPlatePreview.Instance.Show(previewPos, gameObject);
                }
            }
            else
            {
                if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();
            }
        }
    }

    private void Drop()
    {
        isDragging = false;
        
        if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();
        
        // Find absolute nearest cell
        Cell nearestCell = gridManager.GetNearestCell(transform.position, snapDistance);

        if (nearestCell != null)
        {
            if (!nearestCell.IsOccupied)
            {
                Debug.Log($"[INPUT] DROP: {gameObject.name} -> VALID: Cell({nearestCell.x}, {nearestCell.z})");
                transform.position = new Vector3(
                    nearestCell.transform.position.x,
                    originalPosition.y,
                    nearestCell.transform.position.z
                );
                nearestCell.currentPlate = this.gameObject;
                
                // Trigger juice bounce animation on landing
                PizzaPlate pizzaPlate = GetComponent<PizzaPlate>();
                if (pizzaPlate != null)
                {
                    pizzaPlate.PlayBounceAnimation();
                }
                
                // Free the Hold Slot this was dragged from
                if (parentSlot != null)
                {
                    parentSlot.ClearSlot();
                    parentSlot = null;
                }

                // Check if Hold Slots need a refill
                if (HoldSlotsManager.Instance != null)
                {
                    HoldSlotsManager.Instance.CheckAndRefillSlots();
                }
                
                // Trigger merge check
                if (mergeManager != null) mergeManager.CheckMerges(nearestCell);
            }
            else
            {
                Debug.Log($"[INPUT] DROP: {gameObject.name} -> INVALID: Cell Occupied. Target Plate shakes.");
                PizzaPlate occupiedPlate = nearestCell.currentPlate.GetComponent<PizzaPlate>();
                if (occupiedPlate != null)
                {
                    occupiedPlate.PlayShakeAnimation();
                }
                StartCoroutine(ReturnToOriginalRoutine());
            }
        }
        else
        {
            Debug.Log($"[INPUT] DROP: {gameObject.name} -> INVALID: Out of bounds. Returning.");
            StartCoroutine(ReturnToOriginalRoutine());
        }
    }

    private System.Collections.IEnumerator ReturnToOriginalRoutine()
    {
        Vector3 startPos = transform.position;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Interpolate back to starting position smoothly
            transform.position = Vector3.Lerp(startPos, originalPosition, t);
            yield return null;
        }

        transform.position = originalPosition;
    }
}
