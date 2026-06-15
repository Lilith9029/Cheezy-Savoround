using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SkinMaterialMapping
{
    public string skinId;
    public Material material;
}

public class PlateSkin : MonoBehaviour
{
    [Header("Material Overrides")]
    [SerializeField] private List<SkinMaterialMapping> skinMaterials = new List<SkinMaterialMapping>();

    private MeshRenderer _plateRenderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _plateRenderer = GetComponentInChildren<MeshRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ApplyCurrentSkin();
        ShopManager.OnSkinEquipped += OnSkinChanged;
    }

    private void OnDestroy()
    {
        ShopManager.OnSkinEquipped -= OnSkinChanged;
    }

    private void OnSkinChanged(string skinId)
    {
        ApplySkin(skinId);
    }

    public void ApplyCurrentSkin()
    {
        string skinId = UserDataManager.Instance != null
            ? UserDataManager.Instance.EquippedSkin
            : "default";
        ApplySkin(skinId);
    }

    private void ApplySkin(string skinId)
    {
        if (_plateRenderer == null) return;

        // Ưu tiên dùng Material có sẵn nếu được kéo vào
        Material mat = GetMaterialForSkin(skinId);
        if (mat != null)
        {
            _plateRenderer.material = mat;
            return;
        }

        // Fallback: dùng colorHex từ ShopConfig
        if (ShopManager.Instance == null) return;
        SkinConfig config = ShopManager.Instance.GetSkinConfig(skinId);
        if (config == null) return;

        if (ColorUtility.TryParseHtmlString(config.colorHex, out Color color))
        {
            _plateRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color);
            _propBlock.SetFloat("_Metallic", config.metallic);
            _propBlock.SetFloat("_Glossiness", config.smoothness);
            _plateRenderer.SetPropertyBlock(_propBlock);
        }
    }

    private Material GetMaterialForSkin(string skinId)
    {
        if (skinMaterials == null) return null;
        foreach (var mapping in skinMaterials)
        {
            if (mapping.skinId == skinId) return mapping.material;
        }
        return null;
    }
}