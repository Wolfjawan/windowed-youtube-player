using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal sealed class CompactServicePanel : ScrollableControl
{
    private const int ItemWidth = 128;
    private const int ItemHeight = 96;
    private const int Gap = 20;
    private bool arranging;

    public CompactServicePanel()
    {
        AutoScroll = true;
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(8, 12, 8, 12);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        ArrangeItems();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ArrangeItems();
    }

    private void ArrangeItems()
    {
        if (arranging || ClientSize.Width <= 0) return;

        arranging = true;
        try
        {
            int scrollbar = VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            int available = Math.Max(ItemWidth, ClientSize.Width - Padding.Horizontal - scrollbar);
            int columns = Math.Clamp((available + Gap) / (ItemWidth + Gap), 1, Math.Max(1, Controls.Count));
            int rows = Controls.Count == 0 ? 0 : (int)Math.Ceiling(Controls.Count / (double)columns);
            int contentWidth = columns * ItemWidth + Math.Max(0, columns - 1) * Gap;
            int startX = Padding.Left + Math.Max(0, (available - contentWidth) / 2);

            for (int index = 0; index < Controls.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                Controls[index].Bounds = new Rectangle(
                    startX + column * (ItemWidth + Gap),
                    Padding.Top + row * (ItemHeight + Gap),
                    ItemWidth,
                    ItemHeight);
            }

            AutoScrollMinSize = new Size(
                0,
                Padding.Top + rows * ItemHeight + Math.Max(0, rows - 1) * Gap + Padding.Bottom);
        }
        finally
        {
            arranging = false;
        }
    }
}

internal sealed class CompactServiceButton : Control
{
    private readonly SiteChoice site;
    private readonly ServiceBrand brand;
    private bool hovered;
    private bool pressed;

