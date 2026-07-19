using System.Drawing;

namespace WindowedYouTubePlayer;

internal sealed class MainForm : Form
{
    private readonly TextBox _urlTextBox = new();
    private readonly TextBox _bravePathTextBox = new();
    private readonly NumericUpDown _widthInput = new();
    private readonly NumericUpDown _heightInput = new();
    private readonly CheckBox _autoPlayCheckBox = new();
    private readonly CheckBox _borderlessCheckBox = new();
    private readonly Button _launchButton = new();
    private readonly Button _toggleFrameButton = new();
    private readonly Button _closePlayerButton = new();
    private readonly Label _statusLabel = new();
    private readonly AppSettings _settings;

    private PlayerWindowController? _playerController;

    public MainForm()
    {
        _settings = SettingsStore.Load();
        ConfigureForm();
        BuildLayout();
        LoadSettingsIntoControls();
    }

    private void ConfigureForm()
    {
        Text = "Windowed YouTube Player";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(680, 380);
        Size = new Size(780, 430);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 7
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label title = new()
        {
            AutoSize = true,
            Text = "Play YouTube in a clean, resizable Brave window",
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 16)
        };
        root.Controls.Add(title);

        TableLayoutPanel urlRow = CreateTwoColumnRow();
        _urlTextBox.Dock = DockStyle.Fill;
        _urlTextBox.PlaceholderText = "Paste a YouTube video, Short, or playlist URL";
        _urlTextBox.Margin = new Padding(0, 0, 8, 0);
        _urlTextBox.KeyDown += async (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                eventArgs.SuppressKeyPress = true;
                await LaunchPlayerAsync();
            }
        };

        Button pasteButton = new()
        {
            Text = "Paste",
            AutoSize = true,
            Margin = Padding.Empty
        };
        pasteButton.Click += (_, _) =>
        {
            if (Clipboard.ContainsText())
            {
                _urlTextBox.Text = Clipboard.GetText().Trim();
                _urlTextBox.SelectionStart = _urlTextBox.TextLength;
            }
        };
        urlRow.Controls.Add(_urlTextBox, 0, 0);
        urlRow.Controls.Add(pasteButton, 1, 0);
        root.Controls.Add(urlRow);

        TableLayoutPanel sizeRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Margin = new Padding(0, 14, 0, 0)
        };
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _widthInput.Minimum = 480;
        _widthInput.Maximum = 7680;
        _widthInput.Increment = 10;
        _widthInput.Dock = DockStyle.Fill;
        _heightInput.Minimum = 270;
        _heightInput.Maximum = 4320;
        _heightInput.Increment = 10;
        _heightInput.Dock = DockStyle.Fill;

        Button hdPresetButton = new() { Text = "1280 × 720", AutoSize = true };
        hdPresetButton.Click += (_, _) => SetSizePreset(1280, 720);
        Button fullHdPresetButton = new() { Text = "1920 × 1080", AutoSize = true };
        fullHdPresetButton.Click += (_, _) => SetSizePreset(1920, 1080);

        sizeRow.Controls.Add(new Label { Text = "Width", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) }, 0, 0);
        sizeRow.Controls.Add(_widthInput, 1, 0);
        sizeRow.Controls.Add(new Label { Text = "Height", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(14, 6, 8, 0) }, 2, 0);
        sizeRow.Controls.Add(_heightInput, 3, 0);
        sizeRow.Controls.Add(hdPresetButton, 4, 0);
        sizeRow.Controls.Add(fullHdPresetButton, 5, 0);
        root.Controls.Add(sizeRow);

        FlowLayoutPanel optionsRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 14, 0, 0)
        };
        _autoPlayCheckBox.Text = "Autoplay";
        _autoPlayCheckBox.AutoSize = true;
        _borderlessCheckBox.Text = "Start borderless";
        _borderlessCheckBox.AutoSize = true;
        _borderlessCheckBox.Margin = new Padding(22, 3, 3, 3);
        optionsRow.Controls.Add(_autoPlayCheckBox);
        optionsRow.Controls.Add(_borderlessCheckBox);
        root.Controls.Add(optionsRow);

        TableLayoutPanel braveRow = CreateTwoColumnRow();
        braveRow.Margin = new Padding(0, 14, 0, 0);
        _bravePathTextBox.Dock = DockStyle.Fill;
        _bravePathTextBox.ReadOnly = true;
        _bravePathTextBox.Margin = new Padding(0, 0, 8, 0);
        Button browseButton = new() { Text = "Find Brave…", AutoSize = true, Margin = Padding.Empty };
        browseButton.Click += (_, _) => BrowseForBrave();
        braveRow.Controls.Add(_bravePathTextBox, 0, 0);
        braveRow.Controls.Add(browseButton, 1, 0);
        root.Controls.Add(braveRow);

        FlowLayoutPanel actionRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 18, 0, 0)
        };
        _launchButton.Text = "Launch player";
        _launchButton.AutoSize = true;
        _launchButton.Padding = new Padding(12, 5, 12, 5);
        _launchButton.Click += async (_, _) => await LaunchPlayerAsync();

        _toggleFrameButton.Text = "Toggle player frame";
        _toggleFrameButton.AutoSize = true;
        _toggleFrameButton.Enabled = false;
        _toggleFrameButton.Margin = new Padding(12, 0, 0, 0);
        _toggleFrameButton.Click += (_, _) => TogglePlayerFrame();

        _closePlayerButton.Text = "Close player";
        _closePlayerButton.AutoSize = true;
        _closePlayerButton.Enabled = false;
        _closePlayerButton.Margin = new Padding(12, 0, 0, 0);
        _closePlayerButton.Click += (_, _) => ClosePlayer();

        actionRow.Controls.Add(_launchButton);
        actionRow.Controls.Add(_toggleFrameButton);
        actionRow.Controls.Add(_closePlayerButton);
        root.Controls.Add(actionRow);

        _statusLabel.AutoSize = true;
        _statusLabel.Text = "The YouTube fullscreen button is disabled so playback stays inside the resizable window.";
        _statusLabel.ForeColor = SystemColors.GrayText;
        _statusLabel.Margin = new Padding(0, 18, 0, 0);
        root.Controls.Add(_statusLabel);

        Controls.Add(root);
        AcceptButton = _launchButton;
    }

    private static TableLayoutPanel CreateTwoColumnRow()
    {
        TableLayoutPanel row = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return row;
    }

    private void LoadSettingsIntoControls()
    {
        _urlTextBox.Text = _settings.LastUrl;
        _widthInput.Value = Math.Clamp(_settings.PlayerWidth, (int)_widthInput.Minimum, (int)_widthInput.Maximum);
        _heightInput.Value = Math.Clamp(_settings.PlayerHeight, (int)_heightInput.Minimum, (int)_heightInput.Maximum);
        _autoPlayCheckBox.Checked = _settings.AutoPlay;
        _borderlessCheckBox.Checked = _settings.StartBorderless;

        string? bravePath = BraveLocator.Find(_settings.BravePath);
        _bravePathTextBox.Text = bravePath ?? "Brave was not found. Click Find Brave…";
    }

    private async Task LaunchPlayerAsync()
    {
        if (!YouTubeUrlParser.TryCreatePlayerUrl(
                _urlTextBox.Text,
                _autoPlayCheckBox.Checked,
                out YouTubeTarget? target,
                out string error))
        {
            ShowStatus(error, isError: true);
            return;
        }

        string? bravePath = BraveLocator.Find(_bravePathTextBox.Text);
        if (bravePath is null)
        {
            ShowStatus("Brave was not found. Click Find Brave… and select brave.exe.", isError: true);
            return;
        }

        SaveCurrentSettings(bravePath);
        SetBusy(true);
        ShowStatus("Opening the clean player window…", isError: false);

        try
        {
            _playerController = await BraveLauncher.LaunchAsync(
                bravePath,
                target!.PlayerUrl,
                (int)_widthInput.Value,
                (int)_heightInput.Value,
                _borderlessCheckBox.Checked);

            bool controllable = _playerController?.IsAvailable == true;
            _toggleFrameButton.Enabled = controllable;
            _closePlayerButton.Enabled = controllable;

            ShowStatus(
                controllable
                    ? "Player opened. Resize it normally; use Toggle player frame for a clean borderless view."
                    : "Player opened, but its window handle was not detected. Brave playback should still work.",
                isError: !controllable);
        }
        catch (Exception exception)
        {
            ShowStatus($"Could not launch Brave: {exception.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BrowseForBrave()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Brave Browser",
            Filter = "Brave Browser (brave.exe)|brave.exe|Applications (*.exe)|*.exe",
            CheckFileExists = true,
            FileName = "brave.exe"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!BraveLocator.IsBraveExecutable(dialog.FileName))
        {
            ShowStatus("Please select Brave's brave.exe file.", isError: true);
            return;
        }

        _bravePathTextBox.Text = dialog.FileName;
        ShowStatus("Brave selected.", isError: false);
    }

    private void TogglePlayerFrame()
    {
        if (_playerController?.IsAvailable != true)
        {
            _toggleFrameButton.Enabled = false;
            _closePlayerButton.Enabled = false;
            ShowStatus("The player window is no longer open.", isError: true);
            return;
        }

        _playerController.ToggleFrame();
        ShowStatus(
            _playerController.IsBorderless
                ? "Borderless mode enabled. Use Windows snap shortcuts to reposition it quickly."
                : "Normal movable window frame restored.",
            isError: false);
    }

    private void ClosePlayer()
    {
        _playerController?.Close();
        _toggleFrameButton.Enabled = false;
        _closePlayerButton.Enabled = false;
        ShowStatus("Close request sent to the player window.", isError: false);
    }

    private void SaveCurrentSettings(string bravePath)
    {
        _settings.LastUrl = _urlTextBox.Text.Trim();
        _settings.PlayerWidth = (int)_widthInput.Value;
        _settings.PlayerHeight = (int)_heightInput.Value;
        _settings.AutoPlay = _autoPlayCheckBox.Checked;
        _settings.StartBorderless = _borderlessCheckBox.Checked;
        _settings.BravePath = bravePath;
        SettingsStore.Save(_settings);
    }

    private void SetSizePreset(int width, int height)
    {
        _widthInput.Value = width;
        _heightInput.Value = height;
    }

    private void SetBusy(bool busy)
    {
        _launchButton.Enabled = !busy;
        _urlTextBox.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void ShowStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = isError ? Color.Firebrick : SystemColors.GrayText;
    }
}
