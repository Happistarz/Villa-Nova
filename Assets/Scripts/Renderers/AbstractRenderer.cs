using System;
using Core.Events;
using Core.Variables;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for mesh renderers. Handles toggle input, visibility state and mesh rebuild triggers.
/// </summary>
public abstract class AbstractRenderer : MonoBehaviour
{
    public InputActionReference toggleAction;
    public EventData            renderToggledEvent;
    public BoolVariable         renderEnabled;

    public MeshRenderer meshRenderer;
    public MeshFilter   meshFilter;

    protected event Action OnRenderToggled;

    protected void Start()
    {
        toggleAction?.action?.Enable();

        if (meshRenderer) meshRenderer.enabled = renderEnabled.Value;
        if (renderToggledEvent) renderToggledEvent?.Raise();

        MapGenerator.Instance.OnGenerationComplete += BuildMesh;
        GenerationPipeline.Instance.OnPipelineComplete += BuildMesh;
    }

    protected void Update()
    {
        if (toggleAction?.action == null) return;
        if (!toggleAction.action.WasPressedThisFrame()) return;

        ToggleVisibility();
    }

    public void ToggleVisibility()
    {
        if (GenerationPipeline.Instance.IsAnyGenerating) return;

        renderEnabled.Value  = !renderEnabled.Value;
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
            MapGenerator.Instance.OnGenerationComplete -= BuildMesh;

        if (GenerationPipeline.HasInstance)
            GenerationPipeline.Instance.OnPipelineComplete -= BuildMesh;
    }
}