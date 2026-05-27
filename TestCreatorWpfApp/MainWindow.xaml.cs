using LiveCharts.Wpf;
using LiveCharts;
using SchemaDiscovery;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using TestCreator;
using DocumentManager;
using ConnectionManager;

namespace TestCreatorWpfApp
{
    public class AppSettings
    {
        public string LastRequirementsPath { get; set; } = string.Empty;
        public string LastParametersPath { get; set; } = string.Empty;
        public string LastOutputPath { get; set; } = string.Empty;
        public string UdpIp { get; set; } = "127.0.0.1";
        public int UdpPort { get; set; } = 5000;
        public string TcpIp { get; set; } = "127.0.0.1";
        public int TcpPort { get; set; } = 5002;
        public string DbConnectionString { get; set; } = string.Empty;
        public bool UseTcp { get; set; } = false;
        public bool LoadFromSql { get; set; } = false;
        public List<string> History { get; set; } = new List<string>();
    }


    public class TelemetryItem
    {
        public string ParameterName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Units";
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string CurrentValue { get; set; } = "0";
        public string Status { get; set; } = "Normal";
        public SeriesCollection ChartSeries { get; set; } = new SeriesCollection();
    }
    public partial class MainWindow : Window
    {
        private Process? _serverProcess;
        private Process? _clientProcess;
        private Process? _tcpServerProcess;
        private Process? _tcpClientProcess;

        private const int UdpMonitorPort = 5001;
        private const int TcpMonitorPort = 5003;

        private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private AppSettings _settings = new AppSettings();

        private UdpClient? _monitorLogClient;

        public MainWindow()
        {
            InitializeComponent();
            _monitorLogClient = new UdpClient();

            LoadSettings();

            // Connect to DB on startup
            _ = ConnectToDatabaseOnStartup();

            StartExternalTerminals();

            // Send initial ping to monitors
            _ = LogToTerminal("WPF Application Linked - Ready.", null);

            this.Closed += MainWindow_Closed;
        }

        private async Task ConnectToDatabaseOnStartup()
        {
            LogSystemMessage("Initializing system...");
            if (string.IsNullOrWhiteSpace(_settings.DbConnectionString))
            {
                LogSystemMessage("DB: No connection string configured.");
                lblDbStatus.Text = "DB: Not Configured";
                lblDbStatus.Foreground = Brushes.Gray;
                return;
            }

            LogSystemMessage($"DB: Attempting to connect to SQL Server...");
            DatabaseManager dbManager = new DatabaseManager();
            bool isConnected = await dbManager.TestConnectionAsync(_settings.DbConnectionString);

            if (isConnected)
            {
                LogSystemMessage("DB: Connection established successfully.");
                lblDbStatus.Text = "DB: Connected";
                lblDbStatus.Foreground = Brushes.LightGreen;
            }
            else
            {
                LogSystemMessage("DB: Connection failed. Check settings.");
                lblDbStatus.Text = "DB: Disconnected";
                lblDbStatus.Foreground = Brushes.Red;
            }
        }

