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
    private TabPage _exportTab;
    private TabPage _importTab;
    private TabPage _libraryTab;
    private TreeView _exportTreeView;
    private TreeView _importTreeView;
    private ListView _libraryListView;
    private TextBox _exportPathTextBox;
    private TextBox _importPathTextBox;
    private Button _exportBrowseButton;
    private Button _importBrowseButton;
    private Button _exportButton;
    private Button _importButton;
    private Button _selectAllButton;
    private Button _deselectAllButton;
    private Button _backupButton;
    private Button _restoreButton;
    private ProgressBar _progressBar;
    private TextBox _logTextBox;
    private List<SettingsCategory> _currentCategories;
    private SettingsExportData? _loadedExportData;
    private TextBox _jsonPreviewTextBox;

    private static readonly Color PrimaryColor = Color.FromArgb(0, 120, 212);
    private static readonly Color SuccessColor = Color.FromArgb(16, 124, 16);
    private static readonly Color WarningColor = Color.FromArgb(200, 120, 0);
    private static readonly Color FormBackColor = Color.FromArgb(245, 247, 250);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(220, 224, 230);

    public Form1()
    {
        _repository = new WindowsSettingsRepository();
        _exporter = new SettingsExporter(_repository);
        _importer = new SettingsImporter(_repository);
        _backupService = new RegistryBackupService();
        _libraryService = new ConfigLibraryService();
        _currentCategories = _repository.GetDefaultCategories();

        InitializeComponent();
        LoadCategoriesToTree(_exportTreeView, _currentCategories);
        RefreshLibraryList();
        CheckAdminRights();
    }

    private void InitializeComponent()
    {
        Text = "SettingsIE — Управление настройками Windows";
        Size = new Size(1000, 760);
        MinimumSize = new Size(750, 550);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = FormBackColor;
        Font = new Font("Segoe UI", 9.5f);
        Icon = SystemIcons.Application;

        var header = new Panel
        {
            Height = 64,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(32, 36, 48)
        };

        var titleLabel = new Label
        {
            Text = "⚙ SettingsIE",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 14),
            AutoSize = true
        };
        header.Controls.Add(titleLabel);

        var subtitleLabel = new Label
        {
            Text = "Импорт и экспорт настроек Windows 10/11",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(160, 170, 190),
            Location = new Point(150, 24),
            AutoSize = true
        };
        header.Controls.Add(subtitleLabel);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 500,
            SplitterWidth = 1,
            BackColor = BorderColor
        };
        mainSplit.Panel1.BackColor = FormBackColor;
        mainSplit.Panel2.BackColor = FormBackColor;

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Padding = new Point(12, 5)
        };

        _exportTab = new TabPage("📤 Экспорт");
        _importTab = new TabPage("📥 Импорт");
        _libraryTab = new TabPage("📚 Библиотека");

        BuildExportTab();
        BuildImportTab();
        BuildLibraryTab();

        _tabControl.TabPages.Add(_exportTab);
        _tabControl.TabPages.Add(_importTab);
        _tabControl.TabPages.Add(_libraryTab);

        mainSplit.Panel1.Controls.Add(_tabControl);

        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };

        var logHeaderPanel = new Panel
        {
            Height = 28,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(240, 242, 245)
        };
        logHeaderPanel.Controls.Add(new Label
        {
            Text = "📋 Журнал операций",
            Location = new Point(8, 4),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 64, 72)
        });

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Cascadia Code", 9.25f),
            BackColor = Color.FromArgb(30, 30, 36),
            ForeColor = Color.FromArgb(0, 220, 120),
            BorderStyle = BorderStyle.None
        };

        logPanel.Controls.Add(_logTextBox);
        logPanel.Controls.Add(logHeaderPanel);
        mainSplit.Panel2.Controls.Add(logPanel);

        Controls.Add(mainSplit);
        Controls.Add(header);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 4,
            Visible = false,
            Style = ProgressBarStyle.Continuous,
            ForeColor = PrimaryColor,
            BackColor = Color.FromArgb(230, 230, 230)
        };
        Controls.Add(_progressBar);
    }

    private static Button CreateStyledButton(string text, Color bgColor, int width = 140)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = ControlPaint.Light(bgColor) },
            BackColor = bgColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };
    }

    private void BuildExportTab()
    {
        _exportTab.BackColor = PanelColor;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1, RowCount = 3,
            Padding = new Padding(12),
            BackColor = PanelColor
        };

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Height = 38,
            FlowDirection = FlowDirection.LeftToRight, BackColor = PanelColor
        };

        topPanel.Controls.Add(new Label
        {
            Text = "Выберите категории для экспорта:",
            AutoSize = true, Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(80, 84, 92), Location = new Point(0, 8)
        });
        topPanel.Controls.Add(new Panel { Width = 20, Height = 1 });

        _selectAllButton = CreateStyledButton("✓ Выбрать всё", Color.FromArgb(70, 130, 200), 130);
        _selectAllButton.Click += (s, e) => SetAllNodesChecked(_exportTreeView.Nodes, true);
        _deselectAllButton = CreateStyledButton("✕ Снять всё", Color.FromArgb(140, 140, 150), 130);
        _deselectAllButton.Click += (s, e) => SetAllNodesChecked(_exportTreeView.Nodes, false);
        topPanel.Controls.Add(_selectAllButton);
        topPanel.Controls.Add(_deselectAllButton);

        _exportTreeView = new TreeView
        {
            Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253),
            Font = new Font("Segoe UI", 9.5f), ItemHeight = 24, ShowLines = false
        };
        _exportTreeView.AfterCheck += (s, e) =>
        {
            if (e.Node.Tag is SettingsCategory cat) cat.IsSelected = e.Node.Checked;
            if (e.Node.Nodes.Count > 0)
                foreach (TreeNode child in e.Node.Nodes) child.Checked = e.Node.Checked;
        };

        var bottomPanel = new Panel { Dock = DockStyle.Fill, Height = 44, BackColor = PanelColor };
        var bottomTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = PanelColor
        };

        _exportPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WindowsSettings.json"),
            Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        _exportBrowseButton = CreateStyledButton("Обзор...", Color.FromArgb(100, 110, 120), 90);
        _exportBrowseButton.Click += ExportBrowse_Click;

        _exportButton = CreateStyledButton("▶ Экспортировать", PrimaryColor, 160);
        _exportButton.Click += ExportButton_Click;

        var exportToRegButton = CreateStyledButton("Экспорт в .reg", Color.FromArgb(80, 100, 140), 140);
        exportToRegButton.Click += (s, e) =>
        {
            _exportPathTextBox.Text = Path.ChangeExtension(_exportPathTextBox.Text, ".reg");
            ExportButton_Click(s, e);
        };

        bottomTable.Controls.Add(_exportPathTextBox, 0, 0);
        bottomTable.Controls.Add(_exportBrowseButton, 1, 0);
        bottomTable.Controls.Add(exportToRegButton, 2, 0);
        bottomTable.Controls.Add(_exportButton, 3, 0);

        bottomPanel.Controls.Add(bottomTable);

        layout.Controls.Add(topPanel, 0, 0);
        layout.Controls.Add(_exportTreeView, 0, 1);
        layout.Controls.Add(bottomPanel, 0, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _exportTab.Controls.Add(layout);
    }

    private void BuildImportTab()
    {
        _importTab.BackColor = PanelColor;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(12), BackColor = PanelColor
        };

        layout.Controls.Add(new Label
        {
            Text = "Выберите файл для импорта:",
            AutoSize = true, Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(80, 84, 92), Margin = new Padding(0, 4, 0, 4)
        }, 0, 0);

        var filePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Height = 38, ColumnCount = 3, RowCount = 1, BackColor = PanelColor
        };
        _importPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9),
            BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(250, 251, 253)
        };
        _importBrowseButton = CreateStyledButton("Обзор...", Color.FromArgb(100, 110, 120), 90);
        _importBrowseButton.Click += ImportBrowse_Click;
        filePanel.Controls.Add(_importPathTextBox, 0, 0);
        filePanel.Controls.Add(_importBrowseButton, 1, 0);
        layout.Controls.Add(filePanel, 0, 1);

        var previewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
            SplitterDistance = 220, SplitterWidth = 1, BackColor = BorderColor
        };
        previewSplit.Panel1.BackColor = PanelColor;
        previewSplit.Panel2.BackColor = Color.FromArgb(40, 40, 45);

        _importTreeView = new TreeView
        {
            Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(250, 251, 253),
            Font = new Font("Segoe UI", 9.5f), ItemHeight = 22, ShowLines = false
        };

        var jsonPreviewHeader = new Panel { Height = 24, Dock = DockStyle.Top, BackColor = Color.FromArgb(55, 55, 62) };
        jsonPreviewHeader.Controls.Add(new Label
        {
            Text = "📄 Предпросмотр JSON",
            ForeColor = Color.FromArgb(180, 190, 210), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Location = new Point(6, 3), AutoSize = true
        });

        _jsonPreviewTextBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Both, Font = new Font("Cascadia Code", 9f),
            BackColor = Color.FromArgb(40, 40, 45), ForeColor = Color.FromArgb(200, 220, 240),
            BorderStyle = BorderStyle.None, WordWrap = false
        };

        previewSplit.Panel1.Controls.Add(_importTreeView);
        previewSplit.Panel2.Controls.Add(_jsonPreviewTextBox);
        previewSplit.Panel2.Controls.Add(jsonPreviewHeader);

        layout.Controls.Add(previewSplit, 0, 2);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Height = 38,
            FlowDirection = FlowDirection.LeftToRight, BackColor = PanelColor
        };

        _backupButton = CreateStyledButton("💾 Создать резервную копию", WarningColor, 200);
        _backupButton.Click += BackupButton_Click;
        _restoreButton = CreateStyledButton("♻ Восстановить из копии", Color.FromArgb(180, 140, 0), 190);
        _restoreButton.Click += RestoreButton_Click;
        _importButton = CreateStyledButton("▶ Импортировать", SuccessColor, 160);
        _importButton.Click += ImportButton_Click;

        actionPanel.Controls.Add(_backupButton);
        actionPanel.Controls.Add(_restoreButton);
        actionPanel.Controls.Add(new Panel { Width = 15, Height = 1 });
        actionPanel.Controls.Add(_importButton);
        layout.Controls.Add(actionPanel, 0, 3);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        _importTab.Controls.Add(layout);
    }

    private void BuildLibraryTab()
    {
        _libraryTab.BackColor = PanelColor;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
            Padding = new Padding(12), BackColor = PanelColor
        };

        var headerLabel = new Label
        {
            Text = "📚 Локальная библиотека конфигов",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 55, 65), AutoSize = true
        };

        var topActionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Height = 34,
            FlowDirection = FlowDirection.RightToLeft, BackColor = PanelColor
        };

        var saveLibButton = CreateStyledButton("➕ Сохранить текущий", SuccessColor, 180);
        saveLibButton.Click += SaveToLibrary_Click;
        topActionPanel.Controls.Add(saveLibButton);

        layout.Controls.Add(headerLabel, 0, 0);
        layout.Controls.Add(topActionPanel, 1, 0);

        _libraryListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details, FullRowSelect = true,
            HideSelection = false, BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253),
            Font = new Font("Segoe UI", 9.5f),
            MultiSelect = false
        };
        _libraryListView.Columns.Add("Название", 180);
        _libraryListView.Columns.Add("Категорий", 80);
        _libraryListView.Columns.Add("Дата", 140);
        _libraryListView.Columns.Add("Описание", 280);
        _libraryListView.SelectedIndexChanged += LibrarySelectionChanged;

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = PanelColor
        };

        var descLabel = new Label
        {
            Text = "Описание:", Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 84, 92), AutoSize = true
        };
        var descriptionBox = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            BackColor = Color.FromArgb(248, 249, 252),
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(40, 45, 55), Name = "libraryDescBox"
        };

        var libActionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Height = 40,
            FlowDirection = FlowDirection.LeftToRight, BackColor = PanelColor
        };

        var loadLibButton = CreateStyledButton("📥 Загрузить", PrimaryColor, 140);
        loadLibButton.Click += LoadFromLibrary_Click;

        var deleteLibButton = CreateStyledButton("🗑 Удалить", Color.FromArgb(200, 60, 60), 120);
        deleteLibButton.Click += DeleteFromLibrary_Click;

        var exportLibButton = CreateStyledButton("💾 Экспорт в файл", Color.FromArgb(80, 100, 140), 160);
        exportLibButton.Click += ExportLibraryEntry_Click;

        libActionPanel.Controls.Add(loadLibButton);
        libActionPanel.Controls.Add(deleteLibButton);
        libActionPanel.Controls.Add(exportLibButton);

        rightPanel.Controls.Add(descLabel, 0, 0);
        rightPanel.Controls.Add(descriptionBox, 0, 1);
        rightPanel.Controls.Add(libActionPanel, 0, 2);
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        layout.Controls.Add(_libraryListView, 0, 1);
        layout.Controls.Add(rightPanel, 1, 1);
        layout.Controls.Add(new Panel(), 0, 2);
        layout.Controls.Add(new Panel(), 1, 2);

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));

        _libraryTab.Controls.Add(layout);
    }

    private void LoadCategoriesToTree(TreeView treeView, List<SettingsCategory> categories)
    {
        treeView.Nodes.Clear();
        foreach (var category in categories)
        {
            var catNode = new TreeNode(category.Name) { Tag = category, Checked = category.IsSelected };
            foreach (var sub in category.SubCategories)
                catNode.Nodes.Add(new TreeNode(sub.Name) { Tag = sub, Checked = sub.IsSelected });
            treeView.Nodes.Add(catNode);
        }
        treeView.ExpandAll();
    }

    private void SetAllNodesChecked(TreeNodeCollection nodes, bool checkedState)
    {
        foreach (TreeNode node in nodes)
        {
            node.Checked = checkedState;
            if (node.Nodes.Count > 0) SetAllNodesChecked(node.Nodes, checkedState);
        }
    }

    private void SyncTreeToCategories(TreeView treeView, List<SettingsCategory> categories)
    {
        foreach (TreeNode node in treeView.Nodes)
        {
            if (node.Tag is SettingsCategory cat)
            {
                cat.IsSelected = node.Checked;
                if (node.Nodes.Count > 0)
                    for (int i = 0; i < node.Nodes.Count; i++)
                        if (node.Nodes[i].Tag is SettingsCategory sub)
                            sub.IsSelected = node.Nodes[i].Checked;
            }
        }
    }

    private void ExportBrowse_Click(object? sender, EventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON файлы (*.json)|*.json|REG файлы (*.reg)|*.reg|Все файлы (*.*)|*.*",
            FileName = _exportPathTextBox.Text
        };
        if (dlg.ShowDialog() == DialogResult.OK) _exportPathTextBox.Text = dlg.FileName;
    }

    private async void ExportButton_Click(object? sender, EventArgs e)
    {
        SyncTreeToCategories(_exportTreeView, _currentCategories);
        var path = _exportPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Укажите путь для сохранения файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetControlsEnabled(false);
        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));

        try
        {
            if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
                await _exporter.ExportToRegAsync(path, _currentCategories, progress);
            else
                await _exporter.ExportAsync(path, _currentCategories, progress);

            Log($"Экспорт завершен: {path}");
            _progressBar.Value = 100;
            MessageBox.Show($"Настройки успешно экспортированы в:\n{path}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Ошибка экспорта: {ex.Message}");
            MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetControlsEnabled(true);
            _progressBar.Visible = false;
        }
    }

    private void ImportBrowse_Click(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON файлы (*.json)|*.json|REG файлы (*.reg)|*.reg|Все файлы (*.*)|*.*"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _importPathTextBox.Text = dlg.FileName;
            LoadImportFile(dlg.FileName);
        }
    }

    private async void LoadImportFile(string filePath)
    {
        SetControlsEnabled(false);
        _progressBar.Visible = true;

        try
        {
            if (filePath.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
            {
                _importTreeView.Nodes.Clear();
                _importTreeView.Nodes.Add(new TreeNode("REG файл: " + Path.GetFileName(filePath)));
                _jsonPreviewTextBox.Text = File.ReadAllText(filePath);
                _loadedExportData = null;
                Log($"Загружен REG файл: {filePath}");
                return;
            }

            _loadedExportData = await _importer.LoadExportFileAsync(filePath);
            LoadImportCategoriesToTree(_loadedExportData);
            ShowRawJsonPreview(filePath);
            Log($"Загружен файл экспорта: {filePath} (от {_loadedExportData.ExportDate:g})");
        }
        catch (Exception ex)
        {
            Log($"Ошибка загрузки файла: {ex.Message}");
            MessageBox.Show($"Ошибка при загрузке файла:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetControlsEnabled(true);
            _progressBar.Visible = false;
        }
    }

    private void LoadImportCategoriesToTree(SettingsExportData data)
    {
        _importTreeView.Nodes.Clear();
        foreach (var category in data.Categories)
        {
            var catNode = new TreeNode($"{category.Name}  ({category.SubCategories.Count} подкатегорий)")
            {
                Tag = category, Checked = true, ForeColor = Color.FromArgb(30, 60, 120)
            };
            foreach (var sub in category.SubCategories)
            {
                var paramCount = sub.Values.Count;
                var subNode = new TreeNode($"{sub.Name}  [{paramCount} параметров]")
                {
                    Tag = sub, Checked = true, ForeColor = Color.FromArgb(20, 100, 50)
                };

                // Показываем первые 5 значений как дочерние узлы
                int shown = 0;
                foreach (var kvp in sub.Values)
                {
                    if (shown >= 5) break;
                    var valNode = new TreeNode($"{kvp.Key} = {TruncateValue(kvp.Value.Data)}")
                    {
                        ForeColor = Color.FromArgb(100, 100, 120)
                    };
                    subNode.Nodes.Add(valNode);
                    shown++;
                }
                if (sub.Values.Count > 5)
                {
                    subNode.Nodes.Add(new TreeNode($"... и ещё {sub.Values.Count - 5} параметров")
                    {
                        ForeColor = Color.Gray
                    });
                }

                catNode.Nodes.Add(subNode);
            }
            _importTreeView.Nodes.Add(catNode);
        }
        _importTreeView.ExpandAll();
    }

    private void ShowRawJsonPreview(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var obj = JsonSerializer.Deserialize<object>(json);
            _jsonPreviewTextBox.Text = JsonSerializer.Serialize(obj, options);
        }
        catch
        {
            _jsonPreviewTextBox.Text = File.ReadAllText(filePath);
        }
    }

    private static string TruncateValue(string? data, int maxLen = 60)
    {
        if (string.IsNullOrEmpty(data)) return "(пусто)";
        return data.Length > maxLen ? data[..maxLen] + "..." : data;
    }

    // ─── Library tab ──────────────────────────────────────────────

    private void RefreshLibraryList()
    {
        _libraryListView.Items.Clear();
        var entries = _libraryService.GetEntries();
        foreach (var entry in entries.OrderByDescending(e => e.Created))
        {
            var item = new ListViewItem(entry.Name) { Tag = entry.Id };
            item.SubItems.Add(entry.CategoryCount.ToString());
            item.SubItems.Add(entry.Created.ToString("dd.MM.yyyy HH:mm"));
            item.SubItems.Add(entry.Description.Length > 80
                ? entry.Description[..80] + "..."
                : entry.Description);
            _libraryListView.Items.Add(item);
        }
    }

    private void LibrarySelectionChanged(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0)
        {
            if (_libraryTab.Controls.Count > 0)
            {
                var descBox = _libraryTab.Controls.Find("libraryDescBox", true).FirstOrDefault() as TextBox;
                if (descBox != null) descBox.Text = "";
            }
            return;
        }

        var id = _libraryListView.SelectedItems[0].Tag as string;
        if (id == null) return;

        var entries = _libraryService.GetEntries();
        var entry = entries.FirstOrDefault(x => x.Id == id);
        if (entry == null) return;

        var descriptionBox = _libraryTab.Controls.Find("libraryDescBox", true).FirstOrDefault() as TextBox;
        if (descriptionBox != null) descriptionBox.Text = entry.Description;
    }

    private void SaveToLibrary_Click(object? sender, EventArgs e)
    {
        SyncTreeToCategories(_exportTreeView, _currentCategories);

        var nameBox = new TextBox();
        var descBox = new TextBox { Multiline = true, Height = 80, ScrollBars = ScrollBars.Vertical };
        var form = new Form
        {
            Text = "Сохранить в библиотеку",
            Size = new Size(420, 260),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 5 };
        layout.Controls.Add(new Label { Text = "Название:", AutoSize = true });
        layout.Controls.Add(nameBox);
        layout.Controls.Add(new Label { Text = "Описание:", AutoSize = true });
        layout.Controls.Add(descBox);

        var okBtn = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Width = 100, Height = 30, BackColor = PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var cancelBtn = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        btnPanel.Controls.Add(okBtn);
        btnPanel.Controls.Add(cancelBtn);
        layout.Controls.Add(btnPanel);

        form.Controls.Add(layout);
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;

        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text))
        {
            MessageBox.Show("Введите название.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var exportData = new SettingsExportData
        {
            ExportDate = DateTime.Now,
            WindowsVersion = _repository.GetWindowsVersion(),
            Categories = _currentCategories.Where(c => c.IsSelected).Select(c => new SettingsCategory
            {
                Name = c.Name, IsSelected = true,
                SubCategories = c.SubCategories.Where(s => s.IsSelected).Select(s => new SettingsCategory
                {
                    Name = s.Name, IsSelected = true,
                    RegistryPaths = s.RegistryPaths,
                    Values = s.Values
                }).ToList()
            }).ToList()
        };

        try
        {
            _libraryService.Save(nameBox.Text.Trim(), descBox.Text.Trim(), exportData);
            RefreshLibraryList();
            Log($"Конфиг \"{nameBox.Text.Trim()}\" сохранён в библиотеку.");
        }
        catch (Exception ex)
        {
            Log($"Ошибка сохранения: {ex.Message}");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadFromLibrary_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0)
        {
            MessageBox.Show("Выберите конфиг из списка.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var id = _libraryListView.SelectedItems[0].Tag as string;
        var data = _libraryService.Load(id);
        if (data == null)
        {
            MessageBox.Show("Не удалось загрузить конфиг.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _loadedExportData = data;
        LoadImportCategoriesToTree(data);
        _tabControl.SelectedTab = _importTab;
        Log($"Загружен конфиг из библиотеки: {_libraryListView.SelectedItems[0].Text}");
    }

    private void DeleteFromLibrary_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;

        var id = _libraryListView.SelectedItems[0].Tag as string;
        var name = _libraryListView.SelectedItems[0].Text;

        var result = MessageBox.Show($"Удалить конфиг \"{name}\"?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        _libraryService.Delete(id!);
        RefreshLibraryList();
        Log($"Конфиг \"{name}\" удалён из библиотеки.");
    }

    private void ExportLibraryEntry_Click(object? sender, EventArgs e)
    {
        if (_libraryListView.SelectedItems.Count == 0) return;

        var id = _libraryListView.SelectedItems[0].Tag as string;
        var name = _libraryListView.SelectedItems[0].Text;

        var dlg = new SaveFileDialog
        {
            Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
            FileName = $"{name}.json"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            _libraryService.ExportToFile(id!, dlg.FileName);
            Log($"Конфиг \"{name}\" экспортирован в файл: {dlg.FileName}");
            MessageBox.Show("Конфиг экспортирован.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Ошибка экспорта: {ex.Message}");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─── Backup / Import / Utils ──────────────────────────────────

    private async void BackupButton_Click(object? sender, EventArgs e)
    {
        SetControlsEnabled(false);
        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));

        try
        {
            var backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"RegistryBackup_{DateTime.Now:yyyyMMdd_HHmmss}.reg");
            await _backupService.BackupRegistryAsync(backupPath, progress);
            Log($"Резервная копия создана: {backupPath}");
            _progressBar.Value = 100;
            MessageBox.Show($"Резервная копия реестра сохранена:\n{backupPath}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Ошибка создания резервной копии: {ex.Message}");
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private async void RestoreButton_Click(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "REG файлы (*.reg)|*.reg|Все файлы (*.*)|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        if (MessageBox.Show("Восстановление реестра может изменить системные настройки. Продолжить?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        SetControlsEnabled(false);
        _progressBar.Visible = true;
        try
        {
            await _backupService.RestoreRegistryAsync(dlg.FileName);
            Log($"Восстановление из копии выполнено: {dlg.FileName}");
            MessageBox.Show("Реестр восстановлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Ошибка восстановления: {ex.Message}");
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private async void ImportButton_Click(object? sender, EventArgs e)
    {
        var path = _importPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Выберите файл для импорта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (path.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
        {
            ImportRegFileDirect(path);
            return;
        }

        if (_loadedExportData == null)
        {
            MessageBox.Show("Не удалось загрузить данные из файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var selectedCategories = GetSelectedCategoriesFromTree(_importTreeView);
        if (selectedCategories.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одну категорию для импорта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("Импорт изменит параметры реестра. Рекомендуется создать резервную копию. Продолжить?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        SetControlsEnabled(false);
        _progressBar.Visible = true; _progressBar.Value = 0;
        var progress = new Progress<int>(v => _progressBar.Value = Math.Min(v, 100));

        try
        {
            await _importer.ImportAsync(_loadedExportData, selectedCategories, progress);
            Log("Импорт настроек завершен успешно.");
            _progressBar.Value = 100;
            MessageBox.Show("Настройки успешно импортированы.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Ошибка импорта: {ex.Message}");
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetControlsEnabled(true); _progressBar.Visible = false; }
    }

    private void ImportRegFileDirect(string regFilePath)
    {
        if (MessageBox.Show($"Импортировать REG файл?\n{regFilePath}",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"import \"{regFilePath}\"",
                UseShellExecute = false, CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                Log($"REG файл импортирован: {regFilePath}");
                MessageBox.Show("REG файл успешно импортирован.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log($"Ошибка импорта REG файла (код: {process?.ExitCode})");
                MessageBox.Show("Ошибка при импорте REG файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка импорта REG: {ex.Message}");
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private List<SettingsCategory> GetSelectedCategoriesFromTree(TreeView treeView)
    {
        var result = new List<SettingsCategory>();
        foreach (TreeNode node in treeView.Nodes)
        {
            if (node.Tag is SettingsCategory cat && node.Checked)
            {
                var selectedCat = new SettingsCategory { Name = cat.Name, IsSelected = true, SubCategories = new() };
                if (node.Nodes.Count > 0)
                {
                    foreach (TreeNode child in node.Nodes)
                    {
                        if (child.Tag is SettingsCategory sub && child.Checked)
                            selectedCat.SubCategories.Add(new SettingsCategory
                            {
                                Name = sub.Name, IsSelected = true,
                                RegistryPaths = sub.RegistryPaths, Values = sub.Values
                            });
                    }
                }
                result.Add(selectedCat);
            }
        }
        return result;
    }

    private void Log(string message)
    {
        if (_logTextBox.InvokeRequired) { _logTextBox.Invoke(() => Log(message)); return; }
        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _logTextBox.ScrollToCaret();
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (InvokeRequired) { Invoke(() => SetControlsEnabled(enabled)); return; }
        _exportButton.Enabled = enabled;
        _importButton.Enabled = enabled;
        _backupButton.Enabled = enabled;
        _restoreButton.Enabled = enabled;
        _selectAllButton.Enabled = enabled;
        _deselectAllButton.Enabled = enabled;
        _exportBrowseButton.Enabled = enabled;
        _importBrowseButton.Enabled = enabled;
        _exportTreeView.Enabled = enabled;
        _importTreeView.Enabled = enabled;
    }

    private void CheckAdminRights()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            Log(isAdmin
                ? "Программа запущена с правами администратора."
                : "ВНИМАНИЕ: Программа запущена без прав администратора. Некоторые функции могут быть недоступны.");
        }
        catch (Exception ex) { Log($"Не удалось проверить права: {ex.Message}"); }
    }
}
