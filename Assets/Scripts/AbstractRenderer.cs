using System;
using Core.Events;
using Core.Variables;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AbstractRenderer : MonoBehaviour
{
    public InputActionReference toggleAction;
    public EventData renderToggledEvent;
    public BoolVariable renderEnabled;

    public MeshRenderer meshRenderer;
    public MeshFilter   meshFilter;
    
    protected event Action OnRenderToggled;

    protected void Start()
    {
        toggleAction?.action?.Enable();

        if (meshRenderer) meshRenderer.enabled = renderEnabled.Value;
        if (renderToggledEvent) renderToggledEvent?.Raise();

        MapGenerator.Instance.OnMapGenerated += BuildMesh;
    }

    protected void Update()
    {
        if (toggleAction?.action == null) return;
        if (!toggleAction.action.WasPressedThisFrame()) return;
        if (MapGenerator.IsGenerating) return;

        renderEnabled.Value = !renderEnabled.Value;
        meshRenderer.enabled = renderEnabled.Value;
        
        OnRenderToggled?.Invoke();
        if (renderToggledEvent) renderToggledEvent?.Raise();
        if (renderEnabled.Value) BuildMesh();
    }

    public abstract void BuildMesh();

    public bool ToggleRenderer()
    {
        if (!renderEnabled.Value || WorldGrid.Instance.Cells == null) return false;
        meshRenderer.enabled = true;
        return true;
    }

    private void OnEnable()  => toggleAction?.action?.Enable();
    private void OnDisable() => toggleAction?.action?.Disable();

    private void OnDestroy()
    {
        if (MapGenerator.HasInstance)
            MapGenerator.Instance.OnMapGenerated -= BuildMesh;
    }
}