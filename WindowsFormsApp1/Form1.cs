using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Seagull.BarTender.Print;
using NLog;
using EasyModbus;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public Engine engine = new Engine();//打印机 引擎
        public LabelFormatDocument format = null;//获取 模板内容
        private string selectedFilePath = null;
        private TcpServer _tcpServer;
        private bool _tcpRunning = false;
        private bool _templateOpened = false; // 新增标志，表示模板是否已打开
        private ModbusClient _modbusClient;
        private bool _modbusConnected = false;

        // 添加 Modbus 连接保持相关字段
        private System.Windows.Forms.Timer _modbusKeepAliveTimer;
        private DateTime _lastModbusActivity = DateTime.MinValue;
        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const int KEEPALIVE_INTERVAL = 30000; // 30 秒检查一次

        public Form1()
        {
            InitializeComponent();

            var port = int.Parse(ConfigurationManager.AppSettings["TcpPort"]);
            _tcpServer = new TcpServer(port);
            _tcpServer.LogMessage += OnLogMessage;
            _tcpServer.ServerStatusChanged += OnServerStatusChanged;
            _tcpServer.ConnectionStatusChanged += OnClientConnectionStatusChanged;
            _tcpServer.MessageReceived += OnMessageReceived;
            UpdateClientInfo("未连接");

            // 初始化Modbus客户端
            _modbusClient = new ModbusClient();
            _modbusClient.IPAddress = ConfigurationManager.AppSettings["ModbusIpAddress"];
            _modbusClient.Port = int.Parse(ConfigurationManager.AppSettings["ModbusPort"]);

            // 初始化 Modbus 连接保持定时器
            _modbusKeepAliveTimer = new System.Windows.Forms.Timer();
            _modbusKeepAliveTimer.Interval = KEEPALIVE_INTERVAL;
            _modbusKeepAliveTimer.Tick += ModbusKeepAlive_Tick;
        }

        // Modbus 连接保持检查方法
        private void ModbusKeepAlive_Tick(object sender, EventArgs e)
        {
            // 如果超过 KEEPALIVE_INTERVAL 毫秒没有活动，则执行心跳检查
            if ((DateTime.Now - _lastModbusActivity).TotalMilliseconds >= KEEPALIVE_INTERVAL)
            {
                Logger.Debug("执行 Modbus 连接保持检查");
                PerformModbusHeartbeat();
            }
        }

        // 执行 Modbus 心跳操作
        private void PerformModbusHeartbeat()
        {
            try
            {
                // 检查是否已连接
                if (_modbusClient == null || !_modbusClient.Connected)
                {
                    Logger.Info("Modbus 连接已断开，尝试重新连接...");
                    ReconnectModbusClient();
                    return;
                }

                // 执行简单的读取操作作为心跳
                _modbusClient.ReadHoldingRegisters(0, 1);

                // 更新最后活动时间戳
                _lastModbusActivity = DateTime.Now;
                _reconnectAttempts = 0; // 成功操作后重置重连计数器

                Logger.Debug("Modbus 连接保持成功");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Modbus 连接保持失败: {ex.Message}");
                ReconnectModbusClient();
            }
        }

        // 处理 Modbus 重新连接
        private void ReconnectModbusClient()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                Logger.Error($"在 {MAX_RECONNECT_ATTEMPTS} 次尝试后无法重新连接到 Modbus");
                _modbusKeepAliveTimer.Stop();
                UpdateWeightDisplay("--.-"); // 显示连接断开

                // 更新 UI 以反映断开状态
                if (_modbusConnected)
                {
                    _modbusConnected = false;
                    if (btnModbusControl.InvokeRequired)
                    {
                        btnModbusControl.Invoke(new Action(() => btnModbusControl.Text = "连接Modbus"));
                    }
                    else
                    {
                        btnModbusControl.Text = "连接Modbus";
                    }
                }
                return;
            }

            _reconnectAttempts++;

            try
            {
                // 如果存在，关闭现有连接
                if (_modbusClient != null && _modbusClient.Connected)
                {
                    _modbusClient.Disconnect();
                }

                // 在重新连接之前等待一段时间（每次尝试增加延迟）
                int delay = 500 * _reconnectAttempts;
                Logger.Info($"等待 {delay}ms 后进行第 {_reconnectAttempts} 次重连尝试");
                System.Threading.Thread.Sleep(delay);

                // 重新连接
                _modbusClient.Connect();
                _lastModbusActivity = DateTime.Now;
                _modbusConnected = true;

                Logger.Info("成功重新连接到 Modbus");
            }
            catch (Exception ex)
            {
                Logger.Error($"第 {_reconnectAttempts} 次重连尝试失败: {ex.Message}");
            }
        }

        private void OnLogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(OnLogMessage), message);
                return;
            }

            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}\r\n");
        }

        private void OnServerStatusChanged(bool started)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnServerStatusChanged), started);
                return;
            }

            lblTcpStatus.Text = started ? $"TCP: {_tcpServer.Endpoint}" : "TCP: 未启动";
            lblServerInfo.Text = started ? $"服务器: {_tcpServer.Endpoint}" : "服务器: 未启动";
        }

        private void OnClientConnectionStatusChanged(bool connected)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnClientConnectionStatusChanged), connected);
                return;
            }

            if (!connected)
            {
                UpdateClientInfo("未连接");
            }
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

        // 修改后的 GetWeightFunction 方法，以使用连接保持机制
        private string GetWeightFunction()
        {
            try
            {
                // 检查是否已连接，如果没有则连接
                if (_modbusClient == null || !_modbusClient.Connected)
                {
                    Logger.Info("Modbus 未连接，正在尝试连接...");

                    if (_modbusClient == null)
                    {
                        _modbusClient = new ModbusClient(_modbusClient.IPAddress, _modbusClient.Port); // 请替换为您的实际 IP 和端口
                        _modbusClient.ConnectionTimeout = 5000; // 设置超时时间为5秒
                    }

                    try
                    {
                        _modbusClient.Connect();
                        _modbusConnected = true;
                        _lastModbusActivity = DateTime.Now; // 更新活动时间戳

                        // 如果尚未运行，则启动连接保持定时器
                        if (!_modbusKeepAliveTimer.Enabled)
                        {
                            _modbusKeepAliveTimer.Start();
                        }

                        Logger.Info("成功连接到 Modbus 设备");
                    }
                    catch (Exception connEx)
                    {
                        Logger.Error(connEx, "连接 Modbus 设备失败");
                        UpdateWeightDisplay("--.-"); // 显示错误状态
                        return "--.-";
                    }
                }

                // 读取单个保持寄存器
                int[] result = _modbusClient.ReadHoldingRegisters(0, 10);
                Logger.Info($"读取到原始值: {result[0]}");

                // 更新最后活动时间戳
                _lastModbusActivity = DateTime.Now;

                // 获取原始寄存器值
                int rawValue = result[0]; // 例如 1820
                string formattedValue = (rawValue / 100.0).ToString("F2"); // "18.20"

                // 更新显示
                UpdateWeightDisplay(formattedValue);

                return formattedValue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "读取重量失败");

                // 让连接保持机制处理重新连接
                ReconnectModbusClient();

                UpdateWeightDisplay("--.-"); // 显示错误状态
                return "--.-";
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                if (message.StartsWith("CONNECT|"))
                {
                    var clientInfo = message.Substring(8);
                    UpdateClientInfo(clientInfo);
                }
                else if (message == "DISCONNECT")
                {
                    UpdateClientInfo("未连接");
                }
                else
                {
                    // Expected format: "品名|规格|斤数|生产日期|二维码"
                    var parts = message.Split('|');
                    if (parts.Length == 5)
                    {
                        Pint_model(1, parts[0], parts[1], parts[2], parts[3], parts[4]);
                    }
                    else
                    {
                        Logger.Warn($"Invalid message format: {message}");
                        var weight = GetWeightFunction();
                        if (parts[0] == "xmywkfxxjd")
                        {
                            Pint_model(1, "香满园无抗富硒鲜鸡蛋", "30枚盒10盒", weight, "8879");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing received message");
            }
        }

        public void Pint_model(int printnum, string productName = "46无抗鲜鸡蛋",
                             string spec = "360枚", string weight = "12",
                             string date = "20250204", string qrCode = "8879")
        {
            Logger.Info($"开始打印，数量: {printnum}");
            // 检查模板是否已打开
            if (!_templateOpened || format == null)
            {
                Logger.Error("模板未打开，请先选择并加载模板文件");
                MessageBox.Show("请先选择并加载模板文件", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btn_print.Enabled = false;

            try
            {
                // 确保引擎在运行
                if (!engine.IsAlive)
                {
                    engine.Start();
                }

                for (int i = 0; i < printnum; i++)
                {
                    try
                    {
                        // 设置打印数据
                        format.SubStrings["品名"].Value = $"品名：{productName}";
                        format.SubStrings["规格"].Value = $"规格：{spec}";
                        format.SubStrings["斤数"].Value = $"斤数：{weight} kg";
                        format.SubStrings["生产日期"].Value = " ";//$"生产日期：{date}";
                        format.SubStrings["二维码"].Value = qrCode;

                        // 执行打印
                        Result rel = format.Print(); // 获取打印状态
                        if (rel == Result.Success)
                        {
                            Logger.Info($"第 {i + 1} 份打印成功");
                        }
                        else
                        {
                            Logger.Error($"第 {i + 1} 份打印失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"第 {i + 1} 份打印异常: {ex.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"打印过程中发生异常: {ex.ToString()}");
            }
            finally
            {
                btn_print.Enabled = true;
                Logger.Info("打印任务完成");
            }

        }

        private void btn_print_Click(object sender, EventArgs e)
        {
            Pint_model(1);
        }

        private void btn_select_file_Click(object sender, EventArgs e)
        {
            Logger.Debug("开始选择文件");
            // 创建 OpenFileDialog 实例
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // 设置初始目录为程序运行目录
                openFileDialog.InitialDirectory = Application.StartupPath;

                // 可选：设置对话框标题
                openFileDialog.Title = "请选择文件";

                // 可选：设置文件过滤器
                openFileDialog.Filter = "BarTender 模板文件 (*.btw)|*.btw";

                // 可选：设置默认过滤器索引
                openFileDialog.FilterIndex = 1;

                // 可选：是否恢复上次选择的目录
                openFileDialog.RestoreDirectory = true;

                // 显示对话框并检查用户是否选择了文件
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 获取选择的文件路径
                    selectedFilePath = openFileDialog.FileName;
                    Logger.Info($"已选择文件: {selectedFilePath}");

                    // 关闭之前打开的模板
                    CloseTemplate();

                    // 打开引擎并加载模板
                    try
                    {
                        engine.Start();
                        Logger.Debug($"打开模板文件: {selectedFilePath}");
                        format = engine.Documents.Open(selectedFilePath);
                        _templateOpened = true;
                        MessageBox.Show($"模板文件已成功加载: {selectedFilePath}", "文件已加载",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"打开模板文件失败: {ex.Message}");
                        MessageBox.Show($"打开模板文件失败: {ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _templateOpened = false;
                    }
                }
            }
        }

        // 添加一个关闭模板的方法
        private void CloseTemplate()
        {
            if (format != null)
            {
                try
                {
                    // 使用 SaveOptions.DoNotSaveChanges 参数关闭模板
                    format.Close(Seagull.BarTender.Print.SaveOptions.DoNotSaveChanges);
                    format = null;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "关闭模板文件失败");
                }
            }

            if (engine != null && engine.IsAlive)
            {
                try
                {
                    engine.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "停止引擎失败");
                }
            }

            _templateOpened = false;
        }

        private async void btnTcpControl_Click(object sender, EventArgs e)
        {
            btnTcpControl.Enabled = false;

            if (!_tcpRunning)
            {
                try
                {
                    await _tcpServer.StartAsync();
                    _tcpRunning = true;
                    btnTcpControl.Text = "停止TCP";
                    lblTcpStatus.Text = $"TCP: {_tcpServer.Endpoint}";
                }
                catch (Exception ex)
                {
                    _tcpRunning = false;
                    btnTcpControl.Text = "启动TCP";
                    lblTcpStatus.Text = "TCP: 未启动";
                    Logger.Error(ex, "启动TCP服务失败");
                    MessageBox.Show($"启动TCP服务失败: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                try
                {
                    _tcpServer.Stop();
                    _tcpRunning = false;
                    btnTcpControl.Text = "启动TCP";
                    lblTcpStatus.Text = "TCP: 未启动";
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "停止TCP服务失败");
                    MessageBox.Show($"停止TCP服务失败: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            btnTcpControl.Enabled = true;
        }

        // 修改后的 OnFormClosing 方法，以停止连接保持定时器
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_modbusKeepAliveTimer != null && _modbusKeepAliveTimer.Enabled)
            {
                _modbusKeepAliveTimer.Stop();
            }

            if (_tcpRunning)
            {
                _tcpServer.Stop();
            }
            // 关闭模板和引擎
            CloseTemplate();
            base.OnFormClosing(e);
        }

        // 修改后的 ConnectModbusAsync 方法，以启动连接保持定时器
        private async Task ConnectModbusAsync()
        {
            try
            {
                _modbusClient.ConnectionTimeout = 5000;  // 5秒超时
                _modbusClient.Connect();

                _modbusConnected = true;
                _lastModbusActivity = DateTime.Now;
                _reconnectAttempts = 0;

                // 启动连接保持定时器
                if (!_modbusKeepAliveTimer.Enabled)
                {
                    _modbusKeepAliveTimer.Start();
                }

                OnLogMessage("Modbus连接成功");
            }
            catch (Exception ex)
            {
                _modbusConnected = false;
                OnLogMessage($"Modbus连接失败: {ex.Message}");
                Logger.Error(ex, "Modbus连接失败");
            }
        }

        // 修改后的 DisconnectModbus 方法，以停止连接保持定时器
        private void DisconnectModbus()
        {
            try
            {
                // 停止连接保持定时器
                if (_modbusKeepAliveTimer.Enabled)
                {
                    _modbusKeepAliveTimer.Stop();
                }

                _modbusClient.Disconnect();
                _modbusConnected = false;

                OnLogMessage("Modbus已断开");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Modbus断开失败");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            GetWeightFunction();
            var res = GenerateTraceabilityCode(DateTime.Parse("2022-01-01"));
            OnLogMessage(res);
        }

       

        private void btnModbusControl_Click_1(object sender, EventArgs e)
        {
            btnModbusControl.Enabled = false;

            if (!_modbusConnected)
            {
                try
                {
                    ConnectModbusAsync().Wait();
                    btnModbusControl.Text = "断开Modbus";
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Modbus连接失败");
                    MessageBox.Show($"Modbus连接失败: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                DisconnectModbus();
                btnModbusControl.Text = "连接Modbus";
            }

            btnModbusControl.Enabled = true;
        }

        public string GenerateTraceabilityCode(DateTime factoryStartDate)
        {
            // Get current date
            DateTime currentDate = DateTime.Now;

            // 1. First character: Factory code (fixed as 'N')
            char factoryCode = 'N';

            // 2. Second character: First digit of the day
            int dayFirstDigit = currentDate.Day / 10;
            char[] dayFirstDigitMap = { 'A', 'B', 'C', 'D' }; // 0:A, 1:B, 2:C, 3:D
            char mappedDayFirstDigit = dayFirstDigitMap[dayFirstDigit];

            // 3. Third character: Last digit of the day
            int dayLastDigit = currentDate.Day % 10;
            char mappedDayLastDigit = (char)('A' + dayLastDigit); // 0:A, 1:B, ..., 9:J

            // 4. Fourth character: Years of factory operation
            int yearsOfOperation = currentDate.Year - factoryStartDate.Year;
            if (currentDate < factoryStartDate.AddYears(yearsOfOperation))
            {
                yearsOfOperation--; // Adjust if we haven't reached the anniversary date yet
            }
            // Ensure the value is between 1 and 10
            yearsOfOperation = Math.Max(1, Math.Min(10, yearsOfOperation));
            char mappedYearsOfOperation = (char)('A' + (yearsOfOperation - 1)); // 1:A, 2:B, ..., 10:J

            // 5. Fifth character: Month of production date
            int month = currentDate.Month;
            char mappedMonth = (char)('A' + (month - 1)); // 1:A, 2:B, ..., 12:L

            // Combine all characters to form the traceability code
            string traceabilityCode = new string(new[] {
                factoryCode,
                mappedDayFirstDigit,
                mappedDayLastDigit,
                mappedYearsOfOperation,
                mappedMonth
            });

            return traceabilityCode;
        }
    }
}