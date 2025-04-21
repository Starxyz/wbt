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
using Newtonsoft.Json;

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
        private const int KEEPALIVE_INTERVAL = 3000; // 3 秒检查一次

        private DateTime factoryStartDate;

        private List<PrintTemplate> _templates = new List<PrintTemplate>();
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

            factoryStartDate = DateTime.Parse("2022-01-01");

            LoadTemplates(); // 加载分类模板信息

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
            var tasks = new List<Task>
            {
                AutoStartTcpServerAsync(),
                AutoConnectModbusAsync(),
                AutoLoadDefaultTemplateAsync()
            };

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            OnLogMessage("所有自动启动任务已完成");
        }

        // 自动启动TCP服务器
        private async Task AutoStartTcpServerAsync()
        {
            if (!_tcpRunning)
            {
                btnTcpControl.Enabled = false;
                await ToggleTcpServerAsync(true);
                btnTcpControl.Enabled = true;
            }
        }


        // 自动连接Modbus
        private async Task AutoConnectModbusAsync()
        {
            try
            {
                await ConnectModbusAsync();
                btnModbusControl.Text = "断开Modbus";
                OnLogMessage("Modbus已自动连接");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "自动连接Modbus失败");
                OnLogMessage($"自动连接Modbus失败: {ex.Message}");
            }
        }

        // 异步加载默认打印模板
        private async Task AutoLoadDefaultTemplateAsync()
        {
            try
            {
                // 从配置文件获取默认打印模板路径
                string configTemplatePath = Application.StartupPath + "\\" + ConfigurationManager.AppSettings["DefaultTemplatePath"];
                string defaultTemplatePath = !string.IsNullOrEmpty(configTemplatePath)
                    ? configTemplatePath
                    : Path.Combine(Application.StartupPath, "default.btw");

                if (File.Exists(defaultTemplatePath))
                {
                    OnLogMessage($"开始加载默认模板: {defaultTemplatePath}");

                    // 使用Task.Run将耗时操作放入后台线程
                    await Task.Run(() => {
                        try
                        {
                            if (!engine.IsAlive)
                                engine.Start();

                            format = engine.Documents.Open(defaultTemplatePath);
                            _templateOpened = true;
                            selectedFilePath = defaultTemplatePath;
                        }
                        catch (Exception ex)
                        {
                            throw ex; // 重新抛出异常以便在外层捕获
                        }
                    });

                    OnLogMessage($"默认模板已成功加载: {defaultTemplatePath}");
                }
                else
                {
                    Logger.Warn("默认模板文件不存在: " + defaultTemplatePath);
                    OnLogMessage("默认模板文件不存在: " + defaultTemplatePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "自动加载默认模板失败");
                OnLogMessage($"自动加载默认模板失败: {ex.Message}");
            }
        }

        private void LoadTemplates()
        {
            try
            {
                string json = File.ReadAllText("templates.json", Encoding.UTF8);
                _templates = JsonConvert.DeserializeObject<List<PrintTemplate>>(json);
                Logger.Info($"模板文件加载成功，共{_templates.Count}个模板");
                OnLogMessage("模板文件加载成功，共" + _templates.Count + "个模板");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载模板文件失败");
                MessageBox.Show("加载模板文件失败：" + ex.Message);
                OnLogMessage("加载模板文件失败：" + ex.Message);
            }
        }

        private PrintTemplate FindTemplateByKey(string key)
        {
            return _templates.FirstOrDefault(t => t.key == key);
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
                string formattedValue = (rawValue / 1000.0).ToString("F2"); // "18.20"

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
                if (string.IsNullOrWhiteSpace(message)) return;

                // 用 char[] + count + options 的重载
                var parts = message.Split(new[] { '|' }, 2, StringSplitOptions.None);
                var key = parts[0];
                var slot = (parts.Length > 1) ? parts[1] : null;

                PrintTemplate(string.IsNullOrWhiteSpace(slot) ? key : $"{key}-{slot}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing received message");
            }
        }



        private void PrintTemplate(string key)
        {
            var tpl = FindTemplateByKey(key);
            if (tpl != null)
            {

                string weight = GetWeightFunction();
                string traceabilityCode = GenerateTraceabilityCode();
                Pint_model(1, tpl.productName, tpl.spec, weight, traceabilityCode, tpl.qrcode);
            }
            else
            {
                string weight = GetWeightFunction();
                string traceabilityCode = GenerateTraceabilityCode();
                Logger.Warn($"未找到模板: {key}");
                OnLogMessage($"未找到模板: {key}");
                //MessageBox.Show($"未找到模板: {key}", "模板错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        format.SubStrings["斤数"].Value = $"{weight} ";
                        format.SubStrings["生产日期"].Value = $"{date}";//$"生产日期：{date}";
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

        // 公共函数：处理TCP服务器启动/停止
        private async Task<bool> ToggleTcpServerAsync(bool startServer)
        {
            try
            {
                if (startServer)
                {
                    await _tcpServer.StartAsync();
                    _tcpRunning = true;

                    // 使用Invoke确保UI更新在UI线程上执行
                    if (btnTcpControl.InvokeRequired)
                    {
                        btnTcpControl.Invoke(new Action(() => {
                            btnTcpControl.Text = "停止TCP";
                            lblTcpStatus.Text = $"TCP: {_tcpServer.Endpoint}";
                        }));
                    }
                    else
                    {
                        btnTcpControl.Text = "停止TCP";
                        lblTcpStatus.Text = $"TCP: {_tcpServer.Endpoint}";
                    }

                    OnLogMessage("TCP服务器已启动");
                }
                else
                {
                    _tcpServer.Stop();
                    _tcpRunning = false;

                    // 使用Invoke确保UI更新在UI线程上执行
                    if (btnTcpControl.InvokeRequired)
                    {
                        btnTcpControl.Invoke(new Action(() => {
                            btnTcpControl.Text = "启动TCP";
                            lblTcpStatus.Text = "TCP: 未启动";
                        }));
                    }
                    else
                    {
                        btnTcpControl.Text = "启动TCP";
                        lblTcpStatus.Text = "TCP: 未启动";
                    }

                    OnLogMessage("TCP服务器已停止");
                }
                return true;
            }
            catch (Exception ex)
            {
                string operation = startServer ? "启动" : "停止";
                Logger.Error(ex, $"{operation}TCP服务失败");
                OnLogMessage($"{operation}TCP服务失败: {ex.Message}");

                // 如果操作失败，确保UI状态与实际状态一致
                if (startServer)
                {
                    _tcpRunning = false;

                    // 使用Invoke确保UI更新在UI线程上执行
                    if (btnTcpControl.InvokeRequired)
                    {
                        btnTcpControl.Invoke(new Action(() => {
                            btnTcpControl.Text = "启动TCP";
                            lblTcpStatus.Text = "TCP: 未启动";
                        }));
                    }
                    else
                    {
                        btnTcpControl.Text = "启动TCP";
                        lblTcpStatus.Text = "TCP: 未启动";
                    }
                }

                return false;
            }
        }

        private async void btnTcpControl_Click(object sender, EventArgs e)
        {
            btnTcpControl.Enabled = false;

            await ToggleTcpServerAsync(!_tcpRunning);

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
            var res = GenerateTraceabilityCode();
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

        public string GenerateTraceabilityCode()
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

    public class PrintTemplate
    {
        public string key { get; set; }
        public string productName { get; set; }
        public string spec { get; set; }
        public string qrcode { get; set; }
    }
}