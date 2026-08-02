using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal enum ServiceBrand
{
    YouTube,
    Crunchyroll,
    PrimeVideo,
    Netflix,
    DisneyPlus,
    BbcIPlayer,
    Custom
}

internal sealed record ServiceVisual(
    ServiceBrand Brand,
    Color StartColor,
    Color EndColor,
    Color AccentColor,
    string Subtitle);

internal static class ServiceVisuals
{
    public static ServiceVisual For(SiteChoice site)
    {
        string key = $"{site.DisplayName} {site.Url}".ToLowerInvariant();
        if (key.Contains("youtube"))
        {
            return new(ServiceBrand.YouTube, Color.FromArgb(224, 18, 30), Color.FromArgb(118, 4, 18), Color.White, "Videos, music and live streams");
        }

        if (key.Contains("crunchyroll"))
        {
            return new(ServiceBrand.Crunchyroll, Color.FromArgb(255, 123, 32), Color.FromArgb(183, 56, 8), Color.White, "Anime and Asian entertainment");
        }

        if (key.Contains("primevideo") || key.Contains("prime video"))
        {
            return new(ServiceBrand.PrimeVideo, Color.FromArgb(0, 168, 225), Color.FromArgb(0, 54, 92), Color.White, "Movies, series and live sport");
        }

        if (key.Contains("netflix"))
        {
            return new(ServiceBrand.Netflix, Color.FromArgb(35, 35, 39), Color.FromArgb(5, 5, 7), Color.FromArgb(229, 9, 20), "Series, films and originals");
        }

        if (key.Contains("disney"))
        {
            return new(ServiceBrand.DisneyPlus, Color.FromArgb(26, 76, 171), Color.FromArgb(5, 20, 72), Color.White, "Disney, Pixar, Marvel and Star");
        }

        if (key.Contains("iplayer") || key.Contains("bbc"))
        {
            return new(ServiceBrand.BbcIPlayer, Color.FromArgb(241, 73, 151), Color.FromArgb(83, 19, 91), Color.White, "BBC programmes and live channels");
        }

        return new(ServiceBrand.Custom, Color.FromArgb(108, 92, 231), Color.FromArgb(45, 38, 112), Color.White, "Open any secure website address");
    }
}

internal sealed class StreamingBackgroundPanel : Panel
{
    public StreamingBackgroundPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Rectangle bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using LinearGradientBrush background = new(
            bounds,
            Color.FromArgb(8, 12, 30),
            Color.FromArgb(30, 13, 49),
            LinearGradientMode.ForwardDiagonal);
        e.Graphics.FillRectangle(background, bounds);

        DrawGlow(e.Graphics, new RectangleF(bounds.Width * 0.58f, -80, 430, 430), Color.FromArgb(42, 225, 48, 108));
        DrawGlow(e.Graphics, new RectangleF(-150, bounds.Height * 0.45f, 460, 460), Color.FromArgb(34, 0, 168, 225));
        DrawGlow(e.Graphics, new RectangleF(bounds.Width * 0.62f, bounds.Height * 0.62f, 420, 420), Color.FromArgb(30, 117, 73, 255));

        using Pen linePen = new(Color.FromArgb(24, 255, 255, 255), 1.2f);
        for (int offset = -bounds.Height; offset < bounds.Width; offset += 78)
        {
            e.Graphics.DrawLine(linePen, offset, bounds.Height, offset + bounds.Height, 0);
        }

        using SolidBrush dotBrush = new(Color.FromArgb(35, 255, 255, 255));
        for (int y = 44; y < bounds.Height; y += 86)
        {
            for (int x = 32 + ((y / 86) % 2) * 34; x < bounds.Width; x += 104)
            {
                e.Graphics.FillEllipse(dotBrush, x, y, 3, 3);
            }
        }
    }

    private static void DrawGlow(Graphics graphics, RectangleF area, Color color)
    {
        using GraphicsPath path = new();
        path.AddEllipse(area);
        using PathGradientBrush glow = new(path)
        {
            CenterColor = color,
            SurroundColors = [Color.FromArgb(0, color)]
        };
        graphics.FillPath(glow, path);
    }
}

