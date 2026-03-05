using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AbstractRenderer : MonoBehaviour
{
    public InputActionReference toggleAction;

    public MeshRenderer meshRenderer;
    public MeshFilter   meshFilter;

    public bool enabledRender;

    protected void Start()
    {
        toggleAction?.action?.Enable();
        
        // Ensure component state matches our flag at start
        if (meshRenderer) meshRenderer.enabled = enabledRender;

        WorldGrid.Instance.OnMapGenerated += BuildMesh;
    }

    protected void Update()
    {
        if (toggleAction?.action == null) return;

        if (!toggleAction.action.WasPressedThisFrame()) return;

        enabledRender        = !enabledRender;
        meshRenderer.enabled = enabledRender;
        if (enabledRender) BuildMesh();
    }

    public abstract void BuildMesh();

    public bool ToggleRenderer()
    {
        if (!enabledRender || WorldGrid.Instance.Cells == null) return false;
        meshRenderer.enabled = true;
        return true;
    }

    private void OnEnable()  => toggleAction?.action?.Enable();
    private void OnDisable() => toggleAction?.action?.Disable();
}