using UnityEngine;

public class HoldSlot : MonoBehaviour
{
    [Tooltip("The actual transform where the plate sits. Defaults to this transform.")]
    public Transform anchorPoint;
    
    private GameObject _currentPlate;

    public bool IsEmpty => _currentPlate == null;
    public GameObject CurrentPlate => _currentPlate;

    private void Awake()
    {
        if (anchorPoint == null)
        {
            anchorPoint = this.transform;
        }
    }

    /// <summary>
    /// Assigns a plate to this slot and sets its position.
    /// </summary>
    public void AssignPlate(GameObject plate)
    {
        _currentPlate = plate;
        if (plate != null)
        {
            plate.transform.position = anchorPoint.position;
            plate.transform.rotation = anchorPoint.rotation;
            
            // Set this slot as the parent slot of the plate so it can release it when placed
            DraggablePlate draggable = plate.GetComponent<DraggablePlate>();
            if (draggable != null)
            {
                draggable.parentSlot = this;
            }
        }
    }

    /// <summary>
    /// Clears the plate assignment for this slot.
    /// </summary>
    public void ClearSlot()
    {
        _currentPlate = null;
    }
}
