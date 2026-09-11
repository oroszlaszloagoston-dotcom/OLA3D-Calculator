using System.ComponentModel;
using System.Globalization;

namespace OLA3D.Calculator;

using static OLA3D.Calculator.Localizer;

public sealed class MainForm : Form
{
    private sealed class OptionItem
    {
        public string Text { get; }
        public decimal Value { get; }
        public OptionItem(string text, decimal value) { Text = text; Value = value; }
        public override string ToString() => Text;
    }

    private sealed class SettingBinding
    {
        public NumericUpDown Control { get; }
        public Func<decimal> Getter { get; }
        public Action<decimal> Setter { get; }
        public SettingBinding(NumericUpDown control, Func<decimal> getter, Action<decimal> setter)
        {
            Control = control;
            Getter = getter;
            Setter = setter;
        }
    }

    private readonly CultureInfo _culture = Localizer.Culture;
    private AppSettings _settings = SettingsStore.Load();
    private readonly List<SettingBinding> _settingBindings = new();
    private BindingList<MaterialDefinition> _materialEditList = new();

    private readonly Color _accent = Color.FromArgb(32, 99, 155);
    private readonly Color _soft = Color.FromArgb(245, 247, 249);
    private readonly Color _border = Color.FromArgb(218, 223, 228);
    private readonly Color _muted = Color.FromArgb(90, 98, 108);
    private readonly Color _text = Color.FromArgb(35, 41, 47);

    // Design calculator controls
    private ComboBox cmbBaseHours = null!;
    private ComboBox cmbFunction = null!;
    private ComboBox cmbSource = null!;
    private ComboBox cmbPrecision = null!;
    private ComboBox cmbInterfaces = null!;
    private CheckBox chkScan = null!;
    private NumericUpDown nudScanCount = null!;
    private ComboBox cmbScanDifficulty = null!;
    private NumericUpDown nudRevisions = null!;
    private ComboBox cmbRush = null!;
    private CheckBox chkDrawing = null!;
    private CheckBox chkDfm = null!;
    private CheckBox chkOnsite = null!;
    private NumericUpDown nudDistance = null!;
    private ComboBox cmbBuffer = null!;
    private Label lblDesignFinal = null!;
    private Label lblDesignRange = null!;
    private Label lblDesignHours = null!;
    private Label lblDesignTravel = null!;
    private Label lblDesignBreakdown = null!;

    // Print calculator controls
    private NumericUpDown nudPrintHours = null!;
    private DataGridView dgvPrintMaterials = null!;
    private Label lblPrintFinal = null!;
    private Label lblPrintRange = null!;
    private Label lblPrintMaterialCost = null!;
    private Label lblPrintTimeCost = null!;
    private Label lblPrintBreakdown = null!;

    // Settings controls
    private DataGridView dgvMaterialCatalog = null!;
    private Label lblSettingsStatus = null!;

