using UnityEngine;

public class GhostPlatePreview : MonoBehaviour
{
    public static GhostPlatePreview Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color ghostColor = new Color(0.2f, 1f, 0.2f, 0.4f); // Semi-transparent green
    
    private GameObject _ghostInstance;
    private MeshFilter _ghostMeshFilter;
    private MeshRenderer _ghostMeshRenderer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Displays the ghost mesh preview at the target cell position.
    /// </summary>
    public void Show(Vector3 position, GameObject sourcePlate)
    {
        if (_ghostInstance == null)
        {
            _ghostInstance = new GameObject("GhostPlatePreview");
            
            // Try to find the visual mesh in the source plate
            MeshFilter sourceFilter = sourcePlate.GetComponentInChildren<MeshFilter>();
            MeshRenderer sourceRenderer = sourcePlate.GetComponentInChildren<MeshRenderer>();

            if (sourceFilter != null && sourceRenderer != null)
            {
                _ghostMeshFilter = _ghostInstance.AddComponent<MeshFilter>();
                _ghostMeshRenderer = _ghostInstance.AddComponent<MeshRenderer>();
                
                _ghostMeshFilter.sharedMesh = sourceFilter.sharedMesh;
                
                if (ghostMaterial != null)
                {
                    _ghostMeshRenderer.sharedMaterial = ghostMaterial;
                }
                else
                {
                    // Create a beautiful custom semi-transparent material if none is specified
                    Shader standardShader = Shader.Find("Standard");
                    if (standardShader == null) standardShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                    
                    Material fallbackMat = new Material(standardShader);
                    fallbackMat.name = "GhostMaterial_Fallback";
                    
                    // Setup material for transparency (equivalent to Standard Shader transparent mode)
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

        _ghostInstance.transform.position = position;
        _ghostInstance.transform.localScale = sourcePlate.transform.localScale;
        _ghostInstance.SetActive(true);
    }

    /// <summary>
    /// Hides the ghost preview mesh.
    /// </summary>
    public void Hide()
    {
        if (_ghostInstance != null)
        {
            _ghostInstance.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_ghostInstance != null)
        {
            Destroy(_ghostInstance);
        }
    }
}
