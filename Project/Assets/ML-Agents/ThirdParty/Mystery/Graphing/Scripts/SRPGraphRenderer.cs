using UnityEngine;
using System.Collections;
using Mystery.Graphing;
using System.Collections.Generic;

namespace Mystery.Graphing
{
    public abstract class SRPGraphRenderer : IGraphConsoleRenderer
    {
        protected void OnEnable()
        {
#if UNITY_2018_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
#if UNITY_2019_1_OR_NEWER
                UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += endCameraRendering;
#else
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += OnCameraRender;
#endif
            else
#endif
            Camera.onPostRender += OnCameraRender;

            if (Material == null)
                Material = BuiltInGraphShader.GetLineMaterial();
        }

        protected void OnDisable()
        {
#if UNITY_2018_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
#if UNITY_2019_1_OR_NEWER
                UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= endCameraRendering;
#else
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= OnCameraRender;
#endif
            else
#endif
            Camera.onPostRender -= OnCameraRender;
        }

#if UNITY_2019_1_OR_NEWER
        void endCameraRendering(UnityEngine.Rendering.ScriptableRenderContext src, Camera camera)
        {
            if (CheckFilter(camera))
                Render(camera, false);
        }
#endif

        protected void OnCameraRender(Camera camera)
        {
            if (CheckFilter(camera))
                Render(camera, false);
        }

        bool CheckFilter(Camera camera)
        {
            return (camera.cullingMask & (1 << gameObject.layer)) != 0;
        }
    }
}