using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrystalSprint
{
    // Camera-scoped visibility, including Scene View/reflections. No world body objects are disabled.
    [ExecuteAlways]
    public sealed class FirstPersonBodyVisibility : MonoBehaviour
    {
        [SerializeField] private FirstPersonCamera firstPerson;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera armsCamera;
        [SerializeField] private Transform worldVisual;
        [SerializeField] private Transform viewmodelVisual;
        private Renderer[] worldRenderers, armsRenderers;
        private ShadowCastingMode[] originalShadowModes;
        private bool[] originalForceOff;
        private readonly Stack<(bool bodyHidden, bool armsHidden)> stateStack = new();
        private bool bodyHidden, armsHidden;

        public void Configure(FirstPersonCamera look, Camera main, Camera overlay, Transform body, Transform arms)
        {
            firstPerson = look; worldCamera = main; armsCamera = overlay; worldVisual = body; viewmodelVisual = arms;
            CacheRenderers();
        }

        private void OnEnable()
        {
            CacheRenderers();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            Apply(false, false); stateStack.Clear();
        }

        private void CacheRenderers()
        {
            if (worldVisual == null || viewmodelVisual == null) return;
            worldRenderers = worldVisual.GetComponentsInChildren<Renderer>(true);
            armsRenderers = viewmodelVisual.GetComponentsInChildren<Renderer>(true);
            originalShadowModes = new ShadowCastingMode[worldRenderers.Length];
            originalForceOff = new bool[armsRenderers.Length];
            for (int i = 0; i < worldRenderers.Length; i++) originalShadowModes[i] = worldRenderers[i].shadowCastingMode;
            for (int i = 0; i < armsRenderers.Length; i++) originalForceOff[i] = armsRenderers[i].forceRenderingOff;
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            stateStack.Push((bodyHidden, armsHidden));
            bool active = firstPerson != null && firstPerson.isActiveAndEnabled;
            Apply(active && camera == worldCamera, !active || camera != armsCamera);
        }

        private void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (stateStack.Count == 0) { Apply(false, false); return; }
            var previous = stateStack.Pop();
            Apply(previous.bodyHidden, previous.armsHidden);
        }

        private void Apply(bool hideBody, bool hideArms)
        {
            if (worldRenderers == null || armsRenderers == null) return;
            for (int i = 0; i < worldRenderers.Length; i++)
                if (worldRenderers[i] != null) worldRenderers[i].shadowCastingMode = hideBody ? ShadowCastingMode.ShadowsOnly : originalShadowModes[i];
            for (int i = 0; i < armsRenderers.Length; i++)
                if (armsRenderers[i] != null) armsRenderers[i].forceRenderingOff = hideArms || originalForceOff[i];
            bodyHidden = hideBody; armsHidden = hideArms;
        }
    }
}
