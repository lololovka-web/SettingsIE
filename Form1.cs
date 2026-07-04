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
    private bool _darkMode;

    // Light theme colors
    private static readonly Color PrimaryColor = Color.FromArgb(0, 120, 212);
    private static readonly Color SuccessColor = Color.FromArgb(16, 124, 16);
    private static readonly Color WarningColor = Color.FromArgb(200, 120, 0);
    private static readonly Color DangerColor = Color.FromArgb(200, 60, 60);

    // Dark theme colors
    private static readonly Color DarkBg = Color.FromArgb(28, 28, 32);
    private static readonly Color DarkPanel = Color.FromArgb(36, 36, 42);
    private static readonly Color DarkSurface = Color.FromArgb(44, 44, 52);
    private static readonly Color DarkBorder = Color.FromArgb(60, 60, 70);
    private static readonly Color DarkText = Color.FromArgb(210, 210, 220);

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

    private void InitializeComponent()
    {
        Text = "SettingsIE — Управление настройками Windows";
        Size = new Size(1020, 780);
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;
        ApplyTheme();

        var headerPanel = new Panel { Height = 60, Dock = DockStyle.Top };
        headerPanel.Controls.Add(new Label
        {
            Text = "⚙ SettingsIE",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            Location = new Point(16, 10), AutoSize = true, Name = "lblTitle"
        });
        headerPanel.Controls.Add(new Label
        {
            Text = "Экспорт и импорт настроек Windows 10 / 11",
            Font = new Font("Segoe UI", 9),
            Location = new Point(152, 20), AutoSize = true, Name = "lblSubtitle"
        });

        var themeBtn = new Button
        {
            Text = "🌙", FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(34, 30), Location = new Point(headerPanel.Width - 48, 14),
            Cursor = Cursors.Hand, Font = new Font("Segoe UI", 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Name = "btnTheme"
        };
        themeBtn.Click += ToggleTheme;
        headerPanel.Controls.Add(themeBtn);
        headerPanel.Resize += (s, e) => themeBtn.Location = new Point(headerPanel.Width - 48, 14);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
            SplitterDistance = 510, SplitterWidth = 1
        };

        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Padding = new Point(12, 5) };

        _exportWin10Tab = new TabPage("🪟 Windows 10");
        _exportWin11Tab = new TabPage("🪟 Windows 11");
        _importTab = new TabPage("📥 Импорт");
        _libraryTab = new TabPage("📚 Библиотека");

        BuildWinTab(_exportWin10Tab, out _win10TreeView, out var win10ExportBtn, out var win10PathBox, out var win10BrowseBtn, out var win10SelectAll, out var win10DeselectAll, "Win10");
        BuildWinTab(_exportWin11Tab, out _win11TreeView, out var win11ExportBtn, out var win11PathBox, out var win11BrowseBtn, out var win11SelectAll, out var win11DeselectAll, "Win11");
        BuildImportTab();
        BuildLibraryTab();

        _tabControl.TabPages.Add(_exportWin10Tab);
        _tabControl.TabPages.Add(_exportWin11Tab);
        _tabControl.TabPages.Add(_importTab);
        _tabControl.TabPages.Add(_libraryTab);

        mainSplit.Panel1.Controls.Add(_tabControl);

        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        var logHeader = new Panel { Height = 26, Dock = DockStyle.Top };
        logHeader.Controls.Add(new Label
        {
            Text = "  📋 Журнал операций", Location = new Point(0, 3),
            AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), Name = "lblLogHeader"
        });

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Cascadia Code", 9.25f),
            BorderStyle = BorderStyle.None
        };

        logPanel.Controls.Add(_logTextBox);
        logPanel.Controls.Add(logHeader);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom, Height = 4, Visible = false,
            Style = ProgressBarStyle.Continuous
        };

        mainSplit.Panel2.Controls.Add(logPanel);

        Controls.Add(mainSplit);
        Controls.Add(headerPanel);
        Controls.Add(_progressBar);
    }

    private void BuildWinTab(TabPage tab, out TreeView tree, out Button exportBtn, out TextBox pathBox, out Button browseBtn, out Button selectAll, out Button deselectAll, string platform)
    {
        tab.Padding = new Padding(4);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8), Name = $"layout_{platform}" };

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 34, FlowDirection = FlowDirection.LeftToRight, Name = $"topPanel_{platform}" };
        topPanel.Controls.Add(new Label
        {
            Text = "Выберите категории:", AutoSize = true, Font = new Font("Segoe UI", 9.5f), Location = new Point(0, 6), Name = $"lbl_{platform}"
        });
        topPanel.Controls.Add(new Panel { Width = 16, Height = 1 });

        selectAll = CreateStyledButton("✓ Выбрать всё", Color.FromArgb(70, 130, 200), 120);
        deselectAll = CreateStyledButton("✕ Снять всё", Color.FromArgb(140, 140, 150), 120);
        topPanel.Controls.Add(selectAll);
        topPanel.Controls.Add(deselectAll);

        tree = new TreeView
        {
            Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 22, ShowLines = false, Name = $"tree_{platform}"
        };

        var bottomPanel = new Panel { Dock = DockStyle.Fill, Height = 40, Name = $"bottom_{platform}" };
        var bottomTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Name = $"table_{platform}" };
        pathBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"WindowsSettings_{platform}.json"),
            Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle
        };
        browseBtn = CreateStyledButton("Обзор...", Color.FromArgb(100, 110, 120), 85);
        var regBtn = CreateStyledButton("Экспорт .reg", Color.FromArgb(80, 100, 140), 120);
        exportBtn = CreateStyledButton("▶ Экспорт", PrimaryColor, 130);
        exportBtn.Name = $"exportBtn_{platform}";

        string capturedPlatform = platform;
        var capturedTree = tree;
        var capturedPathBox = pathBox;
        selectAll.Click += (s, e) => SetAllChecked(capturedTree.Nodes, true);
        deselectAll.Click += (s, e) => SetAllChecked(capturedTree.Nodes, false);
        browseBtn.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json|REG (*.reg)|*.reg", FileName = capturedPathBox.Text };
            if (dlg.ShowDialog() == DialogResult.OK) capturedPathBox.Text = dlg.FileName;
        };
        regBtn.Click += (s, e) =>
        {
            capturedPathBox.Text = Path.ChangeExtension(capturedPathBox.Text, ".reg");
            ExportWin_Click(capturedPlatform, capturedTree, capturedPathBox);
        };
        exportBtn.Click += (s, e) => ExportWin_Click(capturedPlatform, capturedTree, capturedPathBox);

        bottomTable.Controls.Add(pathBox, 0, 0);
        bottomTable.Controls.Add(browseBtn, 1, 0);
        bottomTable.Controls.Add(regBtn, 2, 0);
        bottomTable.Controls.Add(exportBtn, 3, 0);
        bottomPanel.Controls.Add(bottomTable);

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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8), Name = "importLayout" };

        layout.Controls.Add(new Label
        {
            Text = "Выберите файл для импорта:", AutoSize = true,
            Font = new Font("Segoe UI", 9.5f), Margin = new Padding(0, 4, 0, 4), Name = "lblImport"
        }, 0, 0);

        var filePanel = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 34, ColumnCount = 3, RowCount = 1, Name = "filePanel" };
        _importPathTextBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle };
        _importBrowseButton = CreateStyledButton("Обзор...", Color.FromArgb(100, 110, 120), 85);
        _importBrowseButton.Click += ImportBrowse_Click;
        filePanel.Controls.Add(_importPathTextBox, 0, 0);
        filePanel.Controls.Add(_importBrowseButton, 1, 0);

        var previewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
            SplitterDistance = 220, SplitterWidth = 1, Name = "previewSplit"
        };

        _importTreeView = new TreeView
        {
            Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false,
            BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 22, ShowLines = false, Name = "importTree"
        };

        var jsonHeader = new Panel { Height = 24, Dock = DockStyle.Top, Name = "jsonHeader" };
        jsonHeader.Controls.Add(new Label
        {
            Text = "📄 Предпросмотр JSON", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Location = new Point(6, 3), AutoSize = true, Name = "lblJsonHeader"
        });

        _jsonPreviewTextBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Both, Font = new Font("Cascadia Code", 9f),
            BorderStyle = BorderStyle.None, WordWrap = false, Name = "jsonPreview"
        };

        previewSplit.Panel1.Controls.Add(_importTreeView);
        previewSplit.Panel2.Controls.Add(_jsonPreviewTextBox);
        previewSplit.Panel2.Controls.Add(jsonHeader);
        layout.Controls.Add(previewSplit, 0, 2);

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 36, FlowDirection = FlowDirection.LeftToRight, Name = "actionPanel" };
        _backupButton = CreateStyledButton("💾 Резервная копия", WarningColor, 170);
        _backupButton.Click += BackupButton_Click;
        _restoreButton = CreateStyledButton("♻ Восстановить", Color.FromArgb(180, 140, 0), 150);
        _restoreButton.Click += RestoreButton_Click;
        _importButton = CreateStyledButton("▶ Импортировать", SuccessColor, 150);
        _importButton.Click += ImportButton_Click;

        actionPanel.Controls.Add(_backupButton);
        actionPanel.Controls.Add(_restoreButton);
        actionPanel.Controls.Add(new Panel { Width = 20, Height = 1 });
        actionPanel.Controls.Add(_importButton);
        layout.Controls.Add(actionPanel, 0, 3);

        layout.Controls.Add(filePanel, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _importTab.Controls.Add(layout);
    }

    private void BuildLibraryTab()
    {
        _libraryTab.Padding = new Padding(4);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(8), Name = "libLayout" };

        var headerFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 34, FlowDirection = FlowDirection.LeftToRight, Name = "libHeader" };
        headerFlow.Controls.Add(new Label
        {
            Text = "📚 Локальная библиотека конфигов", Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, Name = "lblLibTitle"
        });

        var filterCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = new Font("Segoe UI", 9),
            Name = "filterCombo", Location = new Point(10, 4)
        };
        filterCombo.Items.AddRange(["Все платформы", "Windows 10", "Windows 11"]);
        filterCombo.SelectedIndex = 0;
        filterCombo.SelectedIndexChanged += (s, e) => RefreshLibraryList(filterCombo.SelectedIndex);

        var saveLibBtn = CreateStyledButton("➕ Сохранить", SuccessColor, 130);

        headerFlow.Controls.Add(new Panel { Width = 20, Height = 1 });
        headerFlow.Controls.Add(new Label { Text = "Фильтр:", AutoSize = true, Font = new Font("Segoe UI", 9), Location = new Point(0, 6), Name = "lblFilter" });
        headerFlow.Controls.Add(filterCombo);
        headerFlow.Controls.Add(new Panel { Width = 20, Height = 1 });
        headerFlow.Controls.Add(saveLibBtn);

        _libraryListView = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            HideSelection = false, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f), MultiSelect = false, Name = "libList"
        };
        _libraryListView.Columns.Add("Название", 150);
        _libraryListView.Columns.Add("Платформа", 90);
        _libraryListView.Columns.Add("Категорий", 70);
        _libraryListView.Columns.Add("Дата", 130);
        _libraryListView.Columns.Add("Описание", 200);
        _libraryListView.SelectedIndexChanged += LibrarySelectionChanged;

        var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Name = "libRight" };
        rightPanel.Controls.Add(new Label
        {
            Text = "Описание:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Name = "lblDesc"
        }, 0, 0);

        var descBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9),
            Name = "libraryDescBox"
        };

        var libActions = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 36, FlowDirection = FlowDirection.LeftToRight, Name = "libActions" };
        var loadLibBtn = CreateStyledButton("📥 Загрузить", PrimaryColor, 130);
        var deleteLibBtn = CreateStyledButton("🗑 Удалить", DangerColor, 110);
        var exportLibBtn = CreateStyledButton("💾 Экспорт", Color.FromArgb(80, 100, 140), 120);

        loadLibBtn.Click += LoadFromLibrary_Click;
        deleteLibBtn.Click += DeleteFromLibrary_Click;
        exportLibBtn.Click += ExportLibraryEntry_Click;
        saveLibBtn.Click += SaveToLibrary_Click;

        libActions.Controls.Add(loadLibBtn);
        libActions.Controls.Add(deleteLibBtn);
        libActions.Controls.Add(exportLibBtn);

        rightPanel.Controls.Add(descBox, 0, 1);
        rightPanel.Controls.Add(libActions, 0, 2);
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        layout.Controls.Add(headerFlow, 0, 0);
        layout.Controls.Add(new Panel(), 1, 0);
        layout.Controls.Add(_libraryListView, 0, 1);
        layout.Controls.Add(rightPanel, 1, 1);
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
        if (sender is Button btn) btn.Text = _darkMode ? "☀️" : "🌙";
        ApplyTheme();
        Refresh();
    }

    private void ApplyTheme()
    {
        if (_darkMode)
        {
            BackColor = DarkBg; ForeColor = DarkText;
            ApplyThemeToControl(this, true);
        }
        else
        {
            BackColor = Color.FromArgb(245, 247, 250); ForeColor = Color.Black;
            ApplyThemeToControl(this, false);
        }
    }

    private void ApplyThemeToControl(Control parent, bool dark)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is TabControl tc)
            {
                tc.BackColor = dark ? DarkPanel : Color.FromArgb(245, 247, 250);
                tc.ForeColor = dark ? DarkText : Color.Black;
                foreach (TabPage tp in tc.TabPages)
                    ApplyThemeToControl(tp, dark);
                continue;
            }

            if (c is TabPage tp2)
            {
                tp2.BackColor = dark ? DarkSurface : Color.White;
                tp2.ForeColor = dark ? DarkText : Color.Black;
                ApplyThemeToControl(tp2, dark);
                continue;
            }

            if (c is SplitContainer sc)
            {
                sc.BackColor = dark ? DarkBorder : Color.FromArgb(220, 224, 230);
                sc.Panel1.BackColor = dark ? DarkSurface : Color.White;
                sc.Panel2.BackColor = dark ? DarkSurface : Color.White;
                ApplyThemeToControl(sc.Panel1, dark);
                ApplyThemeToControl(sc.Panel2, dark);
                continue;
            }

            if (c is TreeView tv)
            {
                tv.BackColor = dark ? DarkPanel : Color.FromArgb(250, 251, 253);
                tv.ForeColor = dark ? DarkText : Color.Black;
                continue;
            }

            if (c is ListView lv)
            {
                lv.BackColor = dark ? DarkPanel : Color.FromArgb(250, 251, 253);
                lv.ForeColor = dark ? DarkText : Color.Black;
                continue;
            }

            if (c is TextBox tb)
            {
                tb.BackColor = dark ? DarkPanel : Color.White;
                tb.ForeColor = dark ? DarkText : Color.Black;
                if (tb.Name == "_logTextBox" || tb.Name == "jsonPreview")
                {
                    tb.BackColor = dark ? Color.FromArgb(20, 20, 24) : Color.FromArgb(30, 30, 36);
                    tb.ForeColor = dark ? Color.FromArgb(0, 200, 100) : Color.FromArgb(0, 220, 120);
                }
                continue;
            }

            if (c is ComboBox cb)
            {
                cb.BackColor = dark ? DarkPanel : Color.White;
                cb.ForeColor = dark ? DarkText : Color.Black;
                continue;
            }

            if (c is Label lbl && (lbl.Name == "lblTitle" || lbl.Name == "lblSubtitle"))
            {
                lbl.ForeColor = dark ? Color.White : Color.Black;
                if (lbl.Name == "lblSubtitle") lbl.ForeColor = dark ? Color.FromArgb(160, 170, 190) : Color.FromArgb(100, 100, 100);
                continue;
            }

            if (c is Panel p)
            {
                if (p.Parent is SplitContainer) continue;
                bool isHeader = p.Dock == DockStyle.Top && p.Height <= 60;
                if (isHeader)
                {
                    p.BackColor = dark ? Color.FromArgb(22, 24, 30) : Color.FromArgb(32, 36, 48);
                    foreach (Control pc in p.Controls)
                        if (pc is Label l) l.ForeColor = dark ? Color.FromArgb(180, 190, 210) : Color.FromArgb(200, 200, 220);
                    continue;
                }
                if (dark)
                {
                    p.BackColor = DarkSurface;
                    ApplyThemeToControl(p, dark);
                    continue;
                }
            }

            if (c is FlowLayoutPanel || c is TableLayoutPanel)
            {
                if (dark) c.BackColor = DarkSurface;
                ApplyThemeToControl(c, dark);
                continue;
            }

            if (c is Button b && b.FlatStyle == FlatStyle.Flat)
            {
                if (b.BackColor == PrimaryColor || b.BackColor == SuccessColor ||
                    b.BackColor == WarningColor || b.BackColor == DangerColor)
                    continue;
                if (dark)
                {
                    b.BackColor = DarkSurface; b.ForeColor = DarkText;
                    b.FlatAppearance.MouseOverBackColor = DarkBorder;
                }
                continue;
            }

            if (dark && c is Label && c.Parent is Panel hp && hp.Dock == DockStyle.Top && hp.Height <= 30)
                c.ForeColor = DarkText;

            if (c.HasChildren)
                ApplyThemeToControl(c, dark);
        }
    }

    // ─── Shared ───────────────────────────────────────────────────

    private static Button CreateStyledButton(string text, Color bgColor, int width)
    {
        return new Button
        {
            Text = text, Width = width, Height = 30,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = ControlPaint.Light(bgColor) },
            BackColor = bgColor, ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter, UseVisualStyleBackColor = false
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

    private void SetAllChecked(TreeNodeCollection nodes, bool state)
    {
        foreach (TreeNode n in nodes) { n.Checked = state; if (n.Nodes.Count > 0) SetAllChecked(n.Nodes, state); }
    }

    private async void ExportWin_Click(string platform, TreeView tree, TextBox pathBox)
    {
        var cats = platform == "Win11" ? _win11Categories : _win10Categories;
        foreach (TreeNode n in tree.Nodes)
        {
            if (n.Tag is SettingsCategory c) { c.IsSelected = n.Checked; if (n.Nodes.Count > 0) for (int i = 0; i < n.Nodes.Count; i++) if (n.Nodes[i].Tag is SettingsCategory s) s.IsSelected = n.Nodes[i].Checked; }
        }

        var path = pathBox.Text;
        if (string.IsNullOrWhiteSpace(path)) { MessageBox.Show("Укажите путь.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        SetControlsEnabled(false);
        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));

        try
        {
            if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
                await _exporter.ExportToRegAsync(path, cats, progress);
            else
                await _exporter.ExportAsync(path, cats, progress);
            Log($"Экспорт {platform} завершен: {path}");
            _progressBar.Value = 100;
            MessageBox.Show($"Настройки {platform} экспортированы.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private void ImportBrowse_Click(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json|REG (*.reg)|*.reg" };
        if (dlg.ShowDialog() == DialogResult.OK) { _importPathTextBox.Text = dlg.FileName; LoadImportFile(dlg.FileName); }
    }

    private async void LoadImportFile(string path)
    {
        SetControlsEnabled(false); _progressBar.Visible = true;
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
                    int shown = 0;
                    foreach (var kv in sub.Values) { if (shown++ >= 5) break; sn.Nodes.Add(new TreeNode($"{kv.Key} = {Truncate(kv.Value.Data)}")); }
                    if (sub.Values.Count > 5) sn.Nodes.Add(new TreeNode($"... ещё {sub.Values.Count - 5}"));
                    cn.Nodes.Add(sn);
                }
                _importTreeView.Nodes.Add(cn);
            }
            _importTreeView.ExpandAll();
            ShowJsonPreview(path);
            Log($"Загружен: {path}");
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private void ShowJsonPreview(string path)
    {
        try { var json = File.ReadAllText(path); _jsonPreviewTextBox.Text = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json), new JsonSerializerOptions { WriteIndented = true }); }
        catch { _jsonPreviewTextBox.Text = File.ReadAllText(path); }
    }

    private static string Truncate(string? s, int max = 60) => string.IsNullOrEmpty(s) ? "(пусто)" : s.Length > max ? s[..max] + "..." : s;

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
            it.SubItems.Add(e.Description.Length > 60 ? e.Description[..60] + "..." : e.Description);
            _libraryListView.Items.Add(it);
        }
    }

    private void LibrarySelectionChanged(object? sender, EventArgs e)
    {
        var box = _libraryTab.Controls.Find("libraryDescBox", true).FirstOrDefault() as TextBox;
        if (box == null) return;
        if (_libraryListView.SelectedItems.Count == 0) { box.Text = ""; return; }
        var id = _libraryListView.SelectedItems[0].Tag as string;
        var entry = _libraryService.GetEntries().FirstOrDefault(x => x.Id == id);
        box.Text = entry?.Description ?? "";
    }

    private void SaveToLibrary_Click(object? sender, EventArgs e)
    {
        var nameBox = new TextBox();
        var descBox = new TextBox { Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };
        var platformCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        platformCombo.Items.AddRange(["Windows 10", "Windows 11"]);
        platformCombo.SelectedIndex = 0;

        var form = new Form { Text = "Сохранить в библиотеку", Size = new Size(420, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 6 };
        layout.Controls.Add(new Label { Text = "Название:", AutoSize = true });
        layout.Controls.Add(nameBox);
        layout.Controls.Add(new Label { Text = "Описание:", AutoSize = true });
        layout.Controls.Add(descBox);
        layout.Controls.Add(new Label { Text = "Платформа:", AutoSize = true });
        layout.Controls.Add(platformCombo);

        var ok = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Width = 100, Height = 30 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
        var bp = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        bp.Controls.Add(ok); bp.Controls.Add(cancel);
        layout.Controls.Add(bp);

        form.Controls.Add(layout); form.AcceptButton = ok; form.CancelButton = cancel;
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Введите название.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var platform = platformCombo.SelectedIndex == 1 ? "Win11" : "Win10";
        var cats = platform == "Win11" ? _win11Categories : _win10Categories;

        var data = new SettingsExportData
        {
            ExportDate = DateTime.Now, WindowsVersion = _repository.GetWindowsVersion(),
            Categories = cats.Where(c => c.IsSelected).Select(c => new SettingsCategory
            {
                Name = c.Name, IsSelected = true,
                SubCategories = c.SubCategories.Where(s => s.IsSelected).Select(s => new SettingsCategory { Name = s.Name, IsSelected = true, RegistryPaths = s.RegistryPaths, Values = s.Values }).ToList()
            }).ToList()
        };

        try { _libraryService.Save(nameBox.Text.Trim(), descBox.Text.Trim(), platform, data); RefreshLibraryList(); Log($"Конфиг \"{nameBox.Text.Trim()}\" ({platform}) сохранён."); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadFromLibrary_Click(object? sender, EventArgs e)
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
                int sh = 0; foreach (var kv in sub.Values) { if (sh++ >= 5) break; sn.Nodes.Add(new TreeNode($"{kv.Key} = {Truncate(kv.Value.Data)}")); }
                if (sub.Values.Count > 5) sn.Nodes.Add(new TreeNode($"... ещё {sub.Values.Count - 5}"));
                cn.Nodes.Add(sn);
            }
            _importTreeView.Nodes.Add(cn);
        }
        _importTreeView.ExpandAll();
        _tabControl.SelectedTab = _importTab;
        Log($"Загружен из библиотеки: {_libraryListView.SelectedItems[0].Text}");
    }

    private void DeleteFromLibrary_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;
        var id = _libraryListView.SelectedItems[0].Tag as string;
        if (MessageBox.Show($"Удалить \"{_libraryListView.SelectedItems[0].Text}\"?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _libraryService.Delete(id!); RefreshLibraryList();
    }

    private void ExportLibraryEntry_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;
        var id = _libraryListView.SelectedItems[0].Tag as string;
        var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"{_libraryListView.SelectedItems[0].Text}.json" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try { _libraryService.ExportToFile(id!, dlg.FileName); Log($"Экспортирован: {dlg.FileName}"); MessageBox.Show("Готово.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ─── Backup / Import actions ──────────────────────────────────

    private async void BackupButton_Click(object? sender, EventArgs e)
    {
        SetControlsEnabled(false); _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RegistryBackup_{DateTime.Now:yyyyMMdd_HHmmss}.reg");
            await _backupService.BackupRegistryAsync(path, progress);
            Log($"Резервная копия: {path}"); _progressBar.Value = 100;
            MessageBox.Show($"Сохранено:\n{path}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private async void RestoreButton_Click(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "REG (*.reg)|*.reg" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        if (MessageBox.Show("Восстановить реестр из копии?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        SetControlsEnabled(false); _progressBar.Visible = true;
        try { await _backupService.RestoreRegistryAsync(dlg.FileName); Log($"Восстановлено: {dlg.FileName}"); MessageBox.Show("Реестр восстановлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private async void ImportButton_Click(object? sender, EventArgs e)
    {
        var path = _importPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(path)) { MessageBox.Show("Выберите файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase)) { ImportRegDirect(path); return; }
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

        SetControlsEnabled(false); _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));
        try { await _importer.ImportAsync(_loadedExportData, selected, progress); Log("Импорт завершён."); _progressBar.Value = 100; MessageBox.Show("Настройки импортированы.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { Log($"Ошибка: {ex.Message}"); MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private void ImportRegDirect(string path)
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

    private void SetControlsEnabled(bool enabled)
    {
        if (InvokeRequired) { Invoke(() => SetControlsEnabled(enabled)); return; }
    }

    private void CheckAdminRights()
    {
        try
        {
            var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            var p = new System.Security.Principal.WindowsPrincipal(id);
            Log(p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? "Права администратора." : "ВНИМАНИЕ: без прав администратора.");
        }
        catch { }
    }
}
