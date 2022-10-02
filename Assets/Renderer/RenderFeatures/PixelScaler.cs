using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelScaler : ScriptableRendererFeature
{
    public class PixelScalerPass : ScriptableRenderPass
    {
        private PixelScalerSettings _settings;
        private const string _passName = "PixelScalerPass";

        private RenderTargetHandle _preScaleTexture;
        public int IntegerScale { get; set; } = 1;

        public PixelScalerPass(PixelScalerSettings settings)
        {
            renderPassEvent = settings.Event;
            _settings = settings;
            _preScaleTexture.Init("_SharpBilinearPass");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            ConfigureInput(ScriptableRenderPassInput.Normal);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
                return;

            if (renderingData.cameraData.targetTexture)
                return;

            var renderer = renderingData.cameraData.renderer;
            var cmd = CommandBufferPool.Get(_passName);

            switch (_settings.scaleMode)
            {
            case ScaleMode.SharpBilinear:
            {
                var preScaleTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
                preScaleTextureDesc.width = _settings.TargetWidth * IntegerScale;
                preScaleTextureDesc.height = _settings.TargetHeight * IntegerScale;
                cmd.GetTemporaryRT(_preScaleTexture.id, preScaleTextureDesc, FilterMode.Bilinear);
                Blit(cmd, new RenderTargetIdentifier(_settings.targetTexture), _preScaleTexture.Identifier(), _settings.integerScaleMaterial);
                Blit(cmd, _preScaleTexture.Identifier(), renderer.cameraColorTarget, _settings.sharpBilinearMaterial);
                break;
            }
            case ScaleMode.SimpleStretch:
            {
                var preScaleTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
                preScaleTextureDesc.width = _settings.TargetWidth * IntegerScale;
                preScaleTextureDesc.height = _settings.TargetHeight * IntegerScale;
                cmd.GetTemporaryRT(_preScaleTexture.id, preScaleTextureDesc, FilterMode.Point);
                Blit(cmd, new RenderTargetIdentifier(_settings.targetTexture), _preScaleTexture.Identifier(), _settings.integerScaleMaterial);
                Blit(cmd, _preScaleTexture.Identifier(), renderer.cameraColorTarget, _settings.sharpBilinearMaterial);
                break;
            }
            case ScaleMode.Integer:
                Blit(cmd, new RenderTargetIdentifier(_settings.targetTexture), renderer.cameraColorTarget, _settings.integerScaleMaterial);
                break;
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
            cmd.ReleaseTemporaryRT(_preScaleTexture.id);
        }
    }

    [System.Serializable]
    public class PixelScalerSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        public Material integerScaleMaterial = null;
        public Material sharpBilinearMaterial = null;
        public RenderTexture targetTexture;

        public ScaleMode scaleMode = ScaleMode.Integer;
        [Tooltip("Trim 1 pixel from each side of your target render texture. Set your render texture to have extra two pixels on the width and height. Useful when you need to use a shader that samples depth and normal texture offscreen.")]
        public bool trimPaddingPixels = true;

        public int TargetHeight => trimPaddingPixels ? targetTexture.height - 2 : targetTexture.height;
        public int TargetWidth => trimPaddingPixels ? targetTexture.width - 2 : targetTexture.width;

        public int RawHeight => targetTexture.height;
        public int RawWidth => targetTexture.width;
    }

    public PixelScalerSettings settings = new PixelScalerSettings();
    private static int s_renderSize = Shader.PropertyToID("_PixelRenderSize");
    private static int s_scale = Shader.PropertyToID("_PixelScale");
    private PixelScalerPass _pass;

    public enum ScaleMode
    {
        Integer,
        SharpBilinear,
        SimpleStretch,
    }

    public override void Create()
    {
        _pass = new PixelScalerPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.integerScaleMaterial == null || settings.sharpBilinearMaterial == null) return;
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (renderingData.cameraData.targetTexture)
            return;
        if (settings.targetTexture == null)
            return;
        var width = settings.TargetWidth;
        var height = settings.TargetHeight;
        var rawWidth = settings.RawWidth;
        var rawHeight = settings.RawHeight;
        var screenAspect = Screen.width / (float)Screen.height;

        var integerScale = Mathf.Max(Mathf.Min(Screen.width / width, Screen.height / height), 1);
        var fillScale = (width / (float)height < Screen.width / (float)Screen.height) ?
            new Vector2(screenAspect / width * height, 1) :
            new Vector2(1, width / (float)height / screenAspect);
        _pass.IntegerScale = integerScale;

        Shader.SetGlobalVector(s_renderSize, new Vector4(width, height, rawWidth, rawHeight));
        Shader.SetGlobalVector(s_scale, new Vector4(fillScale.x, fillScale.y, integerScale, settings.scaleMode == ScaleMode.Integer ? 0 : 1));
        renderer.EnqueuePass(_pass);
    }
}