    public MainForm()
    {
        Text = "OLA3D Calculator 1.0";
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        Size = new Size(1360, 900);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.White;

        BuildUi();
        LoadSettingsIntoSettingsUi();
        RefreshDesignOptions(false);
        RefreshPrintMaterialColumn(false);
        ResetCalculatorInputs();
        WireEvents();
        RecalculateDesign();
        RecalculatePrint();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = _accent, Padding = new Padding(24, 10, 24, 10) };
        header.Controls.Add(new Label
        {
            Text = "OLA3D Calculator 1.0 — OLA3D",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F),
            AutoSize = true,
            Location = new Point(22, 9)
        });
        header.Controls.Add(new Label
        {
            Text = T("Tervezési és többanyagú nyomtatási ajánlatok – a díjszabás külön Beállítások menüben kezelhető.", "Design and multi-material printing quotes. Configure rates in Settings."),
            ForeColor = Color.FromArgb(228, 238, 247),
            Font = new Font("Segoe UI", 9.5F),
            AutoSize = true,
            Location = new Point(24, 45)
        });
        header.Controls.Add(new Label
        {
            Text = T("Pénznem: ", "Currency: ") + SettingsStore.Currency + T(" • A díjak a Beállításokban módosíthatók.", " • Edit rates in Settings."),
            AutoSize = true, ForeColor = Color.White, Location = new Point(24, 82)
        });
        root.Controls.Add(header, 0, 0);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F),
            Padding = new Point(16, 6)
        };
        root.Controls.Add(tabs, 0, 1);

        var designTab = new TabPage(T("Tervezés", "Design")) { BackColor = Color.White, Padding = new Padding(12) };
        var printTab = new TabPage(T("Nyomtatás", "Printing")) { BackColor = Color.White, Padding = new Padding(12) };
        var settingsTab = new TabPage(T("Beállítások", "Settings")) { BackColor = Color.White, Padding = new Padding(12) };
        tabs.TabPages.Add(designTab);
        tabs.TabPages.Add(printTab);
        tabs.TabPages.Add(settingsTab);

        BuildDesignTab(designTab);
        BuildPrintTab(printTab);
        BuildSettingsTab(settingsTab);
    }

    private void BuildDesignTab(TabPage tab)
    {
        var main = TwoColumnLayout(62, 38);
        tab.Controls.Add(main);

        var leftScroll = ScrollHost();
        var left = AutoStack();
        leftScroll.Controls.Add(left);
        main.Controls.Add(leftScroll, 0, 0);

        var rightScroll = ScrollHost();
        var right = AutoStack();
        rightScroll.Controls.Add(right);
        main.Controls.Add(rightScroll, 1, 0);

        var project = CreateSection(T("Projekt", "Project"), T("Csak a munka jellemzőit add meg. A mögöttes óradíjak és szorzók a Beállítások fülön vannak.", "Enter the project details. Hourly rates and multipliers are available in Settings."));
        cmbBaseHours = CreateCombo();
        cmbFunction = CreateCombo();
        cmbSource = CreateCombo();
        cmbPrecision = CreateCombo();
        cmbInterfaces = CreateCombo();
        AddFullField(project.Table, 0, T("Geometriai bonyolultság", "Geometry complexity"), cmbBaseHours);
        AddFullField(project.Table, 1, T("Alkatrész jellege", "Part function"), cmbFunction);
        AddFullField(project.Table, 2, T("Kiindulási adat", "Source data"), cmbSource);
        AddFullField(project.Table, 3, T("Pontossági igény", "Precision requirements"), cmbPrecision);
        AddFullField(project.Table, 4, T("Illeszkedő kapcsolatok", "Mating interfaces"), cmbInterfaces);
        AddSection(left, project.Group);

        var scan = CreateSection(T("3D szkennelés", "3D scanning"), T("A szkennelés külön időt ad a tervezéshez. Több referenciaalkatrész is megadható.", "Scanning adds time to the design estimate. You can include multiple reference parts."));
        chkScan = new CheckBox { Text = T("3D szkennelés szükséges", "3D scanning required"), AutoSize = true, Padding = new Padding(0, 5, 0, 5) };
        nudScanCount = CreateNumeric(1, 50, 1, 1, 0);
        cmbScanDifficulty = CreateCombo();
        AddFullField(scan.Table, 0, "", chkScan);
        AddField(scan.Table, 1, T("Szkennelendő darabok", "Parts to scan"), nudScanCount, T("db", "pcs"));
        AddFullField(scan.Table, 2, T("Szkennelés nehézsége", "Scanning difficulty"), cmbScanDifficulty);
        AddSection(left, scan.Group);

        var extras = CreateSection(T("Kiegészítő igények", "Additional requirements"), T("Egy kisebb korrekciót az alap kalkuláció tartalmaz; az extra módosítások külön adhatók meg.", "The base estimate includes one minor revision. Add further revisions below."));
        nudRevisions = CreateNumeric(0, 20, 1, 0, 0);
        cmbRush = CreateCombo();
        chkDrawing = new CheckBox { Text = T("2D műszaki rajz is szükséges", "2D technical drawing required"), AutoSize = true };
        chkDfm = new CheckBox { Text = T("Gyárthatósági / nyomtathatósági optimalizálás", "Manufacturing / printing optimization"), AutoSize = true };
        chkOnsite = new CheckBox { Text = T("Helyszíni felmérés szükséges", "On-site survey required"), AutoSize = true };
        nudDistance = CreateNumeric(0, 1000, 1, 10, 0);
        cmbBuffer = CreateCombo();
        AddField(extras.Table, 0, T("További módosítási körök", "Additional revision rounds"), nudRevisions, T("db", "pcs"));
        AddFullField(extras.Table, 1, T("Határidő", "Deadline"), cmbRush);
        AddFullField(extras.Table, 2, "", chkDrawing);
        AddFullField(extras.Table, 3, "", chkDfm);
        AddFullField(extras.Table, 4, "", chkOnsite);
        AddField(extras.Table, 5, T("Távolság egy irányba", "One-way distance"), nudDistance, "km");
        AddFullField(extras.Table, 6, T("Projekt bizonytalansága", "Project uncertainty"), cmbBuffer);
        AddSection(left, extras.Group);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 2, 0, 10) };
        var resetCurrent = SecondaryButton(T("Űrlap alaphelyzet", "Reset form"));
        resetCurrent.Click += (_, _) => { ResetDesignInputs(); RecalculateDesign(); };
        buttons.Controls.Add(resetCurrent);
        AddSection(left, buttons);

        var result = CreateCard();
        var resultFlow = CardFlow();
        result.Controls.Add(resultFlow);
        resultFlow.Controls.Add(Caption(T("AJÁNLOTT FIX TERVEZÉSI ÁR", "RECOMMENDED FIXED DESIGN PRICE")));
        lblDesignFinal = BigPriceLabel();
        resultFlow.Controls.Add(lblDesignFinal);
        lblDesignRange = MutedLabel(430);
        resultFlow.Controls.Add(lblDesignRange);
        var metrics = MetricTable();
        lblDesignHours = MetricLabel();
        lblDesignTravel = MetricLabel();
        metrics.Controls.Add(lblDesignHours, 0, 0);
        metrics.Controls.Add(lblDesignTravel, 1, 0);
        resultFlow.Controls.Add(metrics);
        AddSection(right, result);

        var details = CreateCard();
        var detailsFlow = CardFlow();
        details.Controls.Add(detailsFlow);
        detailsFlow.Controls.Add(MakeHeading(T("Kalkuláció részletei", "Calculation details")));
        lblDesignBreakdown = MonospaceLabel(440);
        detailsFlow.Controls.Add(lblDesignBreakdown);
        AddSection(right, details);

        var info = CreateCard();
        var infoFlow = CardFlow();
        info.Controls.Add(infoFlow);
        infoFlow.Controls.Add(MakeHeading(T("Árazási elv", "Pricing approach")));
        infoFlow.Controls.Add(MutedText(T("A főképernyőn nem jelennek meg a szorzók. Az ügyfélnek érdemes egy fix projektárat kommunikálni; a belső díjszabást a Beállítások fülön tudod módosítani.", "Quote a fixed project price to your customer. Adjust internal rates and multipliers in Settings.")));
        AddSection(right, info);
    }

    private void BuildPrintTab(TabPage tab)
    {
        var main = TwoColumnLayout(65, 35);
        tab.Controls.Add(main);

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(4)
        };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.Controls.Add(left, 0, 0);

        var basic = CreateSection(T("Nyomtatási adatok", "Print details"), T("A nyomtatási idő legyen a slicerből kapott teljes gépidő. Az anyagokat külön sorokban add hozzá.", "Use the total print time from your slicer. Add each material on a separate row."));
        nudPrintHours = CreateNumeric(0, 1000, 0.1m, 1, 2);
        AddField(basic.Table, 0, T("Nyomtatási idő", "Print time"), nudPrintHours, T("óra", "hours"));
        left.Controls.Add(basic.Group, 0, 0);

        var materialsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        var materialsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        materialsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        materialsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        materialsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        materialsPanel.Controls.Add(materialsLayout);
        materialsLayout.Controls.Add(new Label
        {
            Text = T("Felhasznált anyagok", "Materials used"),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = _text,
            Margin = new Padding(2, 0, 0, 8)
        }, 0, 0);

        dgvPrintMaterials = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        var materialColumn = new DataGridViewComboBoxColumn
        {
            Name = "MaterialId",
            HeaderText = T("Anyag", "Material"),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat,
            FillWeight = 34
        };
        var gramsColumn = new DataGridViewTextBoxColumn { Name = "Grams", HeaderText = T("Tömeg (g)", "Weight (g)"), FillWeight = 18 };
        var rawColumn = new DataGridViewTextBoxColumn { Name = "RawCost", HeaderText = T("Nyers anyagköltség", "Raw material cost"), ReadOnly = true, FillWeight = 24 };
        var billedColumn = new DataGridViewTextBoxColumn { Name = "BilledCost", HeaderText = T("Kalkulált anyagdíj", "Calculated material charge"), ReadOnly = true, FillWeight = 24 };
        dgvPrintMaterials.Columns.AddRange(materialColumn, gramsColumn, rawColumn, billedColumn);
        materialsLayout.Controls.Add(dgvPrintMaterials, 0, 1);

        var materialButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 0) };
        var addMaterial = PrimaryButton(T("+ Anyag hozzáadása", "+ Add material"));
        var removeMaterial = SecondaryButton(T("Kijelölt sor törlése", "Remove selected row"));
        addMaterial.Click += (_, _) => AddPrintMaterialRow();
        removeMaterial.Click += (_, _) => RemoveSelectedPrintMaterialRow();
        materialButtons.Controls.Add(addMaterial);
        materialButtons.Controls.Add(removeMaterial);
        materialsLayout.Controls.Add(materialButtons, 0, 2);
        left.Controls.Add(materialsPanel, 0, 1);

        var printButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        var resetPrint = SecondaryButton(T("Nyomtatási kalkulátor alaphelyzet", "Reset print calculator"));
        resetPrint.Click += (_, _) => { ResetPrintInputs(); RecalculatePrint(); };
        printButtons.Controls.Add(resetPrint);
        left.Controls.Add(printButtons, 0, 2);

        var rightScroll = ScrollHost();
        var right = AutoStack();
        rightScroll.Controls.Add(right);
        main.Controls.Add(rightScroll, 1, 0);

        var result = CreateCard();
        var flow = CardFlow();
        result.Controls.Add(flow);
        flow.Controls.Add(Caption(T("AJÁNLOTT NYOMTATÁSI ÁR", "RECOMMENDED PRINT PRICE")));
        lblPrintFinal = BigPriceLabel();
        flow.Controls.Add(lblPrintFinal);
        lblPrintRange = MutedLabel(430);
        flow.Controls.Add(lblPrintRange);
        var metrics = MetricTable();
        lblPrintMaterialCost = MetricLabel();
        lblPrintTimeCost = MetricLabel();
        metrics.Controls.Add(lblPrintMaterialCost, 0, 0);
        metrics.Controls.Add(lblPrintTimeCost, 1, 0);
        flow.Controls.Add(metrics);
        AddSection(right, result);

        var formula = CreateCard();
        var formulaFlow = CardFlow();
        formula.Controls.Add(formulaFlow);
        formulaFlow.Controls.Add(MakeHeading(T("Számítás", "Formula")));
        formulaFlow.Controls.Add(new Label
        {
            Text = T("Σ[(tömeg / 1000) × Ft/kg × anyagszorzó × veszteségi szorzó]\n+ nyomtatási idő × gépóradíj\n→ minimumdíj és kerekítés", "Σ[(weight / 1000) × HUF/kg × material factor × waste factor]\n+ print time × machine hourly rate\n→ minimum charge and rounding"),
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            BackColor = _soft,
            Padding = new Padding(12),
            Font = new Font("Segoe UI Semibold", 9.5F)
        });
        AddSection(right, formula);

        var details = CreateCard();
        var detailsFlow = CardFlow();
        details.Controls.Add(detailsFlow);
        detailsFlow.Controls.Add(MakeHeading(T("Kalkuláció részletei", "Calculation details")));
        lblPrintBreakdown = MonospaceLabel(440);
        detailsFlow.Controls.Add(lblPrintBreakdown);
        AddSection(right, details);

        var hint = CreateCard();
        var hintFlow = CardFlow();
        hint.Controls.Add(hintFlow);
        hintFlow.Controls.Add(MakeHeading(T("Többanyagú nyomtatás", "Multi-material printing")));
        hintFlow.Controls.Add(MutedText(T("Minden felhasznált anyagot külön sorban adj meg. Így például PLA + support anyag, vagy két különböző filament egyszerre is számolható. Az anyagkatalógus a Beállítások fülön bővíthető.", "Enter each material on a separate row, for example PLA and support, or two different filaments. Extend the material catalog in Settings.")));
        AddSection(right, hint);
    }

    private void BuildSettingsTab(TabPage tab)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tab.Controls.Add(root);

        var innerTabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 5), Font = new Font("Segoe UI Semibold", 9.8F) };
        var designSettings = new TabPage(T("Tervezési díjszabás", "Design pricing")) { BackColor = Color.White };
        var printSettings = new TabPage(T("Nyomtatás és anyagkatalógus", "Printing and material catalog")) { BackColor = Color.White };
        innerTabs.TabPages.Add(designSettings);
        innerTabs.TabPages.Add(printSettings);
        root.Controls.Add(innerTabs, 0, 0);

        BuildDesignSettingsPage(designSettings);
        BuildPrintSettingsPage(printSettings);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8, 10, 8, 4) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        lblSettingsStatus = new Label
        {
            Text = $"{T("A mentett beállítások helye: ", "Settings file: ")}{SettingsStore.SettingsPath}",
            AutoSize = true,
            ForeColor = _muted,
            Anchor = AnchorStyles.Left,
            MaximumSize = new Size(720, 0)
        };
        bottom.Controls.Add(lblSettingsStatus, 0, 0);
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var factoryReset = SecondaryButton(T("Gyári alaphelyzet", "Factory reset"));
        var save = PrimaryButton(T("Beállítások mentése", "Save settings"));
        factoryReset.Click += (_, _) => FactoryReset();
        save.Click += (_, _) => SaveSettingsFromUi();
        actions.Controls.Add(factoryReset);
        actions.Controls.Add(save);
        bottom.Controls.Add(actions, 1, 0);
        root.Controls.Add(bottom, 0, 1);
    }

    private void BuildDesignSettingsPage(TabPage page)
    {
        var scroll = ScrollHost();
        page.Controls.Add(scroll);
        var stack = AutoStack();
        scroll.Controls.Add(stack);

        var d = _settings.Design;

        var general = CreateSettingsSection(T("Alapdíjak", "Base rates"), T("Ezek adják a tervezési kalkuláció pénzügyi alapját.", "These rates form the basis of design pricing."));
        AddSetting(general.Table, 0, T("CAD óradíj", "CAD hourly rate"), T("Ft/óra", "HUF/hour"), () => _settings.Design.HourlyRate, v => _settings.Design.HourlyRate = v, 0, 1000, 100000, 500);
        AddSetting(general.Table, 1, T("Minimum tervezési díj", "Minimum design fee"), T("Ft", "HUF"), () => _settings.Design.MinimumFee, v => _settings.Design.MinimumFee = v, 0, 0, 200000, 500);
        AddSetting(general.Table, 2, T("Árkerekítés", "Price rounding step"), T("Ft", "HUF"), () => _settings.Design.RoundingStep, v => _settings.Design.RoundingStep = v, 0, 1, 10000, 100);
        AddSetting(general.Table, 3, T("Ajánlati sáv alsó szorzó", "Quote range lower factor"), "×", () => _settings.Design.QuoteLowFactor, v => _settings.Design.QuoteLowFactor = v, 2, 0.1m, 2m, 0.01m);
        AddSetting(general.Table, 4, T("Ajánlati sáv felső szorzó", "Quote range upper factor"), "×", () => _settings.Design.QuoteHighFactor, v => _settings.Design.QuoteHighFactor = v, 2, 0.1m, 3m, 0.01m);
        AddSection(stack, general.Group);

        var baseHours = CreateSettingsSection(T("Alap modellezési idők", "Base modeling times"), T("A projekt geometriai nehézségéhez tartozó kiinduló munkaidő.", "Starting work time based on geometry complexity."));
        AddSetting(baseHours.Table, 0, T("Nagyon egyszerű", "Very simple"), T("óra", "hours"), () => _settings.Design.BaseVerySimpleHours, v => _settings.Design.BaseVerySimpleHours = v, 2, 0, 50, 0.05m);
        AddSetting(baseHours.Table, 1, T("Egyszerű", "Simple"), T("óra", "hours"), () => _settings.Design.BaseSimpleHours, v => _settings.Design.BaseSimpleHours = v, 2, 0, 50, 0.05m);
        AddSetting(baseHours.Table, 2, T("Közepes", "Medium"), T("óra", "hours"), () => _settings.Design.BaseMediumHours, v => _settings.Design.BaseMediumHours = v, 2, 0, 100, 0.1m);
        AddSetting(baseHours.Table, 3, T("Összetett", "Complex"), T("óra", "hours"), () => _settings.Design.BaseComplexHours, v => _settings.Design.BaseComplexHours = v, 2, 0, 200, 0.1m);
        AddSetting(baseHours.Table, 4, T("Nagyon összetett / szabadformás", "Very complex / freeform"), T("óra", "hours"), () => _settings.Design.BaseVeryComplexHours, v => _settings.Design.BaseVeryComplexHours = v, 2, 0, 300, 0.1m);
        AddSection(stack, baseHours.Group);

        var factors1 = CreateSettingsSection(T("Funkció és kiindulási adat szorzói", "Function and source data factors"), T("A szorzók az alap modellezési időt módosítják.", "These factors adjust the base modeling time."));
        AddSetting(factors1.Table, 0, T("Dekoráció / vizuális elem", "Decorative / visual element"), "×", () => _settings.Design.FunctionDecorationFactor, v => _settings.Design.FunctionDecorationFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 1, T("Burkolat / takaróelem", "Enclosure / cover"), "×", () => _settings.Design.FunctionCoverFactor, v => _settings.Design.FunctionCoverFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 2, T("Funkcionális alkatrész", "Functional part"), "×", () => _settings.Design.FunctionFunctionalFactor, v => _settings.Design.FunctionFunctionalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 3, T("Mechanikailag terhelt", "Mechanically loaded"), "×", () => _settings.Design.FunctionLoadedFactor, v => _settings.Design.FunctionLoadedFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 4, T("Pontos rajz / használható CAD", "Accurate drawing / usable CAD"), "×", () => _settings.Design.SourceDrawingFactor, v => _settings.Design.SourceDrawingFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 5, T("Fizikai mintadarab", "Physical sample"), "×", () => _settings.Design.SourcePhysicalFactor, v => _settings.Design.SourcePhysicalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 6, T("Kapott scan / mesh", "Provided scan / mesh"), "×", () => _settings.Design.SourceMeshFactor, v => _settings.Design.SourceMeshFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors1.Table, 7, T("Csak fotó / kevés méret", "Photos only / few dimensions"), "×", () => _settings.Design.SourcePhotoFactor, v => _settings.Design.SourcePhotoFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSection(stack, factors1.Group);

        var factors2 = CreateSettingsSection(T("Pontosság és illesztések szorzói", "Precision and interface factors"), T("Az illeszkedő kapcsolatok a próbák és korrekciók kockázatát modellezik.", "Mating interfaces account for the risk of trial fits and revisions."));
        AddSetting(factors2.Table, 0, T("Normál pontosság", "Standard precision"), "×", () => _settings.Design.PrecisionNormalFactor, v => _settings.Design.PrecisionNormalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 1, T("Illesztési méretek fontosak", "Critical mating dimensions"), "×", () => _settings.Design.PrecisionFitFactor, v => _settings.Design.PrecisionFitFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 2, T("Sok kritikus méret / nagy pontosság", "Many critical dimensions / high precision"), "×", () => _settings.Design.PrecisionHighFactor, v => _settings.Design.PrecisionHighFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 3, T("0 illeszkedés", "0 interfaces"), "×", () => _settings.Design.Interface0Factor, v => _settings.Design.Interface0Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 4, T("1 illeszkedés", "1 interface"), "×", () => _settings.Design.Interface1Factor, v => _settings.Design.Interface1Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 5, T("2 illeszkedés", "2 interfaces"), "×", () => _settings.Design.Interface2Factor, v => _settings.Design.Interface2Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 6, T("3 illeszkedés", "3 interfaces"), "×", () => _settings.Design.Interface3Factor, v => _settings.Design.Interface3Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 7, T("4 illeszkedés", "4 interfaces"), "×", () => _settings.Design.Interface4Factor, v => _settings.Design.Interface4Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(factors2.Table, 8, T("5 vagy több illeszkedés", "5 or more interfaces"), "×", () => _settings.Design.Interface5PlusFactor, v => _settings.Design.Interface5PlusFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSection(stack, factors2.Group);

        var scan = CreateSettingsSection(T("Szkennelés és extra munka", "Scanning and additional work"), T("Időtételek és szkennelési nehézségi szorzók.", "Time allowances and scanning difficulty factors."));
        AddSetting(scan.Table, 0, T("Első szkennelendő tárgy", "First object to scan"), T("óra", "hours"), () => _settings.Design.ScanFirstHours, v => _settings.Design.ScanFirstHours = v, 2, 0, 50, 0.05m);
        AddSetting(scan.Table, 1, T("Minden további tárgy", "Each additional object"), T("óra", "hours"), () => _settings.Design.ScanAdditionalHours, v => _settings.Design.ScanAdditionalHours = v, 2, 0, 50, 0.05m);
        AddSetting(scan.Table, 2, T("Normál szkennelés", "Standard scanning"), "×", () => _settings.Design.ScanNormalFactor, v => _settings.Design.ScanNormalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(scan.Table, 3, T("Fényes/fekete/spray felület", "Glossy / black / sprayed surface"), "×", () => _settings.Design.ScanDifficultSurfaceFactor, v => _settings.Design.ScanDifficultSurfaceFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(scan.Table, 4, T("Komplex / takart felület", "Complex / obscured surface"), "×", () => _settings.Design.ScanComplexFactor, v => _settings.Design.ScanComplexFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(scan.Table, 5, T("Extra módosítási kör", "Additional revision round"), T("óra/db", "hours/pc"), () => _settings.Design.RevisionHours, v => _settings.Design.RevisionHours = v, 2, 0, 50, 0.05m);
        AddSetting(scan.Table, 6, T("2D műszaki rajz", "2D technical drawing"), T("óra", "hours"), () => _settings.Design.DrawingHours, v => _settings.Design.DrawingHours = v, 2, 0, 100, 0.05m);
        AddSetting(scan.Table, 7, T("DFM / nyomtatási optimalizálás", "DFM / print optimization"), T("óra", "hours"), () => _settings.Design.DfmHours, v => _settings.Design.DfmHours = v, 2, 0, 100, 0.05m);
        AddSection(stack, scan.Group);

        var commercial = CreateSettingsSection(T("Sürgősség, tartalék és kiszállás", "Urgency, contingency and travel"), T("Az árképzés végén alkalmazott szorzók és helyszíni költségek.", "Final pricing factors and on-site costs."));
        AddSetting(commercial.Table, 0, T("Normál határidő", "Standard deadline"), "×", () => _settings.Design.RushNormalFactor, v => _settings.Design.RushNormalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 1, T("48 órás határidő", "48-hour deadline"), "×", () => _settings.Design.Rush48Factor, v => _settings.Design.Rush48Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 2, T("24 órás határidő", "24-hour deadline"), "×", () => _settings.Design.Rush24Factor, v => _settings.Design.Rush24Factor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 3, T("Jól definiált projekt", "Well-defined project"), "×", () => _settings.Design.BufferDefinedFactor, v => _settings.Design.BufferDefinedFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 4, T("Normál egyedi projekt", "Standard custom project"), "×", () => _settings.Design.BufferNormalFactor, v => _settings.Design.BufferNormalFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 5, T("Bizonytalan geometria", "Uncertain geometry"), "×", () => _settings.Design.BufferUncertainFactor, v => _settings.Design.BufferUncertainFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 6, T("Magas kockázat / kevés információ", "High risk / limited information"), "×", () => _settings.Design.BufferHighRiskFactor, v => _settings.Design.BufferHighRiskFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(commercial.Table, 7, T("Kiszállási alapdíj", "Travel base fee"), T("Ft", "HUF"), () => _settings.Design.TravelBaseFee, v => _settings.Design.TravelBaseFee = v, 0, 0, 200000, 500);
        AddSetting(commercial.Table, 8, T("Kiszállási kilométerdíj", "Travel fee per kilometer"), T("Ft/km", "HUF/km"), () => _settings.Design.TravelPerKm, v => _settings.Design.TravelPerKm = v, 0, 0, 5000, 10);
        AddSection(stack, commercial.Group);
    }

    private void BuildPrintSettingsPage(TabPage page)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);

        var pricing = CreateSettingsSection(T("Nyomtatási díjszabás", "Print pricing"), T("A gépóradíj tartalmazhatja az áramot, amortizációt, karbantartást és a nyomtató lekötött idejét.", "The machine rate can include electricity, depreciation, maintenance and occupied machine time."));
        AddSetting(pricing.Table, 0, T("Gépóradíj", "Machine hourly rate"), T("Ft/óra", "HUF/hour"), () => _settings.Print.MachineHourlyRate, v => _settings.Print.MachineHourlyRate = v, 0, 0, 100000, 100);
        AddSetting(pricing.Table, 1, T("Minimum nyomtatási díj", "Minimum print fee"), T("Ft", "HUF"), () => _settings.Print.MinimumFee, v => _settings.Print.MinimumFee = v, 0, 0, 200000, 500);
        AddSetting(pricing.Table, 2, T("Anyagveszteségi szorzó", "Material waste factor"), "×", () => _settings.Print.WasteFactor, v => _settings.Print.WasteFactor = v, 2, 0.1m, 5m, 0.01m);
        AddSetting(pricing.Table, 3, T("Árkerekítés", "Price rounding step"), T("Ft", "HUF"), () => _settings.Print.RoundingStep, v => _settings.Print.RoundingStep = v, 0, 1, 10000, 100);
        AddSetting(pricing.Table, 4, T("Ajánlati sáv alsó szorzó", "Quote range lower factor"), "×", () => _settings.Print.QuoteLowFactor, v => _settings.Print.QuoteLowFactor = v, 2, 0.1m, 2m, 0.01m);
        AddSetting(pricing.Table, 5, T("Ajánlati sáv felső szorzó", "Quote range upper factor"), "×", () => _settings.Print.QuoteHighFactor, v => _settings.Print.QuoteHighFactor = v, 2, 0.1m, 3m, 0.01m);
        root.Controls.Add(pricing.Group, 0, 0);

        var catalogPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(0, 10, 0, 0) };
        catalogPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        catalogPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        catalogPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        catalogPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        catalogPanel.Controls.Add(new Label
        {
            Text = T("Anyagkatalógus", "Material catalog"),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = _text,
            Margin = new Padding(2, 0, 0, 4)
        }, 0, 0);
        catalogPanel.Controls.Add(new Label
        {
            Text = T("Az anyagszorzó a drágább/nehezebben nyomtatható anyag kockázati vagy kezelési felárát modellezi. 1,00 = nincs plusz szorzó.", "The material factor covers risk or handling costs for more demanding materials. 1.00 = no additional multiplier."),
            AutoSize = true,
            ForeColor = _muted,
            MaximumSize = new Size(900, 0),
            Margin = new Padding(2, 0, 0, 8)
        }, 0, 1);

        dgvMaterialCatalog = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White
        };
        dgvMaterialCatalog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = T("Anyag neve", "Material name"), FillWeight = 45 });
        dgvMaterialCatalog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = T("Ár (Ft/kg)", "Price (HUF/kg)"), FillWeight = 28 });
        dgvMaterialCatalog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factor", HeaderText = T("Anyagszorzó", "Material factor"), FillWeight = 27 });
        catalogPanel.Controls.Add(dgvMaterialCatalog, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
        var add = PrimaryButton(T("+ Új anyag", "+ New material"));
        var remove = SecondaryButton(T("Kijelölt anyag törlése", "Remove selected material"));
        add.Click += (_, _) => AddCatalogMaterial();
        remove.Click += (_, _) => RemoveCatalogMaterial();
        buttons.Controls.Add(add);
        buttons.Controls.Add(remove);
        catalogPanel.Controls.Add(buttons, 0, 3);
        root.Controls.Add(catalogPanel, 0, 1);
    }

    private void WireEvents()
    {
        foreach (var control in new Control[]
        {
            cmbBaseHours, cmbFunction, cmbSource, cmbPrecision, cmbInterfaces, chkScan, nudScanCount,
            cmbScanDifficulty, nudRevisions, cmbRush, chkDrawing, chkDfm, chkOnsite, nudDistance, cmbBuffer
        })
        {
            switch (control)
            {
                case NumericUpDown n: n.ValueChanged += (_, _) => RecalculateDesign(); break;
                case ComboBox c: c.SelectedIndexChanged += (_, _) => RecalculateDesign(); break;
                case CheckBox ch: ch.CheckedChanged += (_, _) => RecalculateDesign(); break;
            }
        }

        nudPrintHours.ValueChanged += (_, _) => RecalculatePrint();
        dgvPrintMaterials.CellValueChanged += (_, _) => RecalculatePrint();
        dgvPrintMaterials.CellEndEdit += (_, _) => RecalculatePrint();
        dgvPrintMaterials.RowsRemoved += (_, _) => RecalculatePrint();
        dgvPrintMaterials.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (dgvPrintMaterials.IsCurrentCellDirty)
                dgvPrintMaterials.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        dgvPrintMaterials.DataError += (_, _) => { };
        dgvMaterialCatalog.DataError += (_, _) => { };
    }

    private void RefreshDesignOptions(bool preserveSelections)
    {
        int[] indices = preserveSelections
            ? [cmbBaseHours.SelectedIndex, cmbFunction.SelectedIndex, cmbSource.SelectedIndex, cmbPrecision.SelectedIndex, cmbInterfaces.SelectedIndex, cmbScanDifficulty.SelectedIndex, cmbRush.SelectedIndex, cmbBuffer.SelectedIndex]
            : [-1, -1, -1, -1, -1, -1, -1, -1];

        SetComboItems(cmbBaseHours,
            new(T("Nagyon egyszerű", "Very simple"), _settings.Design.BaseVerySimpleHours),
            new(T("Egyszerű", "Simple"), _settings.Design.BaseSimpleHours),
            new(T("Közepes", "Medium"), _settings.Design.BaseMediumHours),
            new(T("Összetett", "Complex"), _settings.Design.BaseComplexHours),
            new(T("Nagyon összetett / szabadformás", "Very complex / freeform"), _settings.Design.BaseVeryComplexHours));

        SetComboItems(cmbFunction,
            new(T("Dekoráció / vizuális elem", "Decorative / visual element"), _settings.Design.FunctionDecorationFactor),
            new(T("Burkolat / takaróelem", "Enclosure / cover"), _settings.Design.FunctionCoverFactor),
            new(T("Funkcionális alkatrész", "Functional part"), _settings.Design.FunctionFunctionalFactor),
            new(T("Mechanikailag terhelt alkatrész", "Mechanically loaded part"), _settings.Design.FunctionLoadedFactor));

        SetComboItems(cmbSource,
            new(T("Pontos rajz / használható CAD", "Accurate drawing / usable CAD"), _settings.Design.SourceDrawingFactor),
            new(T("Fizikai mintadarab", "Physical sample"), _settings.Design.SourcePhysicalFactor),
            new(T("Kapott 3D scan / mesh", "Provided 3D scan / mesh"), _settings.Design.SourceMeshFactor),
            new(T("Csak fotók / kevés méret", "Photos only / few dimensions"), _settings.Design.SourcePhotoFactor));

        SetComboItems(cmbPrecision,
            new(T("Normál", "Standard"), _settings.Design.PrecisionNormalFactor),
            new(T("Illesztési méretek fontosak", "Critical mating dimensions"), _settings.Design.PrecisionFitFactor),
            new(T("Sok kritikus méret / nagy pontosság", "Many critical dimensions / high precision"), _settings.Design.PrecisionHighFactor));

        SetComboItems(cmbInterfaces,
            new("0", _settings.Design.Interface0Factor),
            new("1", _settings.Design.Interface1Factor),
            new("2", _settings.Design.Interface2Factor),
            new("3", _settings.Design.Interface3Factor),
            new("4", _settings.Design.Interface4Factor),
            new(T("5 vagy több", "5 or more"), _settings.Design.Interface5PlusFactor));

        SetComboItems(cmbScanDifficulty,
            new(T("Normál matt geometria", "Standard matte geometry"), _settings.Design.ScanNormalFactor),
            new(T("Fényes / fekete / spray szükséges", "Glossy / black / spray required"), _settings.Design.ScanDifficultSurfaceFactor),
            new(T("Komplex / takart felületek", "Complex / obscured surfaces"), _settings.Design.ScanComplexFactor));

        SetComboItems(cmbRush,
            new(T("Normál", "Standard"), _settings.Design.RushNormalFactor),
            new(T("48 órán belül", "Within 48 hours"), _settings.Design.Rush48Factor),
            new(T("24 órán belül", "Within 24 hours"), _settings.Design.Rush24Factor));

        SetComboItems(cmbBuffer,
            new(T("Jól definiált munka", "Well-defined work"), _settings.Design.BufferDefinedFactor),
            new(T("Normál egyedi munka", "Standard custom work"), _settings.Design.BufferNormalFactor),
            new(T("Bizonytalan geometria", "Uncertain geometry"), _settings.Design.BufferUncertainFactor),
            new(T("Magas kockázat / kevés információ", "High risk / limited information"), _settings.Design.BufferHighRiskFactor));

        if (preserveSelections)
        {
            ComboBox[] combos = [cmbBaseHours, cmbFunction, cmbSource, cmbPrecision, cmbInterfaces, cmbScanDifficulty, cmbRush, cmbBuffer];
            for (int i = 0; i < combos.Length; i++)
                combos[i].SelectedIndex = indices[i] >= 0 && indices[i] < combos[i].Items.Count ? indices[i] : 0;
        }
    }

    private static void SetComboItems(ComboBox combo, params OptionItem[] items)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.AddRange(items);
        combo.SelectedIndex = items.Length > 0 ? 0 : -1;
        combo.EndUpdate();
    }

    private void ResetCalculatorInputs()
    {
        ResetDesignInputs();
        ResetPrintInputs();
    }

    private void ResetDesignInputs()
    {
        cmbBaseHours.SelectedIndex = Math.Min(2, cmbBaseHours.Items.Count - 1);
        cmbFunction.SelectedIndex = Math.Min(2, cmbFunction.Items.Count - 1);
        cmbSource.SelectedIndex = Math.Min(1, cmbSource.Items.Count - 1);
        cmbPrecision.SelectedIndex = Math.Min(1, cmbPrecision.Items.Count - 1);
        cmbInterfaces.SelectedIndex = Math.Min(1, cmbInterfaces.Items.Count - 1);
        chkScan.Checked = false;
        nudScanCount.Value = 1;
        cmbScanDifficulty.SelectedIndex = 0;
        nudRevisions.Value = 0;
        cmbRush.SelectedIndex = 0;
        chkDrawing.Checked = false;
        chkDfm.Checked = false;
        chkOnsite.Checked = false;
        nudDistance.Value = 10;
        cmbBuffer.SelectedIndex = Math.Min(1, cmbBuffer.Items.Count - 1);
    }

    private void ResetPrintInputs()
    {
        nudPrintHours.Value = 1m;
        dgvPrintMaterials.Rows.Clear();
        AddPrintMaterialRow();
    }

    private void RecalculateDesign()
    {
        if (cmbBaseHours is null || cmbBaseHours.SelectedItem is null) return;

        nudScanCount.Enabled = chkScan.Checked;
        cmbScanDifficulty.Enabled = chkScan.Checked;
        nudDistance.Enabled = chkOnsite.Checked;

        decimal baseHours = SelectedValue(cmbBaseHours);
        decimal function = SelectedValue(cmbFunction);
        decimal source = SelectedValue(cmbSource);
        decimal precision = SelectedValue(cmbPrecision);
        decimal interfaces = SelectedValue(cmbInterfaces);
        decimal rush = SelectedValue(cmbRush);
        decimal buffer = SelectedValue(cmbBuffer);
        decimal factorProduct = function * source * precision * interfaces;
        decimal modelHours = baseHours * factorProduct;

        decimal scanHours = 0m;
        if (chkScan.Checked)
        {
            decimal scanBase = _settings.Design.ScanFirstHours + Math.Max(0, (int)nudScanCount.Value - 1) * _settings.Design.ScanAdditionalHours;
            scanHours = scanBase * SelectedValue(cmbScanDifficulty);
        }

        decimal revisionHours = nudRevisions.Value * _settings.Design.RevisionHours;
        decimal drawingHours = chkDrawing.Checked ? _settings.Design.DrawingHours : 0m;
        decimal dfmHours = chkDfm.Checked ? _settings.Design.DfmHours : 0m;
        decimal totalHours = modelHours + scanHours + revisionHours + drawingHours + dfmHours;
        decimal laborRaw = totalHours * _settings.Design.HourlyRate * rush * buffer;
        decimal labor = Math.Max(_settings.Design.MinimumFee, laborRaw);
        decimal travel = chkOnsite.Checked
            ? _settings.Design.TravelBaseFee + nudDistance.Value * 2m * _settings.Design.TravelPerKm
            : 0m;
        decimal final = RoundUp(labor + travel, _settings.Design.RoundingStep);
        decimal low = RoundUp(final * _settings.Design.QuoteLowFactor, _settings.Design.RoundingStep);
        decimal high = RoundUp(final * _settings.Design.QuoteHighFactor, _settings.Design.RoundingStep);

        lblDesignFinal.Text = Money(final);
        lblDesignRange.Text = $"{T("Ajánlati sáv: ", "Quote range: ")}{Money(low)} – {Money(high)}";
        lblDesignHours.Text = $"{T("Becsült munkaidő\r\n", "Estimated work time\r\n")}{Hour(totalHours)}";
        lblDesignTravel.Text = $"{T("Kiszállás\r\n", "Travel\r\n")}{(travel > 0 ? Money(travel) : T("nincs", "none"))}";

        var lines = new List<string>
        {
            $"{T("Alap modellezési idő      ", "Base modeling time       ")}{Hour(baseHours),10}",
            $"{T("Projektjellemzők után     ", "After project factors    ")}{Hour(modelHours),10}"
        };
        if (scanHours > 0) lines.Add($"{T("3D szkennelés             +", "3D scanning              +")}{Hour(scanHours),9}");
        if (revisionHours > 0) lines.Add($"{T("Extra módosítások         +", "Additional revisions     +")}{Hour(revisionHours),9}");
        if (drawingHours > 0) lines.Add($"{T("2D műszaki rajz           +", "2D technical drawing     +")}{Hour(drawingHours),9}");
        if (dfmHours > 0) lines.Add($"{T("Gyárthatósági optimaliz.  +", "DFM optimization         +")}{Hour(dfmHours),9}");
        lines.Add("────────────────────────────────────");
        lines.Add($"{T("Munkaidő összesen          ", "Total work time           ")}{Hour(totalHours),10}");
        lines.Add($"{T("Tervezési rész             ", "Design charge             ")}{Money(labor),10}");
        if (travel > 0) lines.Add($"{T("Kiszállás                  ", "Travel                    ")}{Money(travel),10}");
        lines.Add("────────────────────────────────────");
        lines.Add($"{T("AJÁNLOTT ÁR                ", "RECOMMENDED PRICE         ")}{Money(final),10}");
        lblDesignBreakdown.Text = string.Join(Environment.NewLine, lines);
    }

    private void AddPrintMaterialRow()
    {
        if (_settings.Materials.Count == 0) return;
        int rowIndex = dgvPrintMaterials.Rows.Add();
        var row = dgvPrintMaterials.Rows[rowIndex];
        row.Cells["MaterialId"].Value = _settings.Materials[0].Id;
        row.Cells["Grams"].Value = "0";
        RecalculatePrint();
    }

    private void RemoveSelectedPrintMaterialRow()
    {
        if (dgvPrintMaterials.SelectedRows.Count == 0) return;
        foreach (DataGridViewRow row in dgvPrintMaterials.SelectedRows)
            if (!row.IsNewRow) dgvPrintMaterials.Rows.Remove(row);
        if (dgvPrintMaterials.Rows.Count == 0) AddPrintMaterialRow();
        RecalculatePrint();
    }

    private void RefreshPrintMaterialColumn(bool preserveRows)
    {
        List<(string? id, decimal grams)> saved = [];
        if (preserveRows && dgvPrintMaterials is not null)
        {
            foreach (DataGridViewRow row in dgvPrintMaterials.Rows)
                saved.Add((row.Cells["MaterialId"].Value?.ToString(), ParseDecimal(row.Cells["Grams"].Value)));
        }

        if (dgvPrintMaterials?.Columns["MaterialId"] is DataGridViewComboBoxColumn combo)
        {
            combo.DataSource = null;
            combo.DisplayMember = nameof(MaterialDefinition.Name);
            combo.ValueMember = nameof(MaterialDefinition.Id);
            combo.DataSource = _settings.Materials.Select(CloneMaterial).ToList();
        }

        if (preserveRows && dgvPrintMaterials is not null)
        {
            dgvPrintMaterials.Rows.Clear();
            foreach (var item in saved)
            {
                string id = _settings.Materials.Any(m => m.Id == item.id)
                    ? item.id!
                    : _settings.Materials.FirstOrDefault()?.Id ?? string.Empty;
                int index = dgvPrintMaterials.Rows.Add();
                dgvPrintMaterials.Rows[index].Cells["MaterialId"].Value = id;
                dgvPrintMaterials.Rows[index].Cells["Grams"].Value = item.grams.ToString("0.##", _culture);
            }
            if (dgvPrintMaterials.Rows.Count == 0 && _settings.Materials.Count > 0)
                AddPrintMaterialRow();
        }
    }

    private void RecalculatePrint()
    {
        if (dgvPrintMaterials is null || lblPrintFinal is null) return;

        decimal totalRawMaterial = 0m;
        decimal totalBillableMaterial = 0m;
        var materialLines = new List<string>();

        foreach (DataGridViewRow row in dgvPrintMaterials.Rows)
        {
            string? materialId = row.Cells["MaterialId"].Value?.ToString();
            var material = _settings.Materials.FirstOrDefault(m => m.Id == materialId);
            decimal grams = Math.Max(0m, ParseDecimal(row.Cells["Grams"].Value));
            if (material is null)
            {
                row.Cells["RawCost"].Value = "—";
                row.Cells["BilledCost"].Value = "—";
                continue;
            }

            decimal raw = grams / 1000m * material.PricePerKg;
            decimal billed = raw * material.Factor * _settings.Print.WasteFactor;
            totalRawMaterial += raw;
            totalBillableMaterial += billed;
            row.Cells["RawCost"].Value = Money(raw);
            row.Cells["BilledCost"].Value = Money(billed);
            if (grams > 0)
                materialLines.Add($"{material.Name,-18} {grams,7:0.##} g   {Money(billed),10}");
        }

        decimal timeCost = nudPrintHours.Value * _settings.Print.MachineHourlyRate;
        decimal subtotal = totalBillableMaterial + timeCost;
        decimal beforeRound = Math.Max(_settings.Print.MinimumFee, subtotal);
        decimal final = RoundUp(beforeRound, _settings.Print.RoundingStep);
        decimal low = RoundUp(final * _settings.Print.QuoteLowFactor, _settings.Print.RoundingStep);
        decimal high = RoundUp(final * _settings.Print.QuoteHighFactor, _settings.Print.RoundingStep);

        lblPrintFinal.Text = Money(final);
        lblPrintRange.Text = $"{T("Ajánlati sáv: ", "Quote range: ")}{Money(low)} – {Money(high)}";
        lblPrintMaterialCost.Text = $"{T("Anyagdíj\r\n", "Material charge\r\n")}{Money(totalBillableMaterial)}";
        lblPrintTimeCost.Text = $"{T("Gépidő\r\n", "Machine time\r\n")}{Money(timeCost)}";

        var lines = new List<string>();
        if (materialLines.Count == 0)
            lines.Add(T("Nincs megadott anyagtömeg.", "No material weight entered."));
        else
            lines.AddRange(materialLines);
        lines.Add("────────────────────────────────────");
        lines.Add($"{T("Nyers anyagköltség         ", "Raw material cost        ")}{Money(totalRawMaterial),10}");
        lines.Add($"{T("Kalkulált anyagdíj         ", "Material charge          ")}{Money(totalBillableMaterial),10}");
        lines.Add($"{T("Nyomtatási idő             ", "Print time               ")}{Hour(nudPrintHours.Value),10}");
        lines.Add($"{T("Gépidő díja                ", "Machine time charge      ")}{Money(timeCost),10}");
        lines.Add($"{T("Részösszeg                  ", "Subtotal                 ")}{Money(subtotal),10}");
        if (subtotal < _settings.Print.MinimumFee)
            lines.Add($"{T("Minimumdíj érvényes         ", "Minimum fee applied      ")}{Money(_settings.Print.MinimumFee),10}");
        lines.Add("────────────────────────────────────");
        lines.Add($"{T("AJÁNLOTT ÁR                ", "RECOMMENDED PRICE         ")}{Money(final),10}");
        lblPrintBreakdown.Text = string.Join(Environment.NewLine, lines);
    }

    private void LoadSettingsIntoSettingsUi()
    {
        foreach (var binding in _settingBindings)
            SetNumericSafe(binding.Control, binding.Getter());

        _materialEditList = new BindingList<MaterialDefinition>(_settings.Materials.Select(CloneMaterial).ToList());
        RefreshMaterialCatalogGrid();
    }

    private void RefreshMaterialCatalogGrid()
    {
        if (dgvMaterialCatalog is null) return;
        dgvMaterialCatalog.Rows.Clear();
        foreach (var material in _materialEditList)
        {
            int index = dgvMaterialCatalog.Rows.Add();
            dgvMaterialCatalog.Rows[index].Tag = material.Id;
            dgvMaterialCatalog.Rows[index].Cells["Name"].Value = material.Name;
            dgvMaterialCatalog.Rows[index].Cells["Price"].Value = material.PricePerKg.ToString("0.##", _culture);
            dgvMaterialCatalog.Rows[index].Cells["Factor"].Value = material.Factor.ToString("0.00", _culture);
        }
    }

    private void AddCatalogMaterial()
    {
        dgvMaterialCatalog.EndEdit();
        int index = dgvMaterialCatalog.Rows.Add();
        dgvMaterialCatalog.Rows[index].Tag = Guid.NewGuid().ToString("N");
        dgvMaterialCatalog.Rows[index].Cells["Name"].Value = T("Új anyag", "New material");
        dgvMaterialCatalog.Rows[index].Cells["Price"].Value = (SettingsStore.Currency switch { "EUR" => 20.8m, "USD" => 24m, _ => 8000m }).ToString("0.##", _culture);
        dgvMaterialCatalog.Rows[index].Cells["Factor"].Value = 1m.ToString("0.00", _culture);
        dgvMaterialCatalog.CurrentCell = dgvMaterialCatalog.Rows[index].Cells["Name"];
        dgvMaterialCatalog.BeginEdit(true);
    }

    private void RemoveCatalogMaterial()
    {
        if (dgvMaterialCatalog.SelectedRows.Count == 0) return;
        if (dgvMaterialCatalog.Rows.Count <= 1)
        {
            MessageBox.Show(T("Legalább egy anyagnak maradnia kell a katalógusban.", "The catalog must contain at least one material."), T("Anyagkatalógus", "Material catalog"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        dgvMaterialCatalog.Rows.Remove(dgvMaterialCatalog.SelectedRows[0]);
    }

    private bool SaveSettingsFromUi()
    {
        try
        {
            dgvMaterialCatalog.EndEdit();
            var materials = ReadMaterialsFromGrid();
            if (materials.Count == 0)
                throw new InvalidOperationException(T("Legalább egy anyagot adj meg az anyagkatalógusban.", "Add at least one material to the catalog."));

            foreach (var binding in _settingBindings)
                binding.Setter(binding.Control.Value);
            _settings.Materials = materials;
            SettingsStore.Save(_settings);

            _materialEditList = new BindingList<MaterialDefinition>(_settings.Materials.Select(CloneMaterial).ToList());
            RefreshDesignOptions(true);
            RefreshPrintMaterialColumn(true);
            RecalculateDesign();
            RecalculatePrint();

            lblSettingsStatus.Text = $"{T("Mentve: ", "Saved: ")}{DateTime.Now:yyyy.MM.dd. HH:mm:ss}  •  {SettingsStore.SettingsPath}";
            lblSettingsStatus.ForeColor = _accent;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, T("A beállítások nem menthetők", "Unable to save settings"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private List<MaterialDefinition> ReadMaterialsFromGrid()
    {
        var result = new List<MaterialDefinition>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in dgvMaterialCatalog.Rows)
        {
            string name = row.Cells["Name"].Value?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(T("Minden anyagnak adj nevet.", "Enter a name for every material."));
            if (!names.Add(name))
                throw new InvalidOperationException($"{T("Az anyagnév kétszer szerepel: ", "Duplicate material name: ")}{name}");

            decimal price = ParseDecimal(row.Cells["Price"].Value);
            decimal factor = ParseDecimal(row.Cells["Factor"].Value);
            if (price < 0) throw new InvalidOperationException($"{T("Az anyag ára nem lehet negatív: ", "Material price cannot be negative: ")}{name}");
            if (factor <= 0) throw new InvalidOperationException($"{T("Az anyagszorzó legyen 0-nál nagyobb: ", "Material factor must be greater than zero: ")}{name}");

            result.Add(new MaterialDefinition
            {
                Id = row.Tag?.ToString() ?? Guid.NewGuid().ToString("N"),
                Name = name,
                PricePerKg = price,
                Factor = factor
            });
        }
        return result;
    }

    private void FactoryReset()
    {
        var result = MessageBox.Show(
            T("Visszaállítod az aktuális pénznem díjait, szorzóit és anyagkatalógusát a gyári értékekre?\n\nAz aktuális pénznem mentett beállításai felülíródnak.", "Restore rates, factors and materials for the current currency?\n\nSaved settings for this currency will be overwritten."),
            T("Gyári alaphelyzet", "Factory reset"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        _settings = SettingsStore.ResetToFactoryDefaults();
        LoadSettingsIntoSettingsUi();
        RefreshDesignOptions(false);
        RefreshPrintMaterialColumn(false);
        ResetCalculatorInputs();
        RecalculateDesign();
        RecalculatePrint();
        lblSettingsStatus.Text = T("Gyári alapértékek visszaállítva és elmentve.", "Factory defaults restored and saved.");
        lblSettingsStatus.ForeColor = _accent;
    }

    private void AddSetting(TableLayoutPanel table, int logicalRow, string label, string suffix,
        Func<decimal> getter, Action<decimal> setter, int decimals, decimal min, decimal max, decimal increment)
    {
        bool monetary = suffix.Contains(Localizer.MoneyUnit, StringComparison.Ordinal);
        if (monetary && SettingsStore.Currency != "HUF")
        {
            min = min > 0 ? 0.01m : 0;
            decimals = 2;
            increment = 0.5m;
        }
        var numeric = CreateNumeric(min, max, increment, getter(), decimals);
        AddField(table, logicalRow, label, numeric, suffix);
        _settingBindings.Add(new SettingBinding(numeric, getter, setter));
    }

    private static MaterialDefinition CloneMaterial(MaterialDefinition source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        PricePerKg = source.PricePerKg,
        Factor = source.Factor
    };

    private decimal SelectedValue(ComboBox combo) => combo.SelectedItem is OptionItem item ? item.Value : 1m;

    private static decimal ParseDecimal(object? value)
    {
        if (value is null) return 0m;
        if (value is decimal d) return d;
        var text = value.ToString()?.Trim() ?? string.Empty;
        // Accept either decimal separator, without treating it as a thousands separator.
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        text = text.Replace(" ", "").Replace("\u00a0", "").Replace("\u202f", "").Replace(',', '.');
        if (decimal.TryParse(text, style, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return 0m;
    }

    private static decimal RoundUp(decimal value, decimal step)
    {
        if (step <= 0) return value;
        return Math.Ceiling(value / step) * step;
    }

    private string Money(decimal value) => $"{value.ToString(SettingsStore.Currency == "HUF" ? "N0" : "N2", _culture)} {Localizer.MoneyUnit}";
    private string Hour(decimal value) => $"{value.ToString("0.00", _culture)} h";

    private TableLayoutPanel TwoColumnLayout(float leftPercent, float rightPercent)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, rightPercent));
        return layout;
    }

    private Panel ScrollHost() => new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };

    private TableLayoutPanel AutoStack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(4)
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }

    private (GroupBox Group, TableLayoutPanel Table) CreateSection(string title, string description)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 12, 14, 14),
            Margin = new Padding(0, 0, 0, 12),
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = _text
        };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(2, 4, 2, 2),
            Font = new Font("Segoe UI", 9.7F)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        var desc = new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            ForeColor = _muted,
            Font = new Font("Segoe UI", 8.8F),
            Margin = new Padding(3, 0, 3, 8)
        };
        table.Controls.Add(desc, 0, 0);
        table.SetColumnSpan(desc, 2);
        group.Controls.Add(table);
        return (group, table);
    }

    private (GroupBox Group, TableLayoutPanel Table) CreateSettingsSection(string title, string description)
    {
        var section = CreateSection(title, description);
        section.Table.ColumnStyles.Clear();
        section.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        section.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        return section;
    }

    private Panel CreateCard() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 0, 0, 12)
    };

    private FlowLayoutPanel CardFlow() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(16)
    };

    private Label MakeHeading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F),
        ForeColor = _text,
        Margin = new Padding(0, 0, 0, 8)
    };

    private Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = _muted,
        Font = new Font("Segoe UI Semibold", 9F)
    };

    private Label BigPriceLabel() => new()
    {
        Text = T("0 Ft", "0 HUF"),
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 25F),
        ForeColor = _accent,
        Margin = new Padding(0, 4, 0, 0)
    };

    private Label MutedLabel(int maxWidth) => new()
    {
        AutoSize = true,
        MaximumSize = new Size(maxWidth, 0),
        ForeColor = _muted,
        Margin = new Padding(0, 2, 0, 12)
    };

    private Label MutedText(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(440, 0),
        ForeColor = _muted
    };

    private Label MonospaceLabel(int width) => new()
    {
        AutoSize = true,
        MaximumSize = new Size(width, 0),
        Font = new Font("Consolas", 9F),
        ForeColor = _text
    };

    private TableLayoutPanel MetricTable()
    {
        var t = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, BackColor = _soft, Padding = new Padding(10) };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        return t;
    }

    private Label MetricLabel() => new()
    {
        AutoSize = false,
        Height = 76,
        MinimumSize = new Size(140, 76),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI Semibold", 10F),
        Padding = new Padding(4, 0, 4, 0)
    };

    private ComboBox CreateCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        DropDownHeight = 260,
        Margin = new Padding(3, 4, 3, 5)
    };

    private NumericUpDown CreateNumeric(decimal min, decimal max, decimal increment, decimal value, int decimals) => new()
    {
        Minimum = min,
        Maximum = max,
        Increment = increment,
        Value = Math.Min(max, Math.Max(min, value)),
        DecimalPlaces = decimals,
        ThousandsSeparator = decimals == 0 && max >= 1000,
        Width = 140
    };

    private Button PrimaryButton(string text)
    {
        var b = new Button { Text = text, AutoSize = true, BackColor = _accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Padding = new Padding(8, 4, 8, 4) };
        b.FlatAppearance.BorderColor = _accent;
        return b;
    }

    private Button SecondaryButton(string text)
    {
        var b = new Button { Text = text, AutoSize = true, BackColor = Color.White, ForeColor = _text, FlatStyle = FlatStyle.Flat, Padding = new Padding(8, 4, 8, 4) };
        b.FlatAppearance.BorderColor = _border;
        return b;
    }

    private void AddField(TableLayoutPanel table, int logicalRow, string labelText, Control control, string suffix)
    {
        int row = logicalRow + 1;
        EnsureRow(table, row);
        var label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 8) };
        var flow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        flow.Controls.Add(control);
        flow.Controls.Add(new Label { Text = suffix, AutoSize = true, ForeColor = _muted, Margin = new Padding(5, 8, 0, 0) });
        table.Controls.Add(label, 0, row);
        table.Controls.Add(flow, 1, row);
    }

    private void AddFullField(TableLayoutPanel table, int logicalRow, string labelText, Control control)
    {
        int row = logicalRow + 1;
        EnsureRow(table, row);
        if (!string.IsNullOrWhiteSpace(labelText))
        {
            table.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 8) }, 0, row);
            table.Controls.Add(control, 1, row);
        }
        else
        {
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 2);
        }
    }

    private static void EnsureRow(TableLayoutPanel table, int row)
    {
        while (table.RowCount <= row)
        {
            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
    }

    private static void AddSection(TableLayoutPanel parent, Control section)
    {
        int row = parent.RowCount++;
        parent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Dock = DockStyle.Top;
        parent.Controls.Add(section, 0, row);
    }

    private static void SetNumericSafe(NumericUpDown control, decimal value)
    {
        control.Value = Math.Min(control.Maximum, Math.Max(control.Minimum, value));
    }
}
