using System.Drawing;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal sealed class MainForm : Form
{
    private const string ProductName = "Windowed Streaming Player";

    private readonly BrowserSessionManager sessions;
    private readonly Label browserValueLabel = new();
    private readonly Label preferredSiteValueLabel = new();
    private readonly Label statusLabel = new();

    private AppSettings settings;

    public MainForm(AppSettings initialSettings, BrowserSessionManager sessionManager)
    {
        settings = initialSettings;
        sessions = sessionManager;

        Text = ProductName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 560);
        ClientSize = new Size(820, 620);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = AppIcon.TryLoad();

        MainMenuStrip = BuildMenu();
        Controls.Add(BuildContent());
        Controls.Add(MainMenuStrip);
        UpdateSettingsDisplay();
    }

    private MenuStrip BuildMenu()
    {
        MenuStrip menu = new();

        ToolStripMenuItem fileMenu = new("&File");
        ToolStripMenuItem newWindowItem = new("&New Window…")
        {
            ShortcutKeys = Keys.Control | Keys.N
        };
        newWindowItem.Click += async (_, _) => await ChooseAndOpenSiteAsync();

        ToolStripMenuItem openPreferredItem = new("Open &Preferred Website")
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.N
        };
        openPreferredItem.Click += async (_, _) => await OpenPreferredSiteAsync();

        ToolStripMenuItem exitItem = new("E&xit");
        exitItem.Click += (_, _) => Close();

        fileMenu.DropDownItems.AddRange(
        [
            newWindowItem,
            openPreferredItem,
            new ToolStripSeparator(),
            exitItem
        ]);

        ToolStripMenuItem editMenu = new("&Edit");
        ToolStripMenuItem changeBrowserItem = new("Change &Browser…");
        changeBrowserItem.Click += (_, _) => ChangeBrowser();

        ToolStripMenuItem changePreferredSiteItem = new("Change Preferred &Website…");
        changePreferredSiteItem.Click += (_, _) => ChangePreferredSite();

        editMenu.DropDownItems.AddRange(
        [
            changeBrowserItem,
            changePreferredSiteItem
        ]);

        ToolStripMenuItem helpMenu = new("&Help");
        ToolStripMenuItem aboutItem = new("&About");
        aboutItem.Click += (_, _) => ShowAbout();
        helpMenu.DropDownItems.Add(aboutItem);

        menu.Items.AddRange([fileMenu, editMenu, helpMenu]);
        return menu;
    }

    private Control BuildContent()
    {
        Panel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 30, 26, 22)
        };

        Label heading = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold),
            Location = new Point(26, 34),
            Text = "Open a streaming website"
        };

        Label description = new()
        {
            AutoSize = false,
            Location = new Point(29, 78),
            Size = new Size(740, 48),
            Text = "Choose a service below, or use File → New Window to select a preset or enter any website address. Each selection opens in its own resizable streaming window."
        };

        FlowLayoutPanel services = new()
        {
            Location = new Point(26, 136),
            Size = new Size(760, 280),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 8)
        };

        foreach (SiteChoice site in SiteCatalog.Sites.Where(site => !string.IsNullOrWhiteSpace(site.Url)))
        {
            Button button = CreateSiteButton(site.DisplayName);
            button.Click += async (_, _) => await OpenSiteAsync(site);
            services.Controls.Add(button);
        }

        Button customButton = CreateSiteButton("Custom website…");
        customButton.Click += async (_, _) => await ChooseAndOpenSiteAsync(selectCustom: true);
        services.Controls.Add(customButton);

        GroupBox settingsGroup = new()
        {
            Text = "Current settings",
            Location = new Point(26, 432),
            Size = new Size(760, 112),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        Label browserLabel = new()
        {
            AutoSize = true,
            Location = new Point(18, 29),
            Text = "Browser:"
        };

        browserValueLabel.AutoSize = true;
        browserValueLabel.Location = new Point(105, 29);
        browserValueLabel.Font = new Font(Font, FontStyle.Bold);

        Label siteLabel = new()
        {
            AutoSize = true,
            Location = new Point(18, 61),
            Text = "Preferred:"
        };

        preferredSiteValueLabel.AutoSize = true;
        preferredSiteValueLabel.Location = new Point(105, 61);
        preferredSiteValueLabel.Font = new Font(Font, FontStyle.Bold);

        Button changeBrowserButton = new()
        {
            Text = "Change browser…",
            Location = new Point(565, 24),
            Size = new Size(170, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        changeBrowserButton.Click += (_, _) => ChangeBrowser();

        Button changeSiteButton = new()
        {
            Text = "Change preferred site…",
            Location = new Point(565, 62),
            Size = new Size(170, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        changeSiteButton.Click += (_, _) => ChangePreferredSite();

        settingsGroup.Controls.AddRange(
        [
            browserLabel,
            browserValueLabel,
            siteLabel,
            preferredSiteValueLabel,
            changeBrowserButton,
            changeSiteButton
        ]);

        statusLabel.AutoSize = false;
        statusLabel.Location = new Point(29, 560);
        statusLabel.Size = new Size(750, 30);
        statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        statusLabel.Text = "Ready.";

        root.Controls.AddRange([heading, description, services, settingsGroup, statusLabel]);
        return root;
    }

    private static Button CreateSiteButton(string text) => new()
    {
        Text = text,
        Size = new Size(175, 66),
        Margin = new Padding(0, 0, 14, 14),
        Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10, FontStyle.Bold),
        UseVisualStyleBackColor = true
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
            await sessions.OpenAsync(settings, site);
            settings = settings with
            {
                SchemaVersion = 5,
                SiteName = site.DisplayName,
                SiteUrl = site.Url
            };
            SettingsStore.Save(settings);
            UpdateSettingsDisplay();
            SetStatus($"{site.DisplayName} opened. Use File → New Window to open another service.");
        }
        catch (Exception exception)
        {
            SetStatus("The streaming window could not be opened.");
            MessageBox.Show(
                $"Could not open {site.DisplayName}.\n\n{exception.Message}",
                ProductName,
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
            $"About {ProductName}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
