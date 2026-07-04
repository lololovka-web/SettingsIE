using System.Text.Json;
using SettingsIE.Models;
using SettingsIE.Services;

namespace SettingsIE;

public partial class Form1 : Form
{
    private readonly IWindowsSettingsRepository _repository;
    private readonly ISettingsExporter _exporter;
    private readonly ISettingsImporter _importer;
    private readonly IRegistryBackupService _backupService;
    private readonly ConfigLibraryService _libraryService;

    private TabControl _tabControl;
    private TabPage _exportWin10Tab;
    private TabPage _exportWin11Tab;
    private TabPage _importTab;
    private TabPage _libraryTab;
    private TreeView _win10TreeView;
    private TreeView _win11TreeView;
    private TreeView _importTreeView;
    private ListView _libraryListView;
    private TextBox _importPathTextBox;
    private Button _importBrowseButton;
    private Button _importButton;
    private Button _backupButton;
    private Button _restoreButton;
    private ProgressBar _progressBar;
    private TextBox _logTextBox;
    private TextBox _jsonPreviewTextBox;
    private List<SettingsCategory> _win10Categories;
    private List<SettingsCategory> _win11Categories;
    private SettingsExportData? _loadedExportData;
    private readonly List<Control> _adminWarnings = new();
    private bool _darkMode;
    private bool _hasAdminRights;

    private static readonly Color PrimaryColor = Color.FromArgb(0, 103, 192);
    private static readonly Color PrimaryLight = Color.FromArgb(0, 130, 220);
    private static readonly Color SuccessColor = Color.FromArgb(15, 120, 15);
    private static readonly Color WarningColor = Color.FromArgb(195, 115, 0);
    private static readonly Color DangerColor = Color.FromArgb(195, 55, 55);

    private static readonly Color DarkBg = Color.FromArgb(26, 26, 30);
    private static readonly Color DarkPanel = Color.FromArgb(34, 34, 40);
    private static readonly Color DarkSurface = Color.FromArgb(42, 42, 50);
    private static readonly Color DarkBorder = Color.FromArgb(58, 58, 68);
    private static readonly Color DarkText = Color.FromArgb(215, 215, 225);
    private static readonly Color DarkSubtext = Color.FromArgb(150, 155, 170);

    public Form1()
    {
        _repository = new WindowsSettingsRepository();
        _exporter = new SettingsExporter(_repository);
        _importer = new SettingsImporter(_repository);
        _backupService = new RegistryBackupService();
        _libraryService = new ConfigLibraryService();
        _win10Categories = _repository.GetDefaultCategories();
        _win11Categories = _repository.GetWindows11Categories();
        _darkMode = false;

        InitializeComponent();
        LoadCategoriesToTree(_win10TreeView, _win10Categories);
        LoadCategoriesToTree(_win11TreeView, _win11Categories);
        RefreshLibraryList();
        CheckAdminRights();
    }

    private bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private void CheckAdminRights()
    {
        _hasAdminRights = IsAdministrator();
        foreach (var c in _adminWarnings) c.Visible = !_hasAdminRights;
        Log(_hasAdminRights
            ? "Права администратора: есть."
            : "Права администратора: нет. Некоторые функции могут быть недоступны.");
    }

