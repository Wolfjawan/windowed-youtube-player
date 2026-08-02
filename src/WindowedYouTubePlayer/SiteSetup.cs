using System.Drawing;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal static class SiteCatalog
{
    public static readonly SiteChoice[] Sites =
    [
        new("YouTube", "https://www.youtube.com/"),
        new("Crunchyroll", "https://www.crunchyroll.com/"),
        new("Prime Video", "https://www.primevideo.com/"),
        new("Netflix", "https://www.netflix.com/"),
        new("Disney+", "https://www.disneyplus.com/"),
        new("BBC iPlayer", "https://www.bbc.co.uk/iplayer"),
        new("Custom website…", "")
    ];
}

internal static class SiteUrl
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string candidate = value.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    public static string FriendlyName(string normalizedUrl)
    {
        Uri uri = new(normalizedUrl);
        string host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
        return host;
    }
}

internal sealed class SitePickerForm : Form
{
    private readonly ComboBox siteList = new();
    private readonly TextBox urlBox = new();
    private SiteChoice? selectedSite;

    private SitePickerForm(string? preferredUrl)
    {
        Text = "Choose a streaming website";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(590, 330);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = AppIcon.TryLoad();

        Label heading = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Location = new Point(24, 22),
            Text = "Choose the website to open"
        };

        Label description = new()
        {
            AutoSize = false,
            Location = new Point(25, 58),
            Size = new Size(540, 46),
            Text = "Select a common streaming service or enter the main link for any other website. You can sign in inside the app window."
        };

        Label siteLabel = new()
        {
            AutoSize = true,
            Location = new Point(25, 116),
            Text = "Website"
        };

        siteList.DropDownStyle = ComboBoxStyle.DropDownList;
        siteList.Location = new Point(25, 140);
        siteList.Size = new Size(540, 32);
        siteList.Items.AddRange(SiteCatalog.Sites);
        siteList.SelectedIndexChanged += (_, _) => ApplySelectedSite();

        Label urlLabel = new()
        {
            AutoSize = true,
            Location = new Point(25, 188),
            Text = "Main website link"
        };

        urlBox.Location = new Point(25, 212);
        urlBox.Size = new Size(540, 31);
        urlBox.PlaceholderText = "https://example.com/";
        urlBox.TextChanged += (_, _) => SelectCustomWhenEdited();

        Button cancelButton = new()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(335, 266),
            Size = new Size(105, 36)
        };

        Button launchButton = new()
        {
            Text = "Use website",
            Location = new Point(450, 266),
            Size = new Size(115, 36)
        };
        launchButton.Click += (_, _) => AcceptSite();

        Controls.AddRange([heading, description, siteLabel, siteList, urlLabel, urlBox, cancelButton, launchButton]);
        AcceptButton = launchButton;
        CancelButton = cancelButton;

        int match = -1;
        if (!string.IsNullOrWhiteSpace(preferredUrl))
        {
            match = Array.FindIndex(SiteCatalog.Sites, site =>
                !string.IsNullOrWhiteSpace(site.Url)
                && string.Equals(site.Url, preferredUrl, StringComparison.OrdinalIgnoreCase));
        }

        siteList.SelectedIndex = match >= 0 ? match : 0;
        if (match < 0 && !string.IsNullOrWhiteSpace(preferredUrl))
        {
            siteList.SelectedIndex = SiteCatalog.Sites.Length - 1;
            urlBox.Text = preferredUrl;
        }
    }

    public static SiteChoice? SelectSite(string? preferredUrl)
    {
        using SitePickerForm form = new(preferredUrl);
        return form.ShowDialog() == DialogResult.OK ? form.selectedSite : null;
    }

    private void ApplySelectedSite()
    {
        if (siteList.SelectedItem is SiteChoice site && !string.IsNullOrWhiteSpace(site.Url))
        {
            urlBox.Text = site.Url;
        }
    }

    private void SelectCustomWhenEdited()
    {
        if (siteList.SelectedItem is SiteChoice selected
            && !string.IsNullOrWhiteSpace(selected.Url)
            && !string.Equals(selected.Url, urlBox.Text, StringComparison.OrdinalIgnoreCase))
        {
            siteList.SelectedIndex = SiteCatalog.Sites.Length - 1;
        }
    }

    private void AcceptSite()
    {
        if (!SiteUrl.TryNormalize(urlBox.Text, out string normalized))
        {
            MessageBox.Show(
                "Enter a valid website address, such as https://www.crunchyroll.com/.",
                "Windowed Streaming Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        string displayName = siteList.SelectedItem is SiteChoice site
            && !string.IsNullOrWhiteSpace(site.Url)
            && string.Equals(site.Url, normalized, StringComparison.OrdinalIgnoreCase)
                ? site.DisplayName
                : SiteUrl.FriendlyName(normalized);

        selectedSite = new SiteChoice(displayName, normalized);
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal static class AppIcon
{
    public static Icon? TryLoad()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }
}
