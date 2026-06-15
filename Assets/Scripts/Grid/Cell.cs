using UnityEngine;

public class Cell : MonoBehaviour
{
    public int x;
    public int z;
    public GameObject currentPlate;

    [System.NonSerialized]
    public System.Collections.Generic.List<Cell> neighbors = new System.Collections.Generic.List<Cell>();

    /// <summary>The visual tile prefab spawned at this cell's center.</summary>
    public GameObject tilePrefabInstance;

    private Vector3 _originalTileLocalPos;
    private Color   _originalTileColor;
    private bool    _tileLifted = false;

    public bool IsOccupied => currentPlate != null;

    public void Initialize(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    /// <summary>
    /// Called by GridManager after spawning the tile so we cache its resting position.
    /// </summary>
    public void RegisterTile(GameObject tile)
    {
        tilePrefabInstance = tile;
        if (tile != null)
        {
            _originalTileLocalPos = tile.transform.localPosition;
            Renderer r = tile.GetComponentInChildren<Renderer>();
            if (r != null) _originalTileColor = r.material.color;
        }
    }

    /// <summary>
    /// Lifts the tile upward and brightens its color to signal a valid drop target.
    /// </summary>
    /// <param name="liftAmount">How many world units to raise the tile.</param>
    /// <param name="brightnessBoost">How much to add to each RGB channel (0–1).</param>
    public void LiftTile(float liftAmount = 0.12f, float brightnessBoost = 0.35f)
    {
        if (tilePrefabInstance == null || _tileLifted) return;

        tilePrefabInstance.transform.localPosition =
            _originalTileLocalPos + new Vector3(0f, liftAmount, 0f);

        Renderer r = tilePrefabInstance.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Color boosted = _originalTileColor;
            boosted.r = Mathf.Clamp01(boosted.r + brightnessBoost);
            boosted.g = Mathf.Clamp01(boosted.g + brightnessBoost);
            boosted.b = Mathf.Clamp01(boosted.b + brightnessBoost);
            r.material.color = boosted;
        }

        _tileLifted = true;
    }

    /// <summary>
    /// Resets the tile back to its original resting position and color.
    /// </summary>
    public void ResetTile()
    {
        if (tilePrefabInstance == null || !_tileLifted) return;

        tilePrefabInstance.transform.localPosition = _originalTileLocalPos;

        Renderer r = tilePrefabInstance.GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = _originalTileColor;

        _tileLifted = false;
    }
}
