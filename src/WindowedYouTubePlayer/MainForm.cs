using System.Drawing;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal sealed class MainForm : Form
{
    private const string AppDisplayName = "Windowed Streaming Player";

    private static readonly HashSet<string> HomeServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "YouTube",
        "Crunchyroll",
        "Prime Video",
        "BBC iPlayer"
    };

    private readonly BrowserSessionManager sessions;
    private AppSettings settings;

    public MainForm(AppSettings initialSettings, BrowserSessionManager sessionManager)
    {
        settings = initialSettings;
        sessions = sessionManager;

        Text = AppDisplayName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(540, 350);
        ClientSize = new Size(760, 430);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = AppIcon.TryLoad();
        BackColor = Color.FromArgb(8, 12, 30);

        MainMenuStrip = BuildMenu();
        Controls.Add(BuildContent());
        Controls.Add(MainMenuStrip);
        MainMenuStrip.BringToFront();
    }

    private MenuStrip BuildMenu()
    {
        MenuStrip menu = new()
        {
            Dock = DockStyle.Top,
            Renderer = new ToolStripProfessionalRenderer(new DarkMenuColorTable()),
            BackColor = Color.FromArgb(10, 13, 28),
            ForeColor = Color.White,
            Padding = new Padding(8, 3, 0, 3)
        };

        ToolStripMenuItem fileMenu = MenuItem("&File");
        ToolStripMenuItem newWindowItem = MenuItem("&New Window…");
        newWindowItem.ShortcutKeys = Keys.Control | Keys.N;
        newWindowItem.Click += async (_, _) => await ChooseAndOpenSiteAsync();

        ToolStripMenuItem openPreferredItem = MenuItem("Open &Preferred Website");
        openPreferredItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
        openPreferredItem.Click += async (_, _) => await OpenPreferredSiteAsync();

        ToolStripMenuItem exitItem = MenuItem("E&xit");
        exitItem.Click += (_, _) => Close();
        fileMenu.DropDownItems.AddRange([newWindowItem, openPreferredItem, new ToolStripSeparator(), exitItem]);

        ToolStripMenuItem editMenu = MenuItem("&Edit");
        ToolStripMenuItem changeBrowserItem = MenuItem("Change &Browser…");
        changeBrowserItem.Click += (_, _) => ChangeBrowser();
        ToolStripMenuItem changePreferredSiteItem = MenuItem("Change Preferred &Website…");
        changePreferredSiteItem.Click += (_, _) => ChangePreferredSite();
        editMenu.DropDownItems.AddRange([changeBrowserItem, changePreferredSiteItem]);

        ToolStripMenuItem helpMenu = MenuItem("&Help");
        ToolStripMenuItem aboutItem = MenuItem("&About");
        aboutItem.Click += (_, _) => ShowAbout();
        helpMenu.DropDownItems.Add(aboutItem);

        menu.Items.AddRange([fileMenu, editMenu, helpMenu]);
        return menu;
    }

    private static ToolStripMenuItem MenuItem(string text) => new(text)
    {
        ForeColor = Color.White
    };

    private Control BuildContent()
    {
        StreamingBackgroundPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 28, 28, 24)
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(BuildHeading(), 0, 0);
        layout.Controls.Add(BuildServices(), 0, 1);
        root.Controls.Add(layout);
        return root;
    }

    private static Control BuildHeading()
    {
        Label heading = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = Color.White,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 23f, FontStyle.Bold),
            Text = "What would you like to watch?",
            TextAlign = ContentAlignment.MiddleCenter
        };
        return heading;
    }

    private Control BuildServices()
    {
        CompactServicePanel services = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        foreach (SiteChoice site in SiteCatalog.Sites.Where(site => HomeServices.Contains(site.DisplayName)))
        {
            CompactServiceButton button = new(site);
            button.Click += async (_, _) => await OpenSiteAsync(site);
            services.Controls.Add(button);
        }

        return services;
    }

    private async Task ChooseAndOpenSiteAsync()
    {
        SiteChoice? site = SitePickerForm.SelectSite(settings.SiteUrl);
        if (site is not null)
        {
            await OpenSiteAsync(site);
        }
    }

    private async Task OpenPreferredSiteAsync()
    {
        if (!SiteUrl.TryNormalize(settings.SiteUrl, out string normalized))
        {
            ChangePreferredSite();
            return;
        }

        await OpenSiteAsync(new SiteChoice(settings.SiteName, normalized));
    }

    private async Task OpenSiteAsync(SiteChoice site)
    {
        try
        {
            await sessions.OpenAsync(settings, site);
            settings = settings with
            {
                SchemaVersion = 5,
                SiteName = site.DisplayName,
                SiteUrl = site.Url
            };
            SettingsStore.Save(settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Could not start {site.DisplayName}.\n\n{exception.Message}",
                AppDisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ChangeBrowser()
    {
        BrowserChoice? browser = BrowserPickerForm.SelectBrowser(settings.BrowserPath);
        if (browser is null)
        {
            return;
        }

        settings = settings with
        {
            SchemaVersion = 5,
            BrowserKey = browser.Key,
            BrowserName = browser.DisplayName,
            BrowserPath = Path.GetFullPath(browser.ExecutablePath)
        };
        SettingsStore.Save(settings);
    }

    private void ChangePreferredSite()
    {
        SiteChoice? site = SitePickerForm.SelectSite(settings.SiteUrl);
        if (site is null)
        {
            return;
        }

        settings = settings with
        {
            SchemaVersion = 5,
            SiteName = site.DisplayName,
            SiteUrl = site.Url
        };
        SettingsStore.Save(settings);
    }

    private void ShowAbout()
    {
        string version = Application.ProductVersion;
        MessageBox.Show(
            $"Windowed Streaming Player\nVersion {version}\n\n"
            + "Open YouTube, Crunchyroll, Prime Video and BBC iPlayer "
            + "in separate resizable browser app windows.\n\n"
            + "Use File → New Window for other websites.",
            $"About {AppDisplayName}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
