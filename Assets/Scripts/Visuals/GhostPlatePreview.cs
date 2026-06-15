using UnityEngine;

public class GhostPlatePreview : MonoBehaviour
{
    public static GhostPlatePreview Instance { get; private set; }

    [Header("Ghost Mesh Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color ghostColor = new Color(0.2f, 1f, 0.2f, 0.4f);

    [Header("Tile Highlight Settings")]
    [Tooltip("How many world units to raise the hovered cell tile")]
    [SerializeField] private float tileLiftAmount = 0.12f;
    [Tooltip("How much to brighten each RGB channel of the hovered tile (0–1)")]
    [SerializeField] private float tileBrightnessBoost = 0.35f;

    private GameObject _ghostInstance;
    private MeshFilter _ghostMeshFilter;
    private MeshRenderer _ghostMeshRenderer;

    private Cell _highlightedCell;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (HasConfiguredMaterial() && !Instance.HasConfiguredMaterial())
        {
            Destroy(Instance.gameObject);
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private bool HasConfiguredMaterial()
    {
        return ghostMaterial != null;
    }

    public void Show(Vector3 position, GameObject sourcePlate, Cell targetCell = null)
    {
        if (_ghostInstance == null)
        {
            _ghostInstance = new GameObject("GhostPlatePreview");

            MeshFilter   sourceFilter   = sourcePlate.GetComponentInChildren<MeshFilter>();
            MeshRenderer sourceRenderer = sourcePlate.GetComponentInChildren<MeshRenderer>();

            if (sourceFilter != null && sourceRenderer != null)
            {
                _ghostMeshFilter   = _ghostInstance.AddComponent<MeshFilter>();
                _ghostMeshRenderer = _ghostInstance.AddComponent<MeshRenderer>();

                _ghostMeshFilter.sharedMesh = sourceFilter.sharedMesh;

                if (ghostMaterial != null)
                {
                    _ghostMeshRenderer.sharedMaterial = ghostMaterial;
                }
                else
                {
                    // Create semi-transparent material if none is specified
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader == null) standardShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

                    Material fallbackMat = new Material(standardShader);
                    fallbackMat.name = "GhostMaterial_Fallback";

                    fallbackMat.SetFloat("_Mode", 3f); // Transparent
                    fallbackMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    fallbackMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    fallbackMat.SetInt("_ZWrite", 0);
                    fallbackMat.DisableKeyword("_ALPHATEST_ON");
                    fallbackMat.EnableKeyword("_ALPHABLEND_ON");
                    fallbackMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    fallbackMat.renderQueue = 3000;
                    fallbackMat.color = ghostColor;

                    _ghostMeshRenderer.sharedMaterial = fallbackMat;
                }
            }
            else
            {
                // Fallback: Create a cylinder shape if visual meshes are missing
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(cylinder.GetComponent<Collider>());
                cylinder.transform.SetParent(_ghostInstance.transform);
                cylinder.transform.localPosition = Vector3.zero;
                cylinder.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);

                Renderer r = cylinder.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Standard"));
                m.SetFloat("_Mode", 3f);
                m.color = ghostColor;
                r.sharedMaterial = m;
            }
        }

        _ghostInstance.transform.position   = position;
        _ghostInstance.transform.localScale = sourcePlate.transform.localScale;
        _ghostInstance.SetActive(true);

        if (_highlightedCell != null && _highlightedCell != targetCell)
        {
            _highlightedCell.ResetTile();
            _highlightedCell = null;
        }

        if (targetCell != null && _highlightedCell != targetCell)
        {
            _highlightedCell = targetCell;
            _highlightedCell.LiftTile(tileLiftAmount, tileBrightnessBoost);
        }
    }

    public void Hide()
    {
        if (_ghostInstance != null)
        {
            _ghostInstance.SetActive(false);
        }

        if (_highlightedCell != null)
        {
            _highlightedCell.ResetTile();
            _highlightedCell = null;
        }
    }

    private void OnDestroy()
    {
        if (_highlightedCell != null)
        {
            _highlightedCell.ResetTile();
        }

        if (_ghostInstance != null)
        {
            Destroy(_ghostInstance);
        }
    }
}