internal sealed class ResponsiveServicePanel : ScrollableControl
{
    private const int CardHeight = 142;
    private const int Gap = 16;
    private const int MinimumCardWidth = 215;
    private bool arranging;

    public ResponsiveServicePanel()
    {
        AutoScroll = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        Padding = new Padding(2, 4, 2, 12);
        BackColor = Color.Transparent;
        ResizeRedraw = true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        ArrangeCards();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ArrangeCards();
    }

    private void ArrangeCards()
    {
        if (arranging || ClientSize.Width <= 0)
        {
            return;
        }

        arranging = true;
        try
        {
            int scrollbar = VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            int availableWidth = Math.Max(180, ClientSize.Width - Padding.Horizontal - scrollbar);
            int columns = Math.Clamp((availableWidth + Gap) / (MinimumCardWidth + Gap), 1, 3);
            int cardWidth = Math.Max(170, (availableWidth - ((columns - 1) * Gap)) / columns);

            SuspendLayout();
            for (int index = 0; index < Controls.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                Controls[index].Bounds = new Rectangle(
                    Padding.Left + column * (cardWidth + Gap),
                    Padding.Top + row * (CardHeight + Gap),
                    cardWidth,
                    CardHeight);
            }

            int rows = Controls.Count == 0 ? 0 : (int)Math.Ceiling(Controls.Count / (double)columns);
            AutoScrollMinSize = new Size(
                0,
                Padding.Top + rows * CardHeight + Math.Max(0, rows - 1) * Gap + Padding.Bottom);
            ResumeLayout(false);
        }
        finally
        {
            arranging = false;
        }
    }
}

internal sealed class ServiceCard : Control
{
    private readonly SiteChoice site;
    private readonly ServiceVisual visual;
    private bool hovered;
    private bool pressed;

