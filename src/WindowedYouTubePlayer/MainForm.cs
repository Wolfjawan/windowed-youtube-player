using System.Drawing;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal sealed class MainForm : Form
{
    private const string AppDisplayName = "Windowed Streaming Player";

    private readonly BrowserSessionManager sessions;
    private readonly Label browserValueLabel = new();
    private readonly Label preferredSiteValueLabel = new();
    private readonly Label statusLabel = new();

    private AppSettings settings;

    public MainForm(AppSettings initialSettings, BrowserSessionManager sessionManager)
    {
        settings = initialSettings;
        sessions = sessionManager;

        Text = AppDisplayName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(650, 580);
        ClientSize = new Size(980, 720);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = AppIcon.TryLoad();
        BackColor = Color.FromArgb(8, 12, 30);

        MainMenuStrip = BuildMenu();
        Controls.Add(BuildContent());
        Controls.Add(MainMenuStrip);
        MainMenuStrip.BringToFront();
        UpdateSettingsDisplay();
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
            Padding = new Padding(28, 24, 28, 22)
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        layout.Controls.Add(BuildHero(), 0, 0);
        layout.Controls.Add(BuildServices(), 0, 1);
        layout.Controls.Add(BuildSettingsPanel(), 0, 2);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(205, 211, 230);
        statusLabel.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f, FontStyle.Regular);
        statusLabel.Padding = new Padding(3, 4, 0, 0);
        statusLabel.Text = "Ready.";
        layout.Controls.Add(statusLabel, 0, 3);

        root.Controls.Add(layout);
        return root;
    }

    private static Control BuildHero()
    {
        Panel hero = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };

        Label eyebrow = new()
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(154, 134, 255),
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f, FontStyle.Bold),
            Location = new Point(2, 2),
            Text = "YOUR STREAMING DESK"
        };

        Label heading = new()
        {
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 23f, FontStyle.Bold),
            Location = new Point(0, 24),
            Text = "What would you like to watch?"
        };

        Label description = new()
        {
            AutoSize = false,
            ForeColor = Color.FromArgb(200, 206, 225),
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9.5f, FontStyle.Regular),
            Location = new Point(3, 68),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(850, 40),
            Text = "Choose a service below. Every selection opens in its own resizable app window while your sign-ins remain saved."
        };

        hero.Controls.AddRange([eyebrow, heading, description]);
        hero.Resize += (_, _) => description.Width = Math.Max(200, hero.ClientSize.Width - 6);
        return hero;
    }

    private Control BuildServices()
    {
        ResponsiveServicePanel services = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12)
        };

        foreach (SiteChoice site in SiteCatalog.Sites.Where(site => !string.IsNullOrWhiteSpace(site.Url)))
        {
            ServiceCard card = new(site);
            card.Click += async (_, _) => await OpenSiteAsync(site);
            services.Controls.Add(card);
        }

        SiteChoice custom = new("Custom website", "");
        ServiceCard customCard = new(custom);
        customCard.Click += async (_, _) => await ChooseAndOpenSiteAsync(selectCustom: true);
        services.Controls.Add(customCard);
        return services;
    }

    private Control BuildSettingsPanel()
    {
        Panel settingsCard = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(31, 34, 54),
            Padding = new Padding(20, 16, 20, 14),
            Margin = new Padding(0, 4, 0, 8)
        };

        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 3,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        Label title = new()
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10.5f, FontStyle.Bold),
            Text = "Current setup",
            TextAlign = ContentAlignment.MiddleLeft
        };
        grid.Controls.Add(title, 0, 0);
        grid.SetColumnSpan(title, 3);

        grid.Controls.Add(SettingLabel("Browser"), 0, 1);
        ConfigureSettingValue(browserValueLabel);
        grid.Controls.Add(browserValueLabel, 1, 1);
        Button changeBrowserButton = SecondaryButton("Change browser…");
        changeBrowserButton.Click += (_, _) => ChangeBrowser();
        grid.Controls.Add(changeBrowserButton, 2, 1);

        grid.Controls.Add(SettingLabel("Preferred"), 0, 2);
        ConfigureSettingValue(preferredSiteValueLabel);
        grid.Controls.Add(preferredSiteValueLabel, 1, 2);
        Button changeSiteButton = SecondaryButton("Change preferred site…");
        changeSiteButton.Click += (_, _) => ChangePreferredSite();
        grid.Controls.Add(changeSiteButton, 2, 2);

        settingsCard.Controls.Add(grid);
        return settingsCard;
    }

    private static Label SettingLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Color.FromArgb(167, 174, 200),
        Text = text,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static void ConfigureSettingValue(Label label)
    {
        label.Dock = DockStyle.Fill;
        label.ForeColor = Color.White;
        label.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9.5f, FontStyle.Bold);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
    }

    private static Button SecondaryButton(string text) => new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(10, 4, 0, 4),
        Text = text,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(64, 57, 96),
        ForeColor = Color.White,
        UseVisualStyleBackColor = false,
        Cursor = Cursors.Hand
    };

    private async Task ChooseAndOpenSiteAsync(bool selectCustom = false)
    {
        string preferred = selectCustom ? "https://" : settings.SiteUrl;
        SiteChoice? site = SitePickerForm.SelectSite(preferred);
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
        SetStatus($"Opening {site.DisplayName} in {settings.BrowserName}…");

        try
        {
            BrowserLaunchResult result = await sessions.OpenAsync(settings, site);
            settings = settings with
            {
                SchemaVersion = 5,
                SiteName = site.DisplayName,
                SiteUrl = site.Url
            };
            SettingsStore.Save(settings);
            UpdateSettingsDisplay();

            SetStatus(result.ControllerConnected
                ? $"{site.DisplayName} opened. Use File → New Window to open another service."
                : $"{site.DisplayName} opened. Fullscreen control is reconnecting quietly in the background.");
        }
        catch (Exception exception)
        {
            SetStatus("The streaming window could not be started.");
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
        UpdateSettingsDisplay();
        SetStatus($"New windows will use {settings.BrowserName}. Existing windows remain open.");
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
        UpdateSettingsDisplay();
        SetStatus($"{site.DisplayName} is now the preferred website.");
    }

    private void UpdateSettingsDisplay()
    {
        browserValueLabel.Text = settings.BrowserName;
        preferredSiteValueLabel.Text = settings.SiteName;
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
    }

    private void ShowAbout()
    {
        string version = Application.ProductVersion;
        MessageBox.Show(
            $"Windowed Streaming Player\nVersion {version}\n\n"
            + "Open YouTube, Crunchyroll, Prime Video, Netflix and other websites "
            + "in separate resizable browser app windows.\n\n"
            + "Use File → New Window to choose another service.",
            $"About {AppDisplayName}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
