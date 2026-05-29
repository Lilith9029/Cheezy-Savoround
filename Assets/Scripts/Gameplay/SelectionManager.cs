using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private LayerMask gridLayer;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (gridLayer == 0) gridLayer = 1 << 6; // Default to Layer 6
    }

    public Cell GetCellUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, gridLayer))
        {
            Debug.Log($"Raycast hit: {hit.collider.name}");
            return hit.collider.GetComponent<Cell>();
        }
        Debug.Log("Raycast hit nothing on grid layer.");
        return null;
    }
}