    public ServiceCard(SiteChoice siteChoice)
    {
        site = siteChoice;
        visual = ServiceVisuals.For(siteChoice);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleName = $"Open {site.DisplayName}";
        AccessibleDescription = visual.Subtitle;
        SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            pressed = true;
            Focus();
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        RectangleF card = new(7, 7, Width - 15, Height - 16);
        if (pressed)
        {
            card.Y += 2;
        }

        using GraphicsPath cardPath = RoundedRectangle(card, 18);
        using SolidBrush shadow = new(Color.FromArgb(70, 0, 0, 0));
        using GraphicsPath shadowPath = RoundedRectangle(new RectangleF(card.X + 1, card.Y + 6, card.Width, card.Height), 18);
        e.Graphics.FillPath(shadow, shadowPath);

        using LinearGradientBrush fill = new(card, visual.StartColor, visual.EndColor, 18f);
        e.Graphics.FillPath(fill, cardPath);

        using LinearGradientBrush shine = new(
            new RectangleF(card.X, card.Y, card.Width, card.Height * 0.52f),
            Color.FromArgb(85, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            LinearGradientMode.Vertical);
        e.Graphics.FillPath(shine, cardPath);

        if (hovered)
        {
            using Pen hoverPen = new(Color.FromArgb(205, 255, 255, 255), 2.2f);
            e.Graphics.DrawPath(hoverPen, cardPath);
        }
        else
        {
            using Pen borderPen = new(Color.FromArgb(55, 255, 255, 255), 1f);
            e.Graphics.DrawPath(borderPen, cardPath);
        }

        RectangleF logoArea = new(card.X + 20, card.Y + 18, Math.Min(82, card.Width * 0.30f), 58);
        DrawLogo(e.Graphics, logoArea);

        float textLeft = logoArea.Right + 18;
        float textWidth = Math.Max(80, card.Right - textLeft - 18);
        using Font titleFont = new(SystemFonts.MessageBoxFont.FontFamily, 13.5f, FontStyle.Bold, GraphicsUnit.Point);
        using Font subtitleFont = new(SystemFonts.MessageBoxFont.FontFamily, 8.8f, FontStyle.Regular, GraphicsUnit.Point);
        using SolidBrush white = new(Color.White);
        using SolidBrush secondary = new(Color.FromArgb(215, 255, 255, 255));

        e.Graphics.DrawString(site.DisplayName, titleFont, white, new RectangleF(textLeft, card.Y + 27, textWidth, 29));
        e.Graphics.DrawString(visual.Subtitle, subtitleFont, secondary, new RectangleF(textLeft, card.Y + 60, textWidth, 40));

        using Font actionFont = new(SystemFonts.MessageBoxFont.FontFamily, 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        e.Graphics.DrawString("OPEN  →", actionFont, secondary, new PointF(card.X + 20, card.Bottom - 31));

        if (Focused)
        {
            Rectangle focus = Rectangle.Round(card);
            focus.Inflate(-4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, Color.White, Color.Transparent);
        }
    }

    private void DrawLogo(Graphics graphics, RectangleF area)
    {
        switch (visual.Brand)
        {
            case ServiceBrand.YouTube:
                DrawYouTube(graphics, area);
                break;
            case ServiceBrand.Crunchyroll:
                DrawCrunchyroll(graphics, area);
                break;
            case ServiceBrand.PrimeVideo:
                DrawPrimeVideo(graphics, area);
                break;
            case ServiceBrand.Netflix:
                DrawNetflix(graphics, area);
                break;
            case ServiceBrand.DisneyPlus:
                DrawDisneyPlus(graphics, area);
                break;
            case ServiceBrand.BbcIPlayer:
                DrawBbcIPlayer(graphics, area);
                break;
            default:
                DrawGlobe(graphics, area);
                break;
        }
    }

    private static void DrawYouTube(Graphics graphics, RectangleF area)
    {
        RectangleF mark = new(area.X, area.Y + 5, area.Width * 0.88f, area.Height * 0.72f);
        using GraphicsPath rounded = RoundedRectangle(mark, 12);
        using SolidBrush white = new(Color.White);
        graphics.FillPath(white, rounded);
        PointF[] triangle =
        [
            new(mark.X + mark.Width * 0.43f, mark.Y + mark.Height * 0.27f),
            new(mark.X + mark.Width * 0.43f, mark.Y + mark.Height * 0.73f),
            new(mark.X + mark.Width * 0.70f, mark.Y + mark.Height * 0.50f)
        ];
        using SolidBrush red = new(Color.FromArgb(220, 20, 35));
        graphics.FillPolygon(red, triangle);
    }

    private static void DrawCrunchyroll(Graphics graphics, RectangleF area)
    {
        float size = Math.Min(area.Width, area.Height);
        RectangleF outer = new(area.X + 4, area.Y + (area.Height - size) / 2, size, size);
        using Pen whitePen = new(Color.White, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawArc(whitePen, outer, 25, 300);
        RectangleF eye = new(outer.X + outer.Width * 0.52f, outer.Y + outer.Height * 0.28f, outer.Width * 0.34f, outer.Height * 0.34f);
        using SolidBrush white = new(Color.White);
        graphics.FillEllipse(white, eye);
        using SolidBrush orange = new(Color.FromArgb(244, 117, 33));
        graphics.FillEllipse(orange, new RectangleF(eye.X + 7, eye.Y + 7, Math.Max(4, eye.Width - 14), Math.Max(4, eye.Height - 14)));
    }

    private static void DrawPrimeVideo(Graphics graphics, RectangleF area)
    {
        using Font primeFont = new("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        using Font videoFont = new("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        using SolidBrush white = new(Color.White);
        graphics.DrawString("prime", primeFont, white, area.X, area.Y - 1);
        graphics.DrawString("video", videoFont, white, area.X + 8, area.Y + 28);
        using Pen smile = new(Color.White, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor };
        graphics.DrawArc(smile, area.X + 4, area.Y + 30, area.Width * 0.72f, 25, 15, 145);
    }

    private static void DrawNetflix(Graphics graphics, RectangleF area)
    {
        using Font font = new("Arial Black", 39f, FontStyle.Bold, GraphicsUnit.Point);
        using SolidBrush red = new(Color.FromArgb(229, 9, 20));
        StringFormat format = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("N", font, red, area, format);
    }

    private static void DrawDisneyPlus(Graphics graphics, RectangleF area)
    {
        using Font font = new("Segoe Script", 14f, FontStyle.Bold, GraphicsUnit.Point);
        using SolidBrush white = new(Color.White);
        graphics.DrawString("Disney+", font, white, area.X - 2, area.Y + 17);
        using Pen arc = new(Color.FromArgb(220, 255, 255, 255), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawArc(arc, area.X + 3, area.Y, area.Width * 0.92f, 35, 200, 130);
    }

    private static void DrawBbcIPlayer(Graphics graphics, RectangleF area)
    {
        float box = 18;
        using SolidBrush white = new(Color.White);
        using SolidBrush dark = new(Color.FromArgb(58, 14, 70));
        using Font bbcFont = new("Arial", 10f, FontStyle.Bold, GraphicsUnit.Point);
        for (int index = 0; index < 3; index++)
        {
            RectangleF square = new(area.X + index * (box + 3), area.Y + 5, box, box);
            graphics.FillRectangle(white, square);
            StringFormat format = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString("BBC"[index].ToString(), bbcFont, dark, square, format);
        }

        using Font playerFont = new("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
        graphics.DrawString("iPLAYER", playerFont, white, area.X, area.Y + 31);
    }

    private static void DrawGlobe(Graphics graphics, RectangleF area)
    {
        float size = Math.Min(area.Width, area.Height) - 6;
        RectangleF globe = new(area.X + 3, area.Y + (area.Height - size) / 2, size, size);
        using Pen pen = new(Color.White, 2.2f);
        graphics.DrawEllipse(pen, globe);
        graphics.DrawEllipse(pen, new RectangleF(globe.X + globe.Width * 0.28f, globe.Y, globe.Width * 0.44f, globe.Height));
        graphics.DrawLine(pen, globe.Left, globe.Top + globe.Height * 0.50f, globe.Right, globe.Top + globe.Height * 0.50f);
        graphics.DrawArc(pen, globe.X, globe.Y + globe.Height * 0.18f, globe.Width, globe.Height * 0.34f, 0, 180);
        graphics.DrawArc(pen, globe.X, globe.Y + globe.Height * 0.48f, globe.Width, globe.Height * 0.34f, 180, 180);
    }

    internal static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
    {
        float diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
        GraphicsPath path = new();
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class DarkMenuColorTable : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => Color.FromArgb(10, 13, 28);
    public override Color MenuStripGradientEnd => Color.FromArgb(10, 13, 28);
    public override Color ToolStripDropDownBackground => Color.FromArgb(24, 27, 43);
    public override Color ImageMarginGradientBegin => Color.FromArgb(24, 27, 43);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(24, 27, 43);
    public override Color ImageMarginGradientEnd => Color.FromArgb(24, 27, 43);
    public override Color MenuItemSelected => Color.FromArgb(59, 50, 92);
    public override Color MenuItemBorder => Color.FromArgb(108, 92, 231);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(59, 50, 92);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(59, 50, 92);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(49, 42, 77);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(49, 42, 77);
    public override Color SeparatorDark => Color.FromArgb(70, 73, 92);
    public override Color SeparatorLight => Color.FromArgb(40, 43, 60);
}
