using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PepperMod
{
    public class SpiceHud : HudElement
    {
        private SpiceState state;
        private GuiElementSpiceGauge gauge;
        private LoadedTexture vignetteTexture;
        private float redOpacity;
        private float pulseTime;
        private readonly Vec4f tint = new Vec4f(1, 1, 1, 0);
        public override bool Focusable => false;
        public override bool ShouldReceiveMouseEvents() => false;
        public override bool ShouldReceiveKeyboardEvents() => false;

        public SpiceHud(ICoreClientAPI api) : base(api) { }

        public void Update(SpiceState value)
        {
            state = value;
            if (state.Level == SpiceLevel.None)
            {
                redOpacity = 0;
                pulseTime = 0;
                if (IsOpened()) TryClose();
                return;
            }
            if (SingleComposer == null) ComposeMeter();
            gauge.SetState(state);
            if (!IsOpened()) TryOpen();
        }

        private void ComposeMeter()
        {
            var bounds = ElementBounds.Fixed(0, 0, GuiElementSpiceGauge.Width, GuiElementSpiceGauge.Height)
                .WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-24, -145);
            SingleComposer = capi.Gui.CreateCompo("peppermod-spice", bounds);
            gauge = new GuiElementSpiceGauge(capi, ElementBounds.Fixed(0, 0, GuiElementSpiceGauge.Width, GuiElementSpiceGauge.Height));
            gauge.SetState(state);
            SingleComposer.AddInteractiveElement(gauge, "spice-gauge").Compose(false);
        }

        public override void OnRenderGUI(float dt)
        {
            if (state.Level == SpiceLevel.None || capi.World.Player?.Entity?.Alive != true
                || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator) return;

            float frameTime = capi.IsGamePaused || !float.IsFinite(dt) ? 0 : Math.Clamp(dt, 0, .1f);
            pulseTime = (pulseTime + frameTime) % 4;
            float target = state.RedIntensity * (.28f + .04f * MathF.Sin(pulseTime * MathF.PI / 2));
            redOpacity += (target - redOpacity) * Math.Min(1, frameTime * 3);
            if (redOpacity > .001f)
            {
                if (vignetteTexture == null) CreateVignette();
                tint.A = redOpacity;
                RenderVignette();
            }
            base.OnRenderGUI(dt);
        }

        private void CreateVignette()
        {
            const int size = 256;
            int[] rgba = new int[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2 - 1;
                float dy = (y + .5f) / size * 2 - 1;
                float edge = Math.Clamp((MathF.Sqrt(dx * dx + dy * dy) - .5f) / .65f, 0, 1);
                int alpha = (int)(255 * edge * edge * (3 - 2 * edge));
                rgba[y * size + x] = (alpha << 24) | (12 << 16) | (16 << 8) | 190;
            }
            vignetteTexture = new LoadedTexture(capi, 0, size, size);
            capi.Render.LoadOrUpdateTextureFromRgba(rgba, true, 0, ref vignetteTexture);
        }

        private void RenderVignette()
        {
            // Transparent overlay pixels must not occlude the HUD drawn afterward.
            capi.Render.GLDepthMask(false);
            try
            {
                capi.Render.Render2DTexture(vignetteTexture.TextureId, 0, 0, capi.Render.FrameWidth, capi.Render.FrameHeight, 50, tint);
            }
            finally
            {
                capi.Render.GLDepthMask(true);
            }
        }

        public override void Dispose()
        {
            if (IsOpened()) TryClose();
            vignetteTexture?.Dispose();
            vignetteTexture = null;
            gauge = null;
            base.Dispose();
        }
    }
}
