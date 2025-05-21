using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Seagull.BarTender.Print;
using NLog;
using EasyModbus;
using Newtonsoft.Json;
using System.Globalization;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ITcpService _tcpService;
        private IPrintingService _printingService;
        private IApplicationOrchestrator _appOrchestrator;
        private IModbusService _modbusService; 
        // private bool _templateOpened = false; // Managed by PrintingService
        // ModbusClient and related fields (_modbusConnected, _modbusKeepAliveTimer, etc.) removed

        // private List<PrintTemplate> _templates = new List<PrintTemplate>(); // Managed by PrintingService
        private ProductRuleManager _productRuleManager; // Will be passed to AppOrchestrator
        public Form1()
        {
            InitializeComponent();

            // 获取程序集编译时间作为版本号
            DateTime buildDate = GetBuildDate();
            string versionString = buildDate.ToString("yyyyMMdd-HHmm");
            this.Text = $"打印控制系统 v{versionString}";
            Logger.Info($"应用程序启动，版本: {versionString}");

            var port = int.Parse(ConfigurationManager.AppSettings["TcpPort"]);
            _tcpService = new TcpService(port);
            _tcpService.MessageReceived += Form1_MessageReceived_Handler;
            _tcpService.ServerStatusChanged += Form1_ServerStatusChanged_Handler;
            _tcpService.ClientConnectionChanged += Form1_ClientConnectionChanged_Handler;
            _tcpService.LogMessageRequested += (msg) => OnLogMessage($"[TcpSvc] {msg}");
            UpdateClientInfo("未连接");

            // Initialize PrintingService
            _printingService = new PrintingService();
            _printingService.LogMessageRequested += (msg) => OnLogMessage($"[PrintSvc] {msg}");
            _printingService.PrintSuccessOccurred += (msg) => { OnLogMessage(msg); /* TODO: Add any other UI feedback for success */ };
            _printingService.PrintFailureOccurred += (msg) => { 
                OnLogMessage(msg); 
                MessageBox.Show(msg, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            };

            // Initialize ModbusService (IModbusService)
            string modbusIpAddress = ConfigurationManager.AppSettings["ModbusIpAddress"];
            int modbusPortNumber = int.Parse(ConfigurationManager.AppSettings["ModbusPort"]);
            _modbusService = new ModbusService(modbusIpAddress, modbusPortNumber);
            _modbusService.ConnectionStatusChanged += OnModbusConnectionStatusChanged;
            _modbusService.LogMessageRequested += (msg) => OnLogMessage($"[ModbusSvc] {msg}");
            // UpdateWeightDisplay("--.-"); // Initial state for weight

            // 初始化产品规则管理器，程序启动时重置所有规则的允许打印状态为不允许打印
            _productRuleManager = new ProductRuleManager(resetAllowPrintStatus: true); // Must be initialized before AppOrchestrator

            // Initialize ApplicationOrchestrator
            _appOrchestrator = new ApplicationOrchestrator(_modbusService, _productRuleManager, _printingService);
            _appOrchestrator.LogMessageRequested += (msg) => OnLogMessage($"[Orchestrator] {msg}");
            _appOrchestrator.DetailedMatchFailureLogRequested += (msg) => OnLogMessage($"[OrchestratorDetail] {msg}");
            _appOrchestrator.WeightAvailable += Form1_WeightAvailable_Handler;


            // 使用Timer在UI初始化完成后执行自动启动操作
            System.Windows.Forms.Timer startupTimer = new System.Windows.Forms.Timer();
            startupTimer.Interval = 500; // 500毫秒后执行，确保UI已完全加载
            startupTimer.Tick += StartupTimer_Tick;
            startupTimer.Start();
        }

        private async void StartupTimer_Tick(object sender, EventArgs e)
        {
            // 停止定时器，防止重复执行
            ((System.Windows.Forms.Timer)sender).Stop();

            // 执行自动启动操作（并行执行以提高效率）
            // Fetch default template path from config for AutoLoadDefaultTemplateAsync
            string defaultTemplatePathConfigValue = Application.StartupPath + "\\" + ConfigurationManager.AppSettings["DefaultTemplatePath"];
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["DefaultTemplatePath"]))
            {
                 defaultTemplatePathConfigValue = Path.Combine(Application.StartupPath, "default.btw");
            }


            var tasks = new List<Task>
            {
                AutoStartTcpServerAsync(),
                AutoConnectModbusAsync(),
                _printingService.AutoLoadDefaultTemplateAsync(defaultTemplatePathConfigValue)
            };

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            OnLogMessage("所有自动启动任务已完成");
        }

        // 自动启动TCP服务器
        private async Task AutoStartTcpServerAsync()
        {
            if (!_tcpService.IsRunning) // Check IsRunning from the service
            {
                btnTcpControl.Enabled = false; // Disable button during operation
                try
                {
                    await _tcpService.StartAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "自动启动TCP服务失败");
                    OnLogMessage($"自动启动TCP服务失败: {ex.Message}");
                    // UI update will be handled by Form1_ServerStatusChanged_Handler
                }
                finally
                {
                    btnTcpControl.Enabled = true; // Re-enable button
                }
            }
        }


        // 自动连接Modbus
        private async Task AutoConnectModbusAsync()
        {
            try
            {
                await _modbusService.ConnectAsync();
                // UI update will be handled by OnModbusConnectionStatusChanged
                // OnLogMessage("Modbus已自动连接"); // Logged by ModbusService
            }
            catch (Exception ex) // Should be rare if ModbusService handles its own exceptions
            {
                Logger.Error(ex, "自动连接Modbus时发生未预料的错误");
                OnLogMessage($"自动连接Modbus时发生未预料的错误: {ex.Message}");
            }
        }

        // ModbusKeepAlive_Tick, PerformModbusHeartbeat, ReconnectModbusClient are removed (handled by ModbusService)

        // 日志最大行数，超过这个数量将清除旧日志
        private const int MAX_LOG_LINES = 1000;
        // 当日志行数超过这个阈值时，将清除到这个数量
        private const int CLEAN_TO_LOG_LINES = 800;

        private void OnLogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(OnLogMessage), message);
                return;
            }

            // 添加新日志
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}\r\n");

            // 检查日志行数是否超过最大限制
            if (txtLog.Lines.Length > MAX_LOG_LINES)
            {
                // 计算需要保留的文本
                int linesToRemove = txtLog.Lines.Length - CLEAN_TO_LOG_LINES;
                if (linesToRemove > 0)
                {
                    // 找到要删除的文本的结束位置
                    int removeEndPos = 0;
                    for (int i = 0; i < linesToRemove; i++)
                    {
                        removeEndPos += txtLog.Lines[i].Length + 2; // +2 for \r\n
                    }

                    // 删除旧日志
                    txtLog.Select(0, removeEndPos);
                    txtLog.SelectedText = "";

                    // 添加一条提示信息
                    txtLog.AppendText($"{DateTime.Now:HH:mm:ss} [系统] 已清除 {linesToRemove} 行旧日志以提高性能\r\n");
                }
            }

            // 滚动到最后一行
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void Form1_ServerStatusChanged_Handler(bool isRunning, string endpointInfo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool, string>(Form1_ServerStatusChanged_Handler), isRunning, endpointInfo);
                return;
            }
            lblTcpStatus.Text = isRunning ? $"TCP: {endpointInfo}" : "TCP: 未启动";
            lblServerInfo.Text = isRunning ? $"服务器: {endpointInfo}" : "服务器: 未启动";
            btnTcpControl.Text = isRunning ? "停止TCP" : "启动TCP";
            // OnLogMessage($"TCP Server status: {(isRunning ? "Running" : "Stopped")} on {endpointInfo}"); // Logged by TcpService
        }

        private void Form1_ClientConnectionChanged_Handler(bool isConnected, string clientInfo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool, string>(Form1_ClientConnectionChanged_Handler), isConnected, clientInfo);
                return;
            }
            // This provides generic connected/disconnected status.
            // Specific client IP for lblClientInfo is updated by "CONNECT|" message in Form1_MessageReceived_Handler.
            if (!isConnected)
            {
                UpdateClientInfo("未连接"); // Reset if the service indicates a disconnection
            }
            // OnLogMessage($"TCP Client status: {(isConnected ? "Connected" : "Disconnected")}. Info: {clientInfo}"); // Logged by TcpService
        }
        private void UpdateClientInfo(string clientInfo)
        {
            if (lblClientInfo.InvokeRequired)
            {
                lblClientInfo.Invoke(new Action<string>(UpdateClientInfo), clientInfo);
                return;
            }
            lblClientInfo.Text = $"客户端: {clientInfo}";
        }
        // 辅助方法：更新界面显示
        private void UpdateWeightDisplay(string weightValue)
        {
            if (lblWeight.InvokeRequired)
            {
                lblWeight.Invoke(new Action(() => lblWeight.Text = $"重量: {weightValue}kg"));
            }
            else
            {
                lblWeight.Text = $"重量: {weightValue}kg";
            }
        }

        // GetWeightFunction removed (handled by ModbusService, accessed via Orchestrator)

        // Renamed from OnMessageReceived to Form1_MessageReceived_Handler
        // Now delegates core processing to ApplicationOrchestrator
        private async void Form1_MessageReceived_Handler(string message)
        {
            // Logger.Debug($"Form1_MessageReceived_Handler received: {message}"); // Logged by TcpService

            if (message.StartsWith("CONNECT|"))
            {
                string clientInfo = message.Substring(8); // 去掉"CONNECT|"前缀
                // Logger.Debug($"接收到客户端连接消息，客户端信息: {clientInfo}"); // Logged by TcpService
                UpdateClientInfo(clientInfo);
                return;
            }
            else if (message == "DISCONNECT")
            {
                // Logger.Debug("接收到客户端断开消息"); // Logged by TcpService
                UpdateClientInfo("未连接");
                return;
            }

            // For all other messages, delegate to the orchestrator
            // This runs the orchestrator's processing asynchronously
            await _appOrchestrator.ProcessIncomingMessageAsync(message);
        }

        // ProcessMessageWithLegacyMethod is removed from Form1, now inside ApplicationOrchestrator

        // btn_print_Click still uses PrintingService directly, as it's a UI-initiated action
        // not directly tied to an incoming TCP message processed by orchestrator.
        private async void btn_print_Click(object sender, EventArgs e)
        {
            // Example: Create a default PrintJobData or get values from UI if available
            PrintJobData jobData = new PrintJobData
            {
                ProductName = "46无抗鲜鸡蛋", // Example value
                Specification = "360枚",    // Example value
                Weight = "12",            // Example value
                DateCode = TraceabilityCodeGenerator.GenerateTraceabilityCode(), // Use current date/logic
                QrCode = "8879",          // Example value
                Quantity = 1              // Example value
            };

            btn_print.Enabled = false;
            Logger.Debug("禁用打印按钮，防止重复点击 (manual print)");
            try
            {
                await _printingService.ExecutePrintJobAsync(jobData);
            }
            finally
            {
                btn_print.Enabled = true;
                Logger.Debug("启用打印按钮 (manual print)");
            }
        }

        /// <summary>
        /// 获取程序集的编译时间
        /// </summary>
        /// <returns>编译时间</returns>
        private DateTime GetBuildDate()
        {
            try
            {
                // 获取程序集文件路径
                string filePath = Assembly.GetExecutingAssembly().Location;
                // 获取文件的链接时间（编译时间）
                DateTime buildDate = File.GetLastWriteTime(filePath);
                return buildDate;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取编译时间失败");
                // 如果获取失败，返回当前时间
                return DateTime.Now;
            }
        }

        private async void btn_select_file_Click(object sender, EventArgs e)
        {
            Logger.Debug("开始选择文件");
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Application.StartupPath;
                openFileDialog.Title = "请选择BarTender模板文件";
                openFileDialog.Filter = "BarTender 模板文件 (*.btw)|*.btw";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFile = openFileDialog.FileName;
                    Logger.Info($"用户选择了文件: {selectedFile}");
                    
                    // Use the PrintingService to load the template
                    bool success = await _printingService.LoadTemplateAsync(selectedFile);
                    if (success)
                    {
                        MessageBox.Show($"模板文件已成功加载: {selectedFile}", "文件已加载", 
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        OnLogMessage($"模板文件已成功加载: {selectedFile}");
                    }
                    else
                    {
                        // LogMessageRequested and MessageBox for error are handled by PrintingService events
                        // MessageBox.Show($"打开模板文件失败: {selectedFile}", "错误", 
                        //                 MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

private async void btnTcpControl_Click(object sender, EventArgs e)
        {
    btnTcpControl.Enabled = false;
            try
            {
        if (!_tcpService.IsRunning)
                {
            await _tcpService.StartAsync();
                }
                else
                {
            _tcpService.Stop();
                }
        // UI updates are handled by Form1_ServerStatusChanged_Handler
            }
            catch (Exception ex)
            {
        Logger.Error(ex, "TCP 控制按钮操作失败");
        OnLogMessage($"TCP 控制按钮操作失败: {ex.Message}");
        // Ensure button text and UI reflects actual state if error occurs
        // This might require manually calling Form1_ServerStatusChanged_Handler if the event from service isn't guaranteed on error
        // For now, relying on service's event for status.
    }
    finally
    {
        btnTcpControl.Enabled = true;
            }
        }

// 修改后的 OnFormClosing 方法
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // _modbusKeepAliveTimer.Stop(); // Timer is managed by ModbusService
            _tcpService?.Dispose(); 
            _printingService?.Dispose(); 
            _modbusService?.Dispose(); // Dispose the Modbus service
            // If AppOrchestrator becomes IDisposable, dispose it too.

            base.OnFormClosing(e);
        }

        // ConnectModbusAsync and DisconnectModbus (old direct _modbusClient methods) are removed.
        // UI updates for Modbus connection are handled by OnModbusConnectionStatusChanged.

        private void OnModbusConnectionStatusChanged(bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnModbusConnectionStatusChanged), isConnected);
                return;
            }
            btnModbusControl.Text = isConnected ? "断开Modbus" : "连接Modbus";
            // OnLogMessage($"Modbus Connection Status: {(isConnected ? "Connected" : "Disconnected")}"); // Logged by ModbusService
            if (!isConnected)
            {
                UpdateWeightDisplay("--.-"); // Clear weight display on disconnect
            }
        }

        private void Form1_WeightAvailable_Handler(string weight)
        {
            UpdateWeightDisplay(weight);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            TemplateManagerForm templateManagerForm = new TemplateManagerForm();
            templateManagerForm.ShowDialog();  // 打开模板管理窗口
        }

        private void btnProductRules_Click(object sender, EventArgs e)
        {
            ProductRuleForm form = new ProductRuleForm();
            // 设置窗体为最大化状态
            form.WindowState = FormWindowState.Maximized;
            form.ShowDialog();

            // 重新加载产品规则，但不重置规则的允许打印状态
            _productRuleManager = new ProductRuleManager(resetAllowPrintStatus: false);
            OnLogMessage("已重新加载产品规则，保留了允许打印的设置");
        }



        private async void btnModbusControl_Click_1(object sender, EventArgs e)
        {
            btnModbusControl.Enabled = false;
            try
            {
                if (!_modbusService.IsConnected)
                {
                    await _modbusService.ConnectAsync();
                }
                else
                {
                    _modbusService.Disconnect();
                }
                // UI update is handled by OnModbusConnectionStatusChanged
            }
            catch (Exception ex) // Should be rare if ModbusService handles its own exceptions
            {
                Logger.Error(ex, "Modbus 控制按钮操作失败");
                OnLogMessage($"Modbus 控制按钮操作失败: {ex.Message}");
                // Ensure button text reflects actual state if error occurs
                OnModbusConnectionStatusChanged(_modbusService.IsConnected); 
            }
            finally
            {
                btnModbusControl.Enabled = true;
            }
        }

        /// <summary>
        /// 获取匹配失败的详细原因
        /// </summary>
        /// <param name="category">品类</param>
        /// <param name="chickenHouse">鸡舍号</param>
        /// <param name="customerName">客户名</param>
        /// <param name="weight">重量</param>
        /// <returns>详细的失败原因</returns>
        private string GetMatchingFailureReason(string category, string chickenHouse, string customerName, double weight)
        {
            // 使用优化后的FindMatchingRule方法获取匹配结果
            var matchResult = _productRuleManager.FindMatchingRule(category, chickenHouse, customerName, weight);

            // 如果匹配成功，返回空字符串
            if (matchResult.IsSuccess)
            {
                return string.Empty;
            }

            // 如果匹配失败，返回失败原因
            return matchResult.FailureReason;
        }
    }
}