    private void RequestAdmin_Click(object? sender, EventArgs e)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            System.Diagnostics.Process.Start(psi);
            Application.Exit();
        }
        catch (Exception ex)
        {
            Log($"Не удалось повысить права: {ex.Message}");
            MessageBox.Show("Не удалось запустить программу от имени администратора.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowAdminInfo(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "Некоторые функции SettingsIE требуют прав администратора:\n\n" +
            "• Чтение и запись разделов реестра (HKEY_LOCAL_MACHINE)\n" +
            "• Создание резервной копии реестра\n" +
            "• Импорт .reg файлов\n" +
            "• Создание точки восстановления системы\n\n" +
            "Без прав администратора доступен только экспорт\nиз HKEY_CURRENT_USER.",
            "Зачем нужны права администратора?",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void InitializeComponent()
    {
        Text = "SettingsIE — Управление настройками Windows";
        Size = new Size(1040, 780);
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(245, 247, 250);

        var headerPanel = new Panel { Height = 56, Dock = DockStyle.Top, BackColor = Color.FromArgb(32, 36, 46) };
        headerPanel.Controls.Add(new Label
        {
            Text = "⚙ SettingsIE", Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White, Location = new Point(18, 8), AutoSize = true
        });
        headerPanel.Controls.Add(new Label
        {
            Text = "Windows 10 / 11", Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(150, 160, 185), Location = new Point(162, 17), AutoSize = true
        });

        var themeBtn = new Button
        {
            Text = "🌙", FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 },
            Size = new Size(36, 30), Location = new Point(headerPanel.Width - 48, 12),
            Cursor = Cursors.Hand, Font = new Font("Segoe UI", 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        themeBtn.Click += ToggleTheme;
        headerPanel.Resize += (_, _) => themeBtn.Location = new Point(headerPanel.Width - 48, 12);
        headerPanel.Controls.Add(themeBtn);

        var contentArea = new Panel { Dock = DockStyle.Fill };

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
            SplitterDistance = 510, SplitterWidth = 1, BackColor = Color.FromArgb(220, 224, 230)
        };

        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Padding = new Point(12, 5) };

        _exportWin10Tab = new TabPage("🪟 Windows 10");
        _exportWin11Tab = new TabPage("🪟 Windows 11");
        _importTab = new TabPage("📥 Импорт");
        _libraryTab = new TabPage("📚 Библиотека");

        BuildWinTab(_exportWin10Tab, out _win10TreeView, "Win10");
        BuildWinTab(_exportWin11Tab, out _win11TreeView, "Win11");
        BuildImportTab();
        BuildLibraryTab();

        _tabControl.TabPages.Add(_exportWin10Tab);
        _tabControl.TabPages.Add(_exportWin11Tab);
        _tabControl.TabPages.Add(_importTab);
        _tabControl.TabPages.Add(_libraryTab);

        mainSplit.Panel1.Controls.Add(_tabControl);

        var logPanel = new Panel { Dock = DockStyle.Fill };
        var logHeader = new Panel { Height = 26, Dock = DockStyle.Top, BackColor = Color.FromArgb(240, 242, 245) };
        logHeader.Controls.Add(new Label { Text = "  📋 Журнал операций", Location = new Point(0, 3), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(60, 64, 72) });

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, Font = new Font("Cascadia Code", 9.25f),
            BackColor = Color.FromArgb(28, 28, 34), ForeColor = Color.FromArgb(0, 218, 118), BorderStyle = BorderStyle.None
        };

        logPanel.Controls.Add(_logTextBox);
        logPanel.Controls.Add(logHeader);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom, Height = 4, Visible = false,
            Style = ProgressBarStyle.Continuous, ForeColor = PrimaryColor
        };

        mainSplit.Panel2.Controls.Add(logPanel);
        contentArea.Controls.Add(mainSplit);

        Controls.Add(contentArea);
        Controls.Add(headerPanel);
        Controls.Add(_progressBar);
    }

    private void BuildWinTab(TabPage tab, out TreeView tree, string platform)
    {
        tab.Padding = new Padding(4);
        tab.BackColor = Color.White;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 34, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White };
        topPanel.Controls.Add(new Label
        {
            Text = "Категории:", AutoSize = true, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(70, 74, 82), Location = new Point(0, 6)
        });
        topPanel.Controls.Add(new Panel { Width = 14, Height = 1 });

        var selAll = CreateBtn("✓ Все", Color.FromArgb(70, 128, 200), 90);
        var deselAll = CreateBtn("✕ Снять", Color.FromArgb(135, 135, 145), 90);
        topPanel.Controls.Add(selAll);
        topPanel.Controls.Add(deselAll);

        tree = new TreeView
        {
            Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 22, ShowLines = false, BackColor = Color.FromArgb(250, 251, 253)
        };

        var bottomPanel = new Panel { Dock = DockStyle.Fill, Height = 40, BackColor = Color.White };
        var botTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1, BackColor = Color.White };

        var pathBox = new TextBox
        {
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle,
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"WindowsSettings_{platform}.json"),
            BackColor = Color.FromArgb(250, 251, 253)
        };
        var browseBtn = CreateBtn("Обзор", Color.FromArgb(100, 108, 118), 80);
        var regBtn = CreateBtn("Экспорт .reg", Color.FromArgb(75, 95, 135), 115, 9f);
        var exportBtn = CreateBtn("▶ Экспорт", PrimaryColor, 120, 9.5f);

        // Admin warning inline (right after export button)
        var adminPanel = CreateAdminWarningPanel();
        botTable.SetColumnSpan(adminPanel, 2);
        _adminWarnings.Add(adminPanel);

        var capturedTree = tree;
        var capturedBox = pathBox;
        selAll.Click += (_, _) => SetAllChecked(capturedTree.Nodes, true);
        deselAll.Click += (_, _) => SetAllChecked(capturedTree.Nodes, false);
        browseBtn.Click += (_, _) =>
        {
            var d = new SaveFileDialog { Filter = "JSON (*.json)|*.json|REG (*.reg)|*.reg", FileName = capturedBox.Text };
            if (d.ShowDialog() == DialogResult.OK) capturedBox.Text = d.FileName;
        };
        regBtn.Click += (_, _) =>
        {
            capturedBox.Text = Path.ChangeExtension(capturedBox.Text, ".reg");
            ExportWin(platform, capturedTree, capturedBox);
        };
        exportBtn.Click += (_, _) => ExportWin(platform, capturedTree, capturedBox);

        botTable.Controls.Add(pathBox, 0, 0);
        botTable.Controls.Add(browseBtn, 1, 0);
        botTable.Controls.Add(regBtn, 2, 0);
        botTable.Controls.Add(exportBtn, 3, 0);
        botTable.Controls.Add(adminPanel, 4, 0);
        botTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        botTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        botTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        botTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        botTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.Controls.Add(botTable);

        layout.Controls.Add(topPanel, 0, 0);
        layout.Controls.Add(tree, 0, 1);
        layout.Controls.Add(bottomPanel, 0, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        tab.Controls.Add(layout);
    }

    private void BuildImportTab()
    {
        _importTab.Padding = new Padding(4);
        _importTab.BackColor = Color.White;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
        layout.Controls.Add(new Label
        {
            Text = "Файл для импорта:", AutoSize = true, Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(70, 74, 82), Margin = new Padding(0, 4, 0, 4)
        }, 0, 0);

        var fileP = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 34, ColumnCount = 3, RowCount = 1, BackColor = Color.White };
        _importPathTextBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(250, 251, 253) };
        _importBrowseButton = CreateBtn("Обзор", Color.FromArgb(100, 108, 118), 80);
        _importBrowseButton.Click += (_, _) =>
        {
            var d = new OpenFileDialog { Filter = "JSON (*.json)|*.json|REG (*.reg)|*.reg" };
            if (d.ShowDialog() == DialogResult.OK) { _importPathTextBox.Text = d.FileName; LoadImportFile(d.FileName); }
        };
        fileP.Controls.Add(_importPathTextBox, 0, 0);
        fileP.Controls.Add(_importBrowseButton, 1, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 220, SplitterWidth = 1, BackColor = Color.FromArgb(220, 224, 230) };
        _importTreeView = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9.5f), ItemHeight = 22, ShowLines = false, BackColor = Color.FromArgb(250, 251, 253) };

        var jsonH = new Panel { Height = 24, Dock = DockStyle.Top, BackColor = Color.FromArgb(50, 50, 58) };
        jsonH.Controls.Add(new Label { Text = "📄 Предпросмотр JSON", ForeColor = Color.FromArgb(170, 180, 210), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(6, 3), AutoSize = true });
        _jsonPreviewTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Cascadia Code", 9f), BackColor = Color.FromArgb(38, 38, 44), ForeColor = Color.FromArgb(195, 215, 240), BorderStyle = BorderStyle.None, WordWrap = false };

        split.Panel1.Controls.Add(_importTreeView);
        split.Panel2.Controls.Add(_jsonPreviewTextBox);
        split.Panel2.Controls.Add(jsonH);
        layout.Controls.Add(split, 0, 2);

        var actionP = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 36, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White };
        _backupButton = CreateBtn("💾 Резервная копия", WarningColor, 165, 9f);
        _backupButton.Click += Backup_Click;
        _restoreButton = CreateBtn("♻ Восстановить", Color.FromArgb(175, 135, 0), 145, 9f);
        _restoreButton.Click += Restore_Click;
        _importButton = CreateBtn("▶ Импортировать", SuccessColor, 145, 9.5f);
        _importButton.Click += Import_Click;

        actionP.Controls.Add(_backupButton);
        actionP.Controls.Add(_restoreButton);
        actionP.Controls.Add(new Panel { Width = 20, Height = 1 });
        actionP.Controls.Add(_importButton);
        var importAdminWarning = CreateAdminWarningPanel();
        _adminWarnings.Add(importAdminWarning);
        actionP.Controls.Add(importAdminWarning);
        layout.Controls.Add(actionP, 0, 3);
        layout.Controls.Add(fileP, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _importTab.Controls.Add(layout);
    }

    private void BuildLibraryTab()
    {
        _libraryTab.Padding = new Padding(4);
        _libraryTab.BackColor = Color.White;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(8) };

        var headerF = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 34, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White };
        headerF.Controls.Add(new Label { Text = "📚 Локальная библиотека конфигов", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(45, 48, 58), AutoSize = true });
        headerF.Controls.Add(new Panel { Width = 16, Height = 1 });
        headerF.Controls.Add(new Label { Text = "Фильтр:", AutoSize = true, ForeColor = Color.FromArgb(80, 84, 92), Font = new Font("Segoe UI", 9) });

        var filterCb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = new Font("Segoe UI", 9), BackColor = Color.White };
        filterCb.Items.AddRange(["Все", "Windows 10", "Windows 11"]);
        filterCb.SelectedIndex = 0;
        filterCb.SelectedIndexChanged += (_, _) => RefreshLibraryList(filterCb.SelectedIndex);

        var saveLibBtn = CreateBtn("➕ Сохранить", SuccessColor, 125);
        saveLibBtn.Click += SaveToLib_Click;

        headerF.Controls.Add(filterCb);
        headerF.Controls.Add(new Panel { Width = 16, Height = 1 });
        headerF.Controls.Add(saveLibBtn);

        _libraryListView = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            HideSelection = false, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f), MultiSelect = false, BackColor = Color.FromArgb(250, 251, 253)
        };
        _libraryListView.Columns.Add("Название", 140);
        _libraryListView.Columns.Add("Платформа", 85);
        _libraryListView.Columns.Add("Категорий", 65);
        _libraryListView.Columns.Add("Дата", 125);
        _libraryListView.Columns.Add("Описание", 200);
        _libraryListView.SelectedIndexChanged += (_, _) =>
        {
            var box = _libraryTab.Controls.Find("libDesc", true).FirstOrDefault() as TextBox;
            if (box == null) return;
            if (_libraryListView.SelectedItems.Count == 0) { box.Text = ""; return; }
            var entry = _libraryService.GetEntries().FirstOrDefault(x => x.Id == (string?)_libraryListView.SelectedItems[0].Tag);
            box.Text = entry?.Description ?? "";
        };

        var rightP = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.White };
        rightP.Controls.Add(new Label { Text = "Описание:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(70, 74, 82), AutoSize = true }, 0, 0);

        var descBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(248, 249, 252), ForeColor = Color.FromArgb(40, 44, 52), Name = "libDesc" };

        var acts = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 36, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White };
        var loadBtn = CreateBtn("📥 Загрузить", PrimaryColor, 125);
        var delBtn = CreateBtn("🗑 Удалить", DangerColor, 105);
        var expBtn = CreateBtn("💾 Экспорт", Color.FromArgb(75, 95, 135), 115, 9f);

        loadBtn.Click += LoadFromLib_Click;
        delBtn.Click += DeleteFromLib_Click;
        expBtn.Click += ExportLibEntry_Click;

        acts.Controls.Add(loadBtn);
        acts.Controls.Add(delBtn);
        acts.Controls.Add(expBtn);

        rightP.Controls.Add(descBox, 0, 1);
        rightP.Controls.Add(acts, 0, 2);
        rightP.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        rightP.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightP.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        layout.Controls.Add(headerF, 0, 0);
        layout.Controls.Add(new Panel(), 1, 0);
        layout.Controls.Add(_libraryListView, 0, 1);
        layout.Controls.Add(rightP, 1, 1);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _libraryTab.Controls.Add(layout);
    }

    // ─── Theme ────────────────────────────────────────────────────

    private void ToggleTheme(object? sender, EventArgs e)
    {
        _darkMode = !_darkMode;
        if (sender is Button b) b.Text = _darkMode ? "☀️" : "🌙";
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_darkMode)
        {
            BackColor = DarkBg; ForeColor = DarkText;
            ApplyPalette(this, true);
            _logTextBox.BackColor = Color.FromArgb(20, 20, 24);
            _logTextBox.ForeColor = Color.FromArgb(0, 200, 100);
            _jsonPreviewTextBox.BackColor = DarkPanel;
            _jsonPreviewTextBox.ForeColor = Color.FromArgb(170, 200, 230);
        }
        else
        {
            BackColor = Color.FromArgb(245, 247, 250); ForeColor = Color.Black;
            ApplyPalette(this, false);
            _logTextBox.BackColor = Color.FromArgb(28, 28, 34);
            _logTextBox.ForeColor = Color.FromArgb(0, 218, 118);
            _jsonPreviewTextBox.BackColor = Color.FromArgb(38, 38, 44);
            _jsonPreviewTextBox.ForeColor = Color.FromArgb(195, 215, 240);
        }
    }

    private void ApplyPalette(Control parent, bool dark)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is TabPage tp) { tp.BackColor = dark ? DarkSurface : Color.White; ApplyPalette(tp, dark); continue; }
            if (c is SplitContainer sc) { sc.BackColor = dark ? DarkBorder : Color.FromArgb(220, 224, 230); sc.Panel1.BackColor = dark ? DarkSurface : Color.White; sc.Panel2.BackColor = dark ? DarkSurface : Color.White; ApplyPalette(sc.Panel1, dark); ApplyPalette(sc.Panel2, dark); continue; }
            if (c is TreeView tv) { tv.BackColor = dark ? DarkPanel : Color.FromArgb(250, 251, 253); tv.ForeColor = dark ? DarkText : Color.Black; continue; }
            if (c is ListView lv) { lv.BackColor = dark ? DarkPanel : Color.FromArgb(250, 251, 253); lv.ForeColor = dark ? DarkText : Color.Black; continue; }
            if (c is TextBox tb && tb.ReadOnly && tb.Multiline && tb.Name != "libDesc") continue;
            if (c is TextBox tb2) { tb2.BackColor = dark ? DarkPanel : Color.White; tb2.ForeColor = dark ? DarkText : Color.Black; continue; }
            if (c is ComboBox cb) { cb.BackColor = dark ? DarkPanel : Color.White; cb.ForeColor = dark ? DarkText : Color.Black; continue; }
            if (c is Panel p)
            {
                if (p.Dock == DockStyle.Top && p.Height <= 56) { p.BackColor = dark ? Color.FromArgb(22, 24, 30) : Color.FromArgb(32, 36, 46); continue; }
                if (dark) { p.BackColor = DarkSurface; if (p.Name == "layout_Win10" || p.Name == "layout_Win11" || p.Name == "importLayout" || p.Name == "libLayout") p.BackColor = DarkSurface; }
                ApplyPalette(p, dark);
                continue;
            }
            if (c is FlowLayoutPanel || c is TableLayoutPanel) { if (dark) c.BackColor = DarkSurface; ApplyPalette(c, dark); continue; }
            if (c is Button btn && btn.FlatStyle == FlatStyle.Flat && btn.BackColor != PrimaryColor && btn.BackColor != SuccessColor && btn.BackColor != WarningColor && btn.BackColor != DangerColor) { if (dark) { btn.BackColor = DarkSurface; btn.ForeColor = DarkText; } continue; }
            if (c is Label l)
            {
                if (dark) { if (l.Parent is Panel hp && hp.Dock == DockStyle.Top && hp.Height <= 26) { l.ForeColor = DarkText; } else if (l.Parent is TableLayoutPanel || l.Parent is FlowLayoutPanel) l.ForeColor = DarkText; }
                continue;
            }
            if (c.HasChildren) ApplyPalette(c, dark);
        }
    }

    // ─── Shared helpers ───────────────────────────────────────────

    private Panel CreateAdminWarningPanel()
    {
        var panel = new Panel { Height = 28, Width = 200, Visible = false };
        var reqBtn = new Button
        {
            Text = "🔑 Запросить права", FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(255, 240, 210) },
            BackColor = Color.FromArgb(255, 235, 200), ForeColor = Color.FromArgb(110, 65, 0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
            Size = new Size(130, 24), Location = new Point(0, 2), TextAlign = ContentAlignment.MiddleCenter
        };
        reqBtn.Click += RequestAdmin_Click;

        var why = new Label
        {
            Text = "Зачем?", ForeColor = Color.FromArgb(60, 100, 180),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Underline), Cursor = Cursors.Hand,
            Location = new Point(136, 5), AutoSize = true
        };
        why.Click += ShowAdminInfo;
        why.MouseEnter += (_, _) => why.ForeColor = Color.FromArgb(0, 80, 180);
        why.MouseLeave += (_, _) => why.ForeColor = Color.FromArgb(60, 100, 180);

        panel.Controls.Add(reqBtn);
        panel.Controls.Add(why);
        return panel;
    }

    private static Button CreateBtn(string text, Color bgColor, int width, float fontSize = 9.5f)
    {
        return new Button
        {
            Text = text, Width = width, Height = 28,
            FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0, MouseOverBackColor = ControlPaint.Light(bgColor) },
            BackColor = bgColor, ForeColor = Color.White, Font = new Font("Segoe UI", fontSize),
            Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter, UseVisualStyleBackColor = false
        };
    }

    private void LoadCategoriesToTree(TreeView tree, List<SettingsCategory> cats)
    {
        tree.Nodes.Clear();
        foreach (var cat in cats)
        {
            var n = new TreeNode(cat.Name) { Tag = cat, Checked = cat.IsSelected };
            foreach (var sub in cat.SubCategories)
                n.Nodes.Add(new TreeNode(sub.Name) { Tag = sub, Checked = sub.IsSelected });
            tree.Nodes.Add(n);
        }
        tree.ExpandAll();
    }

    private static void SetAllChecked(TreeNodeCollection nodes, bool state)
    {
        foreach (TreeNode n in nodes) { n.Checked = state; if (n.Nodes.Count > 0) SetAllChecked(n.Nodes, state); }
    }

    private async void ExportWin(string platform, TreeView tree, TextBox pathBox)
    {
        var cats = platform == "Win11" ? _win11Categories : _win10Categories;
        foreach (TreeNode n in tree.Nodes)
        {
            if (n.Tag is SettingsCategory c) { c.IsSelected = n.Checked; if (n.Nodes.Count > 0) for (int i = 0; i < n.Nodes.Count; i++) if (n.Nodes[i].Tag is SettingsCategory s) s.IsSelected = n.Nodes[i].Checked; }
        }

        var path = pathBox.Text;
        if (string.IsNullOrWhiteSpace(path)) { MessageBox.Show("Укажите путь.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));

        try
        {
            if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
                await _exporter.ExportToRegAsync(path, cats, progress);
            else
                await _exporter.ExportAsync(path, cats, progress);
            Log($"Экспорт {platform} завершён: {path}");
            _progressBar.Value = 100;
            MessageBox.Show($"Настройки {platform} экспортированы.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _progressBar.Visible = false; }
    }

    private async void LoadImportFile(string path)
    {
        _progressBar.Visible = true;
        try
        {
            if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
            {
                _importTreeView.Nodes.Clear();
                _importTreeView.Nodes.Add(new TreeNode("REG: " + Path.GetFileName(path)));
                _jsonPreviewTextBox.Text = File.ReadAllText(path);
                _loadedExportData = null; Log($"Загружен REG: {path}"); return;
            }
            _loadedExportData = await _importer.LoadExportFileAsync(path);
            _importTreeView.Nodes.Clear();
            foreach (var cat in _loadedExportData.Categories)
            {
                var cn = new TreeNode($"{cat.Name}  ({cat.SubCategories.Count} подкатегорий)") { Tag = cat, Checked = true };
                foreach (var sub in cat.SubCategories)
                {
                    var sn = new TreeNode($"{sub.Name}  [{sub.Values.Count} параметров]") { Tag = sub, Checked = true };
                    int shown = 0; foreach (var kv in sub.Values) { if (shown++ >= 5) break; sn.Nodes.Add(new TreeNode($"{kv.Key} = {Trunc(kv.Value.Data)}")); }
                    if (sub.Values.Count > 5) sn.Nodes.Add(new TreeNode($"... ещё {sub.Values.Count - 5}"));
                    cn.Nodes.Add(sn);
                }
                _importTreeView.Nodes.Add(cn);
            }
            _importTreeView.ExpandAll();
            try { var j = File.ReadAllText(path); _jsonPreviewTextBox.Text = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(j), new JsonSerializerOptions { WriteIndented = true }); }
            catch { _jsonPreviewTextBox.Text = File.ReadAllText(path); }
            Log($"Загружен: {path}");
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _progressBar.Visible = false; }
    }

    private static string Trunc(string? s, int max = 60) => string.IsNullOrEmpty(s) ? "(пусто)" : s.Length > max ? s[..max] + "..." : s;

    // ─── Library ──────────────────────────────────────────────────

    private void RefreshLibraryList(int filterIndex = 0)
    {
        _libraryListView.Items.Clear();
        var entries = _libraryService.GetEntries().OrderByDescending(e => e.Created);
        foreach (var e in entries)
        {
            if (filterIndex == 1 && e.Platform != "Win10") continue;
            if (filterIndex == 2 && e.Platform != "Win11") continue;
            var it = new ListViewItem(e.Name) { Tag = e.Id };
            it.SubItems.Add(e.Platform == "Win11" ? "Windows 11" : "Windows 10");
            it.SubItems.Add(e.CategoryCount.ToString());
            it.SubItems.Add(e.Created.ToString("dd.MM.yyyy HH:mm"));
            it.SubItems.Add(e.Description.Length > 55 ? e.Description[..55] + "..." : e.Description);
            _libraryListView.Items.Add(it);
        }
    }

    private void SaveToLib_Click(object? sender, EventArgs e)
    {
        var nameBox = new TextBox();
        var descBox = new TextBox { Multiline = true, Height = 65, ScrollBars = ScrollBars.Vertical };
        var platBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        platBox.Items.AddRange(["Windows 10", "Windows 11"]); platBox.SelectedIndex = 0;

        var f = new Form { Text = "Сохранить в библиотеку", Size = new Size(400, 280), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White };
        var lay = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 6 };
        lay.Controls.Add(new Label { Text = "Название:", AutoSize = true }); lay.Controls.Add(nameBox);
        lay.Controls.Add(new Label { Text = "Описание:", AutoSize = true }); lay.Controls.Add(descBox);
        lay.Controls.Add(new Label { Text = "Платформа:", AutoSize = true }); lay.Controls.Add(platBox);
        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Width = 100, Height = 28, BackColor = PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 };
        var bp = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        bp.Controls.Add(ok); bp.Controls.Add(cancel); lay.Controls.Add(bp);
        f.Controls.Add(lay); f.AcceptButton = ok; f.CancelButton = cancel;

        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Введите название.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var platform = platBox.SelectedIndex == 1 ? "Win11" : "Win10";
        var cats = platform == "Win11" ? _win11Categories : _win10Categories;
        var data = new SettingsExportData
        {
            ExportDate = DateTime.Now, WindowsVersion = _repository.GetWindowsVersion(),
            Categories = cats.Where(c => c.IsSelected).Select(c => new SettingsCategory { Name = c.Name, IsSelected = true, SubCategories = c.SubCategories.Where(s => s.IsSelected).Select(s => new SettingsCategory { Name = s.Name, IsSelected = true, RegistryPaths = s.RegistryPaths, Values = s.Values }).ToList() }).ToList()
        };

        try { _libraryService.Save(nameBox.Text.Trim(), descBox.Text.Trim(), platform, data); RefreshLibraryList(); Log($"Сохранено: \"{nameBox.Text.Trim()}\" ({platform})"); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadFromLib_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) { MessageBox.Show("Выберите конфиг.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var id = _libraryListView.SelectedItems[0].Tag as string;
        var data = _libraryService.Load(id!);
        if (data == null) { MessageBox.Show("Не удалось загрузить.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        _loadedExportData = data;
        _importTreeView.Nodes.Clear();
        foreach (var cat in data.Categories)
        {
            var cn = new TreeNode($"{cat.Name}  ({cat.SubCategories.Count} подкатегорий)") { Tag = cat, Checked = true };
            foreach (var sub in cat.SubCategories)
            {
                var sn = new TreeNode($"{sub.Name}  [{sub.Values.Count} параметров]") { Tag = sub, Checked = true };
                int sh = 0; foreach (var kv in sub.Values) { if (sh++ >= 5) break; sn.Nodes.Add(new TreeNode($"{kv.Key} = {Trunc(kv.Value.Data)}")); }
                if (sub.Values.Count > 5) sn.Nodes.Add(new TreeNode($"... ещё {sub.Values.Count - 5}"));
                cn.Nodes.Add(sn);
            }
            _importTreeView.Nodes.Add(cn);
        }
        _importTreeView.ExpandAll();
        _tabControl.SelectedTab = _importTab;
        Log($"Загружен из библиотеки: {_libraryListView.SelectedItems[0].Text}");
    }

    private void DeleteFromLib_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;
        var id = _libraryListView.SelectedItems[0].Tag as string;
        if (MessageBox.Show($"Удалить \"{_libraryListView.SelectedItems[0].Text}\"?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _libraryService.Delete(id!); RefreshLibraryList();
    }

    private void ExportLibEntry_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;
        var id = _libraryListView.SelectedItems[0].Tag as string;
        var d = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"{_libraryListView.SelectedItems[0].Text}.json" };
        if (d.ShowDialog() != DialogResult.OK) return;
        try { _libraryService.ExportToFile(id!, d.FileName); Log($"Экспортирован: {d.FileName}"); MessageBox.Show("Готово.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ─── Backup / Import ──────────────────────────────────────────

    private async void Backup_Click(object? sender, EventArgs e)
    {
        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RegistryBackup_{DateTime.Now:yyyyMMdd_HHmmss}.reg");
            await _backupService.BackupRegistryAsync(path, progress);
            Log($"Резервная копия: {path}"); _progressBar.Value = 100;
            MessageBox.Show($"Сохранено:\n{path}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _progressBar.Visible = false; }
    }

    private async void Restore_Click(object? sender, EventArgs e)
    {
        var d = new OpenFileDialog { Filter = "REG (*.reg)|*.reg" };
        if (d.ShowDialog() != DialogResult.OK) return;
        if (MessageBox.Show("Восстановить реестр из копии?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _progressBar.Visible = true;
        try { await _backupService.RestoreRegistryAsync(d.FileName); Log($"Восстановлено: {d.FileName}"); MessageBox.Show("Реестр восстановлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _progressBar.Visible = false; }
    }

    private async void Import_Click(object? sender, EventArgs e)
    {
        var path = _importPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(path)) { MessageBox.Show("Выберите файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase)) { ImportReg(path); return; }
        if (_loadedExportData == null) { MessageBox.Show("Не удалось загрузить данные.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var selected = new List<SettingsCategory>();
        foreach (TreeNode n in _importTreeView.Nodes)
            if (n.Tag is SettingsCategory ct && n.Checked)
            {
                var sc = new SettingsCategory { Name = ct.Name, IsSelected = true, SubCategories = new() };
                foreach (TreeNode ch in n.Nodes)
                    if (ch.Tag is SettingsCategory sb && ch.Checked)
                        sc.SubCategories.Add(new SettingsCategory { Name = sb.Name, IsSelected = true, RegistryPaths = sb.RegistryPaths, Values = sb.Values });
                selected.Add(sc);
            }
        if (selected.Count == 0) { MessageBox.Show("Выберите категории.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (MessageBox.Show("Импорт изменит реестр. Продолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));
        try { await _importer.ImportAsync(_loadedExportData, selected, progress); Log("Импорт завершён."); _progressBar.Value = 100; MessageBox.Show("Настройки импортированы.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _progressBar.Visible = false; }
    }

    private void ImportReg(string path)
    {
        if (MessageBox.Show($"Импортировать REG?\n{path}", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo { FileName = "reg.exe", Arguments = $"import \"{path}\"", UseShellExecute = false, CreateNoWindow = true };
            using var p = System.Diagnostics.Process.Start(psi); p?.WaitForExit();
            if (p?.ExitCode == 0) { Log($"REG импортирован: {path}"); MessageBox.Show("Готово.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            else { Log($"Ошибка REG (код: {p?.ExitCode})"); MessageBox.Show("Ошибка.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ─── Utils ────────────────────────────────────────────────────

    private void Log(string msg)
    {
        if (_logTextBox.InvokeRequired) { _logTextBox.Invoke(() => Log(msg)); return; }
        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}"); _logTextBox.ScrollToCaret();
    }
}