    public CompactServiceButton(SiteChoice siteChoice)
    {
        site = siteChoice;
        brand = ServiceVisuals.For(siteChoice).Brand;
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        TabStop = true;
        BackColor = Color.Transparent;
        AccessibleName = $"Open {site.DisplayName}";
        AccessibleRole = AccessibleRole.PushButton;
        SetStyle(
            ControlStyles.Selectable
            | ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.SupportsTransparentBackColor,
            true);
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

        float scale = pressed ? 0.90f : hovered ? 1.06f : 1f;
        RectangleF iconArea = new(24, 17, Width - 48, Height - 34);
        float centerX = iconArea.X + iconArea.Width / 2f;
        float centerY = iconArea.Y + iconArea.Height / 2f;
        iconArea = new RectangleF(
            centerX - iconArea.Width * scale / 2f,
            centerY - iconArea.Height * scale / 2f,
            iconArea.Width * scale,
            iconArea.Height * scale);

        if (hovered || Focused)
        {
            using GraphicsPath glowPath = new();
            RectangleF glowArea = new(10, 8, Width - 20, Height - 16);
            glowPath.AddEllipse(glowArea);
            using PathGradientBrush glow = new(glowPath)
            {
                CenterColor = Color.FromArgb(45, 255, 255, 255),
                SurroundColors = [Color.FromArgb(0, 255, 255, 255)]
            };
            e.Graphics.FillPath(glow, glowPath);
        }

        DrawLogo(e.Graphics, iconArea);

        if (Focused)
        {
            Rectangle focus = ClientRectangle;
            focus.Inflate(-7, -7);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, Color.White, Color.Transparent);
        }
    }

    private void DrawLogo(Graphics graphics, RectangleF area)
    {
        switch (brand)
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
            case ServiceBrand.BbcIPlayer:
                DrawBbcIPlayer(graphics, area);
                break;
        }
    }

    private static void DrawYouTube(Graphics graphics, RectangleF area)
    {
        RectangleF mark = new(area.X + 2, area.Y + area.Height * 0.16f, area.Width - 4, area.Height * 0.68f);
        using GraphicsPath rounded = RoundedRectangle(mark, Math.Max(8, mark.Height * 0.22f));
        using SolidBrush red = new(Color.FromArgb(255, 0, 0));
        graphics.FillPath(red, rounded);

        PointF[] triangle =
        [
            new(mark.X + mark.Width * 0.43f, mark.Y + mark.Height * 0.25f),
            new(mark.X + mark.Width * 0.43f, mark.Y + mark.Height * 0.75f),
            new(mark.X + mark.Width * 0.70f, mark.Y + mark.Height * 0.50f)
        ];
        using SolidBrush white = new(Color.White);
        graphics.FillPolygon(white, triangle);
    }

    private static void DrawCrunchyroll(Graphics graphics, RectangleF area)
    {
        float size = Math.Min(area.Width, area.Height) * 0.92f;
        RectangleF outer = new(
            area.X + (area.Width - size) / 2f,
            area.Y + (area.Height - size) / 2f,
            size,
            size);
        using Pen orange = new(Color.FromArgb(244, 117, 33), Math.Max(6f, size * 0.11f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawArc(orange, outer, 25, 300);

        RectangleF eye = new(
            outer.X + outer.Width * 0.54f,
            outer.Y + outer.Height * 0.29f,
            outer.Width * 0.31f,
            outer.Height * 0.31f);
        using SolidBrush orangeBrush = new(Color.FromArgb(244, 117, 33));
        graphics.FillEllipse(orangeBrush, eye);
        using SolidBrush background = new(Color.FromArgb(14, 13, 27));
        float inset = Math.Max(4f, eye.Width * 0.28f);
        graphics.FillEllipse(background, new RectangleF(
            eye.X + inset,
            eye.Y + inset,
            Math.Max(3, eye.Width - inset * 2),
            Math.Max(3, eye.Height - inset * 2)));
    }

    private static void DrawPrimeVideo(Graphics graphics, RectangleF area)
    {
        using Font primeFont = new("Segoe UI", 15.5f, FontStyle.Bold, GraphicsUnit.Point);
        using SolidBrush white = new(Color.White);
        StringFormat format = new() { Alignment = StringAlignment.Center };
        RectangleF word = new(area.X - 8, area.Y + 4, area.Width + 16, 30);
        graphics.DrawString("prime", primeFont, white, word, format);

        using Pen smile = new(Color.FromArgb(0, 168, 225), 3f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.ArrowAnchor
        };
        graphics.DrawArc(
            smile,
            area.X + area.Width * 0.16f,
            area.Y + area.Height * 0.43f,
            area.Width * 0.68f,
            area.Height * 0.31f,
            12,
            155);
    }

    private static void DrawBbcIPlayer(Graphics graphics, RectangleF area)
    {
        float boxSize = Math.Min(18f, area.Width / 4.6f);
        float totalWidth = boxSize * 3 + 5 * 2;
        float startX = area.X + (area.Width - totalWidth) / 2f;
        float top = area.Y + 6;

        using Font bbcFont = new("Arial", 9f, FontStyle.Bold, GraphicsUnit.Point);
        using SolidBrush white = new(Color.White);
        using SolidBrush dark = new(Color.FromArgb(14, 13, 27));
        using StringFormat centered = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (int index = 0; index < 3; index++)
        {
            RectangleF box = new(startX + index * (boxSize + 5), top, boxSize, boxSize);
            graphics.FillRectangle(white, box);
            graphics.DrawString("BBC"[index].ToString(), bbcFont, dark, box, centered);
        }

        PointF[] play =
        [
            new(area.X + area.Width * 0.35f, area.Y + area.Height * 0.52f),
            new(area.X + area.Width * 0.35f, area.Y + area.Height * 0.91f),
            new(area.X + area.Width * 0.70f, area.Y + area.Height * 0.715f)
        ];
        using SolidBrush pink = new(Color.FromArgb(255, 76, 160));
        graphics.FillPolygon(pink, play);
    }

    private static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
    {
        float diameter = radius * 2f;
        GraphicsPath path = new();
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
