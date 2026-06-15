using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggablePlate : MonoBehaviour
{
    [Tooltip("How close (in world units) the plate needs to be to a cell to snap")]
    [SerializeField] private float snapDistance = 1.5f;

    [Tooltip("How many world units to lift the plate above its resting Y while dragging (prevents tiles from covering it)")]
    [SerializeField] private float dragLiftOffset = 0.3f;

    [HideInInspector] public HoldSlot parentSlot; // Reference to the slot this plate was spawned in

    private bool    isDragging = false;

    public bool IsDragging => isDragging;

    private static int _activeDragCount = 0;

    public static bool IsAnyDragging()
    {
        return _activeDragCount > 0;
    }

    public static void CancelAllActiveDrags()
    {
        DraggablePlate[] plates = FindObjectsByType<DraggablePlate>(FindObjectsSortMode.None);
        foreach (DraggablePlate plate in plates)
        {
            if (plate != null && plate.isDragging) plate.CancelDrag();
        }
        _activeDragCount = 0; // Force reset to prevent drift
        if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();
    }
    private Vector3 originalPosition;
    private float   _liftedY;          // Y used during drag (originalPosition.y + dragLiftOffset)
    private GridManager gridManager;
    private MergeManager mergeManager;
    private Camera mainCamera;
    private Plane  dragPlane;

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
        StopAllCoroutines();
        if (isDragging)
        {
            isDragging = false;
            _activeDragCount--;
        }
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
            if (!isDragging)
            {
                isDragging = true;
                _activeDragCount++;
            }
            originalPosition = transform.position;

            // Lift the plate above tile level while dragging so tiles don't cover it
            _liftedY  = originalPosition.y + dragLiftOffset;
            dragPlane = new Plane(Vector3.up, new Vector3(0, _liftedY, 0));

            // Snap plate up immediately on pickup
            transform.position = new Vector3(transform.position.x, _liftedY, transform.position.z);
        }
    }

    private void Drag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float dist))
        {
            Vector3 worldPoint = ray.GetPoint(dist);
            // Keep the plate at the lifted Y while dragging
            transform.position = new Vector3(worldPoint.x, _liftedY, worldPoint.z);
            
            // Check for nearest cell for snap preview
            Cell nearestCell = gridManager.GetNearestCell(transform.position, snapDistance);
            if (nearestCell != null && !nearestCell.IsOccupied)
            {
                if (GhostPlatePreview.Instance != null)
                {
                    // Show ghost at the height of the current plate
                    Vector3 previewPos = new Vector3(nearestCell.transform.position.x, originalPosition.y, nearestCell.transform.position.z);
                    GhostPlatePreview.Instance.Show(previewPos, gameObject, nearestCell);
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
        if (isDragging)
        {
            isDragging = false;
            _activeDragCount--;
        }
        
        if (GhostPlatePreview.Instance != null) GhostPlatePreview.Instance.Hide();
        
        // Find absolute nearest cell
        Cell nearestCell = gridManager.GetNearestCell(transform.position, snapDistance);

        if (nearestCell != null)
        {
            if (!nearestCell.IsOccupied)
            {
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

                if (ComboAudioPlayer.Instance != null)
                    ComboAudioPlayer.Instance.PlayPlaceSound();

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

    private void OnDestroy()
    {
        if (isDragging)
        {
            isDragging = false;
            _activeDragCount--;
        }
    }
}
