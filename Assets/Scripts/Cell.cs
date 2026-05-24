using UnityEngine;

public class Cell : MonoBehaviour
{
    public int x;
    public int z;
    public GameObject currentPlate;

    public bool IsOccupied => currentPlate != null;

    public void Initialize(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}
