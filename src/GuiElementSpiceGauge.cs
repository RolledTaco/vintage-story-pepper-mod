using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace PepperMod
{
    public sealed class GuiElementSpiceGauge : GuiElement
    {
        public const int Width = 200;
        public const int Height = 154;
        public const int PixelSize = 2;
        private const double CenterX = 100, CenterY = 96;
        private static readonly double[][] Colors = {
            new[] { .47, .61, .32 }, new[] { .84, .61, .28 }, new[] { .76, .30, .23 }
        };
        private LoadedTexture texture;
        private SpiceState state;
        private bool composed;
        public override bool Focusable => false;

        public GuiElementSpiceGauge(ICoreClientAPI api, ElementBounds bounds) : base(api, bounds)
        {
            texture = new LoadedTexture(api);
        }

        public void SetState(SpiceState value)
        {
            bool changed = state.Heat != value.Heat;
            state = value;
            if (composed && changed && state.Level != SpiceLevel.None) Redraw();
        }

        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();
            composed = true;
            Redraw();
        }

        private void Redraw()
        {
            string level = Lang.Get("peppermod:spice-" + state.Level.ToString().ToLowerInvariant());
            using var surface = CreateDial(state, Lang.Get("peppermod:spice-gauge", level), GuiStyle.StandardFontName);
            generateTexture(surface, ref texture, linearMag: false);
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            if (state.Level != SpiceLevel.None && texture.TextureId != 0)
                api.Render.Render2DTexture(texture.TextureId, Bounds);
        }

        public override void Dispose()
        {
            composed = false;
            texture.Dispose();
            base.Dispose();
        }

        public static double NeedleAngle(float heat) => Math.PI * (150 + 240 * new SpiceState(heat).Heat / SpiceState.MaximumHeat) / 180;

        // The same native Cairo drawing is used by the HUD and the raster preview tests.
        public static void DrawDial(Context ctx, SpiceState value, string label, string fontName)
        {
            using var surface = CreateDial(value, label, fontName);
            using var pattern = new SurfacePattern(surface) { Filter = Filter.Nearest };
            ctx.Save();
            ctx.NewPath();
            ctx.Scale(PixelSize, PixelSize);
            ctx.SetSource(pattern);
            ctx.Rectangle(0, 0, surface.Width, surface.Height);
            ctx.Fill();
            ctx.Restore();
        }

        private static ImageSurface CreateDial(SpiceState value, string label, string fontName)
        {
            var surface = new ImageSurface(Format.Argb32, Width / PixelSize, Height / PixelSize);
            try
            {
                using var ctx = new Context(surface);
                ctx.Scale(1d / PixelSize, 1d / PixelSize);
                DrawPixelDial(ctx, value, label, fontName);
                surface.Flush();
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static void DrawPixelDial(Context ctx, SpiceState value, string label, string fontName)
        {
            ctx.Save();
            ctx.NewPath();
            ctx.Antialias = Antialias.None;
            ctx.LineJoin = LineJoin.Miter;
            ctx.Arc(CenterX, CenterY + 4, 92, NeedleAngle(0), NeedleAngle(100));
            ctx.LineTo(178, 152); ctx.LineTo(22, 152);
            ctx.ClosePath();
            ctx.SetSourceRGBA(.035, .045, .035, .7); ctx.Fill();
            ctx.Arc(CenterX, CenterY, 90, NeedleAngle(0), NeedleAngle(100));
            ctx.LineTo(176, 148); ctx.LineTo(24, 148);
            ctx.ClosePath();
            ctx.SetSourceRGB(.36, .38, .33); ctx.FillPreserve();
            ctx.SetSourceRGB(.15, .18, .15); ctx.LineWidth = 2; ctx.Stroke();
            Arc(ctx, 88, 2, 98, 2, .55, .56, .46);
            ctx.Arc(CenterX, CenterY, 84, NeedleAngle(0), NeedleAngle(100));
            ctx.LineTo(170, 144); ctx.LineTo(30, 144);
            ctx.ClosePath();
            ctx.SetSourceRGB(.13, .16, .14); ctx.FillPreserve();
            ctx.Save(); ctx.Clip();
            // Sparse, deterministic wear keeps the face still while the needle moves.
            for (int y = 18; y < 140; y += 6)
            for (int x = 16; x < 188; x += 6)
            {
                int grain = (x * 17 + y * 31 + x * y) % 29;
                if (grain > 2) continue;
                ctx.SetSourceRGB(grain == 0 ? .18 : .105, grain == 0 ? .20 : .13, grain == 0 ? .16 : .11);
                ctx.Rectangle(x, y, grain == 0 ? 4 : 2, 2); ctx.Fill();
            }
            ctx.Restore();

            for (int heat = 3; heat < 100; heat += 7)
            {
                double angle = NeedleAngle(heat);
                Pixel(ctx, CenterX + Math.Cos(angle) * 87, CenterY + Math.Sin(angle) * 87, 2, 2, .24, .28, .23);
            }
            Rivet(ctx, 14, 96); Rivet(ctx, 100, 10); Rivet(ctx, 186, 96);

            float[] limits = { 0, SpiceState.HotThreshold, SpiceState.ExtremeThreshold, SpiceState.MaximumHeat };
            for (int i = 0; i < 3; i++)
            {
                double[] c = Colors[i];
                Arc(ctx, 76, limits[i] + 1, limits[i + 1] - 1, 10, c[0], c[1], c[2]);
            }
            for (int heat = 4; heat < 100; heat += 9)
            {
                double angle = NeedleAngle(heat);
                double radius = heat % 2 == 0 ? 73 : 78;
                Pixel(ctx, CenterX + Math.Cos(angle) * radius, CenterY + Math.Sin(angle) * radius, 2, 2, .25, .29, .20);
            }
            for (int heat = 0; heat <= 100; heat += 10)
            {
                double angle = NeedleAngle(heat);
                bool major = heat % 50 == 0;
                Line(ctx, angle, major ? 60 : 64, 68, major ? 3 : 2, .77, .75, .59);
            }
            foreach (float threshold in new[] { SpiceState.HotThreshold, SpiceState.ExtremeThreshold })
                Line(ctx, NeedleAngle(threshold), 71, 81, 2, .82, .79, .61);

            double needle = NeedleAngle(value.Heat);
            ctx.Save();
            ctx.Translate(CenterX, CenterY);
            ctx.Rotate(needle);
            ctx.MoveTo(66, 0); ctx.LineTo(48, -2); ctx.LineTo(-4, -4);
            ctx.LineTo(-4, 4); ctx.LineTo(48, 2); ctx.ClosePath();
            ctx.SetSourceRGB(.86, .80, .61); ctx.Fill();
            ctx.Restore();
            Pixel(ctx, CenterX - 6, CenterY - 6, 12, 12, .09, .12, .10);
            Pixel(ctx, CenterX - 4, CenterY - 4, 8, 8, .47, .43, .30);
            Pixel(ctx, CenterX - 4, CenterY - 4, 6, 2, .76, .70, .49);
            Pixel(ctx, CenterX - 2, CenterY, 4, 2, .20, .23, .18);

            using var fontOptions = new FontOptions { Antialias = Antialias.None, HintStyle = HintStyle.Full, HintMetrics = HintMetrics.On };
            ctx.FontOptions = fontOptions;
            ctx.SelectFontFace(fontName, FontSlant.Normal, FontWeight.Bold);
            double[] color = Colors[Math.Clamp((int)value.Level - 1, 0, 2)];
            Text(ctx, label.ToUpperInvariant(), CenterX, 142, 18, 122, color[0], color[1], color[2]);
            ctx.Restore();
        }

        private static void Pixel(Context ctx, double x, double y, double width, double height, double r, double g, double b)
        {
            ctx.SetSourceRGB(r, g, b);
            ctx.Rectangle(Math.Round(x / PixelSize) * PixelSize, Math.Round(y / PixelSize) * PixelSize, width, height);
            ctx.Fill();
        }

        private static void Rivet(Context ctx, double x, double y)
        {
            Pixel(ctx, x - 2, y - 2, 6, 6, .16, .20, .16);
            Pixel(ctx, x - 2, y - 2, 4, 2, .67, .65, .49);
            Pixel(ctx, x, y, 2, 2, .43, .46, .36);
        }

        private static void Arc(Context ctx, double radius, float start, float end, double width, double r, double g, double b)
        {
            ctx.NewPath(); ctx.Arc(CenterX, CenterY, radius, NeedleAngle(start), NeedleAngle(end));
            ctx.SetSourceRGB(r, g, b); ctx.LineWidth = width; ctx.Stroke();
        }

        private static void Line(Context ctx, double angle, double inner, double outer, double width, double r, double g, double b)
        {
            ctx.MoveTo(CenterX + Math.Cos(angle) * inner, CenterY + Math.Sin(angle) * inner);
            ctx.LineTo(CenterX + Math.Cos(angle) * outer, CenterY + Math.Sin(angle) * outer);
            ctx.SetSourceRGB(r, g, b); ctx.LineWidth = width; ctx.Stroke();
        }

        private static void Text(Context ctx, string text, double x, double baseline, double size, double maxWidth, double r, double g, double b)
        {
            ctx.SetFontSize(size);
            var extents = ctx.TextExtents(text);
            // Hinted glyph widths jump at small pixel sizes, so measure again after fitting.
            for (int i = 0; i < 12 && extents.Width > maxWidth; i++)
            {
                size *= maxWidth / extents.Width * .95;
                ctx.SetFontSize(size);
                extents = ctx.TextExtents(text);
            }
            ctx.MoveTo(Math.Round((x - extents.Width / 2 - extents.XBearing) / PixelSize) * PixelSize, baseline);
            ctx.SetSourceRGB(r, g, b); ctx.ShowText(text);
        }
    }
}