        private void LogSystemMessage(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Application.Current.Dispatcher.Invoke(() =>
            {
                lstSystemLog.Items.Insert(0, entry);
                if (lstSystemLog.Items.Count > 100) lstSystemLog.Items.RemoveAt(100);
            });
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                    txtReq.Text = _settings.LastRequirementsPath;
                    txtParams.Text = _settings.LastParametersPath;
                    txtOutput.Text = _settings.LastOutputPath;
                    txtUdpIp.Text = _settings.UdpIp;
                    txtUdpPort.Text = _settings.UdpPort.ToString();
                    txtTcpIp.Text = _settings.TcpIp;
                    txtTcpPort.Text = _settings.TcpPort.ToString();
                    txtDbConnectionString.Text = _settings.DbConnectionString;
                    chkUseTcp.IsChecked = _settings.UseTcp;
                    chkLoadFromSql.IsChecked = _settings.LoadFromSql;

                    RefreshHistoryList(); LoadRequirementPreview();
                }
                else { InitializeDefaultPaths(); }
            }
            catch { InitializeDefaultPaths(); }
        }

        private void SaveSettings()
        {
            try
            {
                _settings.LastRequirementsPath = txtReq.Text;
                _settings.LastParametersPath = txtParams.Text;
                _settings.LastOutputPath = txtOutput.Text;
                _settings.UdpIp = txtUdpIp.Text;
                if (int.TryParse(txtUdpPort.Text, out int uPort)) _settings.UdpPort = uPort;
                _settings.TcpIp = txtTcpIp.Text;
                if (int.TryParse(txtTcpPort.Text, out int tPort)) _settings.TcpPort = tPort;
                _settings.DbConnectionString = txtDbConnectionString.Text;
                _settings.UseTcp = chkUseTcp.IsChecked ?? false;
                _settings.LoadFromSql = chkLoadFromSql.IsChecked ?? false;

                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        private void AddToHistory(string action)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action}";
            _settings.History.Insert(0, entry);
            if (_settings.History.Count > 5) _settings.History = _settings.History.Take(5).ToList();
            RefreshHistoryList(); LoadRequirementPreview();
            SaveSettings();
        }

        private void RefreshHistoryList()
        {
            lstHistory.ItemsSource = null;
            lstHistory.ItemsSource = _settings.History;
        }

        private void InitializeDefaultPaths()
        {
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\EnbeddedRequirmentToTest\Assets");
            if (Directory.Exists(assetsPath))
            {
                txtReq.Text = Path.GetFullPath(Path.Combine(assetsPath, "Requirements.docx"));
                txtParams.Text = Path.GetFullPath(Path.Combine(assetsPath, "TestTable.xlsx"));
                txtOutput.Text = Path.GetFullPath(Path.Combine(assetsPath, "GeneratedTestTable.xlsx"));
            }
            txtUdpIp.Text = "127.0.0.1"; txtUdpPort.Text = "5000";
            txtTcpIp.Text = "127.0.0.1"; txtTcpPort.Text = "5002";
            txtDbConnectionString.Text = "";
        }

        private void nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string target = btn.Tag?.ToString() ?? "";
                viewDashboard.Visibility = target == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
                viewLogic.Visibility = target == "Logic" ? Visibility.Visible : Visibility.Collapsed;
                viewRecordTesting.Visibility = target == "RecordTesting" ? Visibility.Visible : Visibility.Collapsed;
                viewHistory.Visibility = target == "History" ? Visibility.Visible : Visibility.Collapsed;
                viewSettings.Visibility = target == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _settings.History.Clear();
            RefreshHistoryList(); LoadRequirementPreview();
            SaveSettings();
            LogSystemMessage("Action history cleared.");
        }

        private void btnSaveUdpSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.UdpIp = txtUdpIp.Text;
            if (int.TryParse(txtUdpPort.Text, out int port)) _settings.UdpPort = port;
            SaveSettings();
            LogSystemMessage("UDP Configuration saved.");
        }

        private void btnSaveTcpSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.TcpIp = txtTcpIp.Text;
            if (int.TryParse(txtTcpPort.Text, out int port)) _settings.TcpPort = port;
            SaveSettings();
            LogSystemMessage("TCP Configuration saved.");
        }

        private void btnSaveDbSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.DbConnectionString = txtDbConnectionString.Text;
            SaveSettings();
            LogSystemMessage("Database settings saved.");
            _ = ConnectToDatabaseOnStartup();
        }

        private async void btnTestDbConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDbConnectionString.Text))
            {
                LogSystemMessage("Warning: No DB connection string provided.");
                return;
            }

            LogSystemMessage("Testing DB connection...");
            DatabaseManager dbManager = new DatabaseManager();
            bool isConnected = await dbManager.TestConnectionAsync(txtDbConnectionString.Text);

            if (isConnected)
            {
                LogSystemMessage("Success: Database Connection Successful!");
                AddToHistory("Database Connection Test: SUCCESS");
            }
            else
            {
                LogSystemMessage("Error: Database Connection Failed.");
                AddToHistory("Database Connection Test: FAILED");
            }
        }

        private void StartExternalTerminals()
        {
            try
            {
                string toolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\UdpTerminalTool\bin\Debug\net9.0");
                string toolExe = Path.Combine(toolDir, "UdpTerminalTool.exe");
                if (!File.Exists(toolExe)) {
                    toolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\UdpTerminalTool\bin\Debug\net8.0");
                    toolExe = Path.Combine(toolDir, "UdpTerminalTool.exe");
                }

                if (File.Exists(toolExe))
                {
                    _serverProcess = Process.Start(new ProcessStartInfo { FileName = toolExe, Arguments = $"server {_settings.UdpIp} {_settings.UdpPort}", UseShellExecute = true });
                    _clientProcess = Process.Start(new ProcessStartInfo { FileName = toolExe, Arguments = "client", UseShellExecute = true });
                    _tcpServerProcess = Process.Start(new ProcessStartInfo { FileName = toolExe, Arguments = $"tcpserver {_settings.TcpIp} {_settings.TcpPort}", UseShellExecute = true });
                    _tcpClientProcess = Process.Start(new ProcessStartInfo { FileName = toolExe, Arguments = "tcpclient", UseShellExecute = true });
                    LogSystemMessage("External terminals started.");
                }
            }
            catch (Exception ex) { LogSystemMessage($"Error starting terminals: {ex.Message}"); }
        }

        private async Task LogToTerminal(string message, bool? isTcp)
        {
            try {
                if (_monitorLogClient == null) return;
                byte[] data = Encoding.UTF8.GetBytes($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");

                if (isTcp == true || isTcp == null)
                    await _monitorLogClient.SendAsync(data, data.Length, "127.0.0.1", TcpMonitorPort);

                if (isTcp == false || isTcp == null)
                    await _monitorLogClient.SendAsync(data, data.Length, "127.0.0.1", UdpMonitorPort);
            } catch { }
        }

        private void LoadRequirementPreview()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtReq.Text) || !File.Exists(txtReq.Text))
                {
                    lstRequirements.Items.Clear();
                    return;
                }

                ExcelTestTableCreator creator = new ExcelTestTableCreator();
                // We need to access the logic to read the doc. 
                // Since ReadConditionFromDoc is private in ExcelTestTableCreator, I will use a simple implementation here
                // or I should have made it public. Let s just read it here for simplicity of the UI update.
                using (DocumentFormat.OpenXml.Packaging.WordprocessingDocument wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(txtReq.Text, false))
                {
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    string condition = body?.InnerText ?? "No text found.";
                    
                    Application.Current.Dispatcher.Invoke(() => {
                        lstRequirements.Items.Clear();
                        lstRequirements.Items.Add(condition);
                    });
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Requirement Preview Error: {ex.Message}");
            }
        }
        private void btnBrowseReq_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Word Files|*.docx" };
            if (ofd.ShowDialog() == true) { txtReq.Text = ofd.FileName; SaveSettings(); LoadRequirementPreview(); LogSystemMessage($"Requirements file selected: {Path.GetFileName(ofd.FileName)}"); }
        }

        private void btnBrowseParams_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel/JSON Files|*.xlsx;*.json|Excel Files|*.xlsx|JSON Files|*.json" };
            if (ofd.ShowDialog() == true) { txtParams.Text = ofd.FileName; SaveSettings(); LogSystemMessage($"Parameters file selected: {Path.GetFileName(ofd.FileName)}"); }
        }

        private void btnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Files|*.xlsx" };
            if (sfd.ShowDialog() == true) { txtOutput.Text = sfd.FileName; SaveSettings(); LogSystemMessage($"Output path selected: {Path.GetFileName(sfd.FileName)}"); }
        }

        private async void btnCreateOnly_Click(object sender, RoutedEventArgs e)
        {
            bool useSql = chkLoadFromSql.IsChecked == true;
            if (string.IsNullOrWhiteSpace(txtReq.Text) || (!useSql && string.IsNullOrWhiteSpace(txtParams.Text)) || string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                LogSystemMessage("Warning: Missing required inputs.");
                return;
            }

            try {
                SaveSettings();
                gridChart.Visibility = Visibility.Collapsed;
                LogSystemMessage("Generating test permutations...");
                ExcelTestTableCreator creator = new ExcelTestTableCreator();

                if (useSql)
                {
                    DatabaseManager dbManager = new DatabaseManager();
                    var paramsMap = await dbManager.LoadParametersAsync(_settings.DbConnectionString);
                    creator.CreatePermutationTable(txtReq.Text, paramsMap, txtOutput.Text);
                }
                else if (txtParams.Text.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    JsonParameterReader jsonReader = new JsonParameterReader();
                    var paramsMap = jsonReader.ReadParametersFromJson(txtParams.Text);
                    creator.CreatePermutationTable(txtReq.Text, paramsMap, txtOutput.Text);
                }
                else
                {
                    creator.CreatePermutationTable(txtReq.Text, txtParams.Text, txtOutput.Text);
                }

                await PopulatePreviewAsync(txtOutput.Text, runNetworkTest: false);
                AddToHistory($"Created test file: {Path.GetFileName(txtOutput.Text)}");
                LogSystemMessage("Success: Test Table generated.");
            } catch (Exception ex) { LogSystemMessage($"Error: {ex.Message}"); }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            bool useSql = chkLoadFromSql.IsChecked == true;
            if (string.IsNullOrWhiteSpace(txtReq.Text) || (!useSql && string.IsNullOrWhiteSpace(txtParams.Text)) || string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                LogSystemMessage("Warning: Missing required inputs.");
                return;
            }

            try {
                SaveSettings();
                gridChart.Visibility = Visibility.Collapsed;
                LogSystemMessage("Executing creation and verification sequence...");
                ExcelTestTableCreator creator = new ExcelTestTableCreator();

                if (useSql)
                {
                    DatabaseManager dbManager = new DatabaseManager();
                    var paramsMap = await dbManager.LoadParametersAsync(_settings.DbConnectionString);
                    creator.CreatePermutationTable(txtReq.Text, paramsMap, txtOutput.Text);
                }
                else if (txtParams.Text.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    JsonParameterReader jsonReader = new JsonParameterReader();
                    var paramsMap = jsonReader.ReadParametersFromJson(txtParams.Text);
                    creator.CreatePermutationTable(txtReq.Text, paramsMap, txtOutput.Text);
                }
                else
                {
                    creator.CreatePermutationTable(txtReq.Text, txtParams.Text, txtOutput.Text);
                }

                await PopulatePreviewAsync(txtOutput.Text, runNetworkTest: true);
                AddToHistory($"Executed {(_settings.UseTcp ? "TCP" : "UDP")} test: {Path.GetFileName(txtOutput.Text)}");
                LogSystemMessage("Success: Full execution completed.");
            } catch (Exception ex) { LogSystemMessage($"Error: {ex.Message}"); }
        }

        private async void btnTestEcho_Click(object sender, RoutedEventArgs e)
        {
            try {
                bool isTcp = _settings.UseTcp;
                string proto = isTcp ? "TCP" : "UDP";
                LogSystemMessage($"Starting manual {proto} echo test...");
                await LogToTerminal($"Manual {proto} Echo Test started...", isTcp);
                string testMsg = $"Ping {DateTime.Now:T}";

                if (isTcp) {
                    TcpSocketManager tcp = new TcpSocketManager();
                    await LogToTerminal($"[Client] Connecting to {_settings.TcpIp}:{_settings.TcpPort}...", true);
                    string? echo = await tcp.SendAndReceiveEchoAsync(testMsg, _settings.TcpIp, _settings.TcpPort, 1500);
                    if (echo == testMsg) { LogSystemMessage($"Success: TCP Echo verified."); await LogToTerminal($"[Client] Echo Success.", true); }
                    else { LogSystemMessage($"Error: TCP Echo timeout or failure."); await LogToTerminal($"[Client] Echo Fail.", true); }
                } else {
                    UdpSocketManager udp = new UdpSocketManager();
                    await LogToTerminal($"[Client] Sending to {_settings.UdpIp}:{_settings.UdpPort} -> \"{testMsg}\"", false);
                    string? echo = await udp.SendAndReceiveEchoAsync(testMsg, _settings.UdpIp, _settings.UdpPort, 1500);
                    if (echo == testMsg) { LogSystemMessage($"Success: UDP Echo verified."); await LogToTerminal($"[Client] Echo Success.", false); }
                    else { LogSystemMessage($"Error: UDP Echo timeout or failure."); await LogToTerminal($"[Client] Echo Fail.", false); }
                }
                AddToHistory($"{proto} Echo Test performed");
            } catch (Exception ex) { LogSystemMessage($"Error: {ex.Message}"); }
        }

        private async Task PopulatePreviewAsync(string filePath, bool runNetworkTest)
        {
            lstPreview.Items.Clear();
            DocumentReader reader = new DocumentReader();
            string[,] data = reader.ReadExcelToTwoDimensionalArray(filePath);
            int rows = data.GetLength(0), cols = data.GetLength(1);
            int[] colWidths = new int[cols];
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < Math.Min(rows, 16); r++)
                    if (data[r, c] != null) colWidths[c] = Math.Max(colWidths[c], data[r, c].Length);

            UdpSocketManager udpClient = new UdpSocketManager();
            TcpSocketManager tcpClient = new TcpSocketManager();
            int pass = 0, fail = 0, warn = 0, notRun = 0;
            bool isTcp = _settings.UseTcp;

            int maxRows = Math.Min(rows, 16);
            notRun = runNetworkTest ? maxRows - 1 : maxRows;

            if (runNetworkTest) { pbTotal.Visibility = Visibility.Visible; pbTotal.Value = 0; pbTotal.Maximum = maxRows - 1; }
            for (int r = 0; r < maxRows; r++)
            {
                string line = "";
                for (int c = 0; c < cols; c++) line += (data[r, c] ?? "").PadRight(colWidths[c] + 2) + (c < cols - 1 ? "| " : "");
                string timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}";
                var row = new PreviewRow { Text = timestampedLine };

                if (runNetworkTest && r > 0) {
                    notRun--;
                    await LogToTerminal($"[Client] Processing Row {r}...", isTcp);
                    await LogToTerminal($"[Client] Sending: \"{line.Trim()}\"", isTcp);

                    string? echo = isTcp
                        ? await tcpClient.SendAndReceiveEchoAsync(line, _settings.TcpIp, _settings.TcpPort, 800)
                        : await udpClient.SendAndReceiveEchoAsync(line, _settings.UdpIp, _settings.UdpPort, 800);

                    row.Progress = 50; 
                    if (echo == line) {
                        row.RowColor = Brushes.Green;
                        pass++;
                        await LogToTerminal($"[Client] Row {r} SUCCESS", isTcp);
                    }
                    else if (echo != null) {
                        row.RowColor = Brushes.Orange;
                        warn++;
                        await LogToTerminal($"[Client] Row {r} WARNING (Corrupted)", isTcp);
                    }
                    else {
                        row.RowColor = Brushes.Red;
                        fail++;
                        await LogToTerminal($"[Client] Row {r} FAILED", isTcp);
                    }
                    row.Progress = 100;
                    pbTotal.Value++;
                    UpdateChart(pass, fail, warn, notRun);
                }
                else row.RowColor = Brushes.Black;
                lstPreview.Items.Add(row);
            }
            UpdateChart(pass, fail, warn, notRun);
        }

        private void UpdateChart(int pass, int fail, int warn, int notRun)
        {
            gridChart.Visibility = Visibility.Visible;
            lblPassCount.Text = $"PASS: {pass}";
            lblFailCount.Text = $"FAIL: {fail}";
            lblWarnCount.Text = $"WARN: {warn}";
            lblNotRunCount.Text = $"NOT RUN: {notRun}";

            double total = pass + fail + warn + notRun;
            if (total > 0)
            {
                double maxHeight = 5.0;
                Application.Current.Dispatcher.Invoke(() => {
                    scalePass.ScaleY = Math.Max(0.1, (pass / total) * maxHeight);
                    scaleFail.ScaleY = Math.Max(0.1, (fail / total) * maxHeight);
                    scaleWarn.ScaleY = Math.Max(0.1, (warn / total) * maxHeight);
                    scaleNotRun.ScaleY = Math.Max(0.1, (notRun / total) * maxHeight);
                });
            }
        }
        private async void btnRunSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedRows = lstPreview.SelectedItems.Cast<PreviewRow>().ToList();
            if (selectedRows.Count == 0)
            {
                LogSystemMessage("Warning: No test rows selected.");
                return;
            }

            try
            {
                LogSystemMessage($"Running {selectedRows.Count} selected test rows...");
                bool isTcp = _settings.UseTcp;
                
                foreach (var selectedRow in selectedRows)
                {
                    // Extract the data part after the timestamp [yyyy-MM-dd HH:mm:ss] 
                    string text = selectedRow.Text;
                    int dataIndex = text.IndexOf("] ") + 1;
                    if (dataIndex > 0) text = text.Substring(dataIndex).Trim();
                    
                    // Clean the data to remove the column separators " | " used for display
                    string cleanData = string.Join(" ", text.Split("|").Select(s => s.Trim()));

                    selectedRow.Progress = 0;
                    selectedRow.RowColor = Brushes.Orange;
                    await Task.Delay(50); // Small UI breathing room
                    selectedRow.Progress = 50;

                    string? echo = null;
                    if (isTcp)
                    {
                        TcpSocketManager tcp = new TcpSocketManager();
                        echo = await tcp.SendAndReceiveEchoAsync(cleanData, _settings.TcpIp, _settings.TcpPort, 1500);
                    }
                    else
                    {
                        UdpSocketManager udp = new UdpSocketManager();
                        echo = await udp.SendAndReceiveEchoAsync(cleanData, _settings.UdpIp, _settings.UdpPort, 1500);
                    }

                    if (echo == cleanData)
                    {
                        selectedRow.RowColor = Brushes.Green;
                    }
                    else
                    {
                        selectedRow.RowColor = Brushes.Red;
                    }
                    selectedRow.Progress = 100;
                }
                LogSystemMessage("Batch execution of selected rows completed.");
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Error running selected tests: {ex.Message}");
            }
        }
                private void btnClearRecord_Click(object sender, RoutedEventArgs e)
        {
            txtRequirementToTest.Clear();
            LogSystemMessage("Record Testing: Input cleared.");
        }

                private async void btnQueryRecord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogSystemMessage("Record Testing: Loading telemetry data...");
                
                string telemetryPath = @"C:\Users\nissa\source\repos\Test Files\telemetria.xlsx";
                if (!File.Exists(telemetryPath))
                {
                    LogSystemMessage("Error: Telemetry file not found at " + telemetryPath);
                    return;
                }

                DocumentReader reader = new DocumentReader();
                string[,] excelData = reader.ReadExcelToTwoDimensionalArray(telemetryPath);
                
                if (excelData == null || excelData.GetLength(0) < 2)
                {
                    LogSystemMessage("Error: Telemetry file is empty or invalid.");
                    return;
                }

                int rows = excelData.GetLength(0);
                int cols = excelData.GetLength(1);
                
                // Limit to 5 columns if there are more, or use all available
                int processCols = Math.Min(cols, 5);
                LogSystemMessage($"Record Testing: Processing {processCols} telemetry columns.");

                var schema = treeDbSchema.ItemsSource as List<DbNode>;
                var selectedTables = schema?.Where(t => t.IsSelected).ToList() ?? new List<DbNode>();

                List<TelemetryItem> telemetryItems = new List<TelemetryItem>();
                SchemaExplorer explorer = new SchemaExplorer();

                for (int c = 0; c < processCols; c++)
                {
                    string paramName = excelData[0, c]?.Trim();
                    if (string.IsNullOrEmpty(paramName)) paramName = $"T_{c + 1}";
                    
                    var item = new TelemetryItem { ParameterName = paramName, Unit = "Value" };
                    
                    List<double> values = new List<double>();
                    for (int r = 1; r < rows; r++)
                    {
                        if (double.TryParse(excelData[r, c], out double val)) values.Add(val);
                    }

                    if (values.Count > 0)
                    {
                        item.CurrentValue = values.Last().ToString("F2");
                        
                        double minVal = values.Min();
                        double maxVal = values.Max();
                        double range = maxVal - minVal;
                        if (range == 0) range = 1;

                        // Generate LiveCharts Series
                        item.ChartSeries = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Values = new ChartValues<double>(values),
                                PointGeometry = null,
                                Fill = Brushes.Transparent,
                                StrokeThickness = 2,
                                Stroke = (SolidColorBrush)Application.Current.Resources["AccentPurple"]
                            }
                        };
                        
                        // Default boundaries if no DB connection
                        item.MinValue = Math.Floor(minVal * 0.8);
                        item.MaxValue = Math.Ceiling(maxVal * 1.2);
                    }

                    // Try to get boundaries from DB for selected tables
                    foreach (var table in selectedTables)
                    {
                        var column = table.Children.FirstOrDefault(col => 
                            col.Name.Split(' ')[0].Equals(paramName, StringComparison.OrdinalIgnoreCase));
                        
                        if (column != null)
                        {
                            string colName = column.Name.Split(' ')[0];
                            var stats = await explorer.GetColumnStatsAsync(_settings.DbConnectionString, table.Name, colName);
                            if (stats.Max > stats.Min)
                            {
                                item.MinValue = stats.Min;
                                item.MaxValue = stats.Max;
                            }
                            break;
                        }
                    }

                    // Update Status based on boundaries
                    if (double.TryParse(item.CurrentValue, out double current))
                    {
                        if (current < item.MinValue || current > item.MaxValue) item.Status = "Alert";
                        else item.Status = "Normal";
                    }

                    telemetryItems.Add(item);
                }

                lstTelemetry.ItemsSource = telemetryItems;
                // Populate Global Chart
                var globalSeries = new SeriesCollection();
                var colors = new List<SolidColorBrush> { 
                    (SolidColorBrush)Application.Current.Resources["AccentPurple"],
                    Brushes.SeaGreen, Brushes.DodgerBlue, Brushes.Orange, Brushes.Crimson 
                };

                for (int i = 0; i < telemetryItems.Count; i++)
                {
                    var item = telemetryItems[i];
                    var series = new LineSeries
                    {
                        Title = item.ParameterName,
                        Values = (item.ChartSeries[0] as LineSeries).Values,
                        PointGeometry = null,
                        Fill = Brushes.Transparent,
                        Stroke = colors[i % colors.Count],
                        StrokeThickness = 2
                    };
                    globalSeries.Add(series);
                }
                chartGlobal.Series = globalSeries;

                LogSystemMessage("Record Testing: Telemetry Analysis updated.");
            }
            catch (Exception ex)
            {
                LogSystemMessage("Record Testing Error: " + ex.Message);
            }
        }
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            SaveSettings();
            _monitorLogClient?.Close();
            try { if (_serverProcess != null && !_serverProcess.HasExited) _serverProcess.Kill(); } catch { }
            try { if (_clientProcess != null && !_clientProcess.HasExited) _clientProcess.Kill(); } catch { }
            try { if (_tcpServerProcess != null && !_tcpServerProcess.HasExited) _tcpServerProcess.Kill(); } catch { }
            try { if (_tcpClientProcess != null && !_tcpClientProcess.HasExited) _tcpClientProcess.Kill(); } catch { }
            Application.Current.Shutdown();
        }
    }
}








