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
using System.Globalization;

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
        private ProductRuleManager _productRuleManager;
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

            // 初始化产品规则管理器，程序启动时重置所有规则的允许打印状态为不允许打印
            _productRuleManager = new ProductRuleManager(resetAllowPrintStatus: true);

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

            if (connected)
            {
                // 客户端连接时，我们还不知道具体的客户端信息
                // 这个信息会在MessageReceived事件中通过CONNECT消息提供
                UpdateClientInfo("已连接");
            }
            else
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
            Logger.Debug($"开始处理接收到的消息: {message}");
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    Logger.Warn("接收到空消息，忽略处理");
                    return;
                }

                // 检查是否是连接/断开消息
                if (message.StartsWith("CONNECT|"))
                {
                    string clientInfo = message.Substring(8); // 去掉"CONNECT|"前缀
                    Logger.Debug($"接收到客户端连接消息，客户端信息: {clientInfo}");
                    UpdateClientInfo(clientInfo);
                    // 不再添加日志，因为TcpServer.cs中已经添加了
                    return;
                }
                else if (message == "DISCONNECT")
                {
                    Logger.Debug("接收到客户端断开消息");
                    UpdateClientInfo("未连接");
                    // 不再添加日志，因为TcpServer.cs中已经添加了
                    return;
                }

                // 解析消息
                // 格式：品类|鸡舍|版面状态|客户名
                var parts = message.Split(new[] { '|' }, StringSplitOptions.None);
                Logger.Debug($"消息解析为 {parts.Length} 个部分");

                // 提取消息中的信息
                string category = parts.Length > 0 ? parts[0] : null;
                string chickenHouse = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;
                string panelStatus = parts.Length > 2 ? parts[2] : null;

                // 提取客户名，如果长度大于20，则设置为null
                string customerName = null;
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && !parts[3].Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts[3].Length <= 20)
                    {
                        customerName = parts[3];
                    }
                    else
                    {
                        Logger.Warn($"客户名长度超过20个字符，已忽略: {parts[3]}");
                    }
                }

                // 记录接收到的消息
                Logger.Info($"接收到消息: 品类={category}, 鸡舍={chickenHouse}, 版面状态={panelStatus}, 客户名={customerName}");
                OnLogMessage($"接收到消息: 品类={category}, 鸡舍={chickenHouse}, 版面状态={panelStatus}");



                // 如果没有品类，无法处理
                if (string.IsNullOrWhiteSpace(category))
                {
                    string rejectReason = "消息中缺少品类信息，无法确定打印模板";
                    Logger.Warn("消息中缺少品类信息，无法处理");
                    OnLogMessage($"不执行打印，原因: {rejectReason}");
                    return;
                }

                // 检查版面状态，如果为0则不打印
                if (panelStatus == "0")
                {
                    string rejectReason = "版面状态为0，表示不需要打印";
                    Logger.Debug("版面状态为0，不执行打印");
                    OnLogMessage($"不执行打印，原因: {rejectReason}");
                    return;
                }

                // 获取重量
                Logger.Debug("开始获取重量数据");
                string weightStr = GetWeightFunction().Trim();
                Logger.Debug($"获取到重量数据: {weightStr}");

                if (weightStr == "--.-" || !double.TryParse(weightStr.Replace(',', '.'),
                                     NumberStyles.Any,
                                     CultureInfo.InvariantCulture,
                                     out double weight))
                {
                    string rejectReason = $"无法获取有效重量值: {weightStr}，请检查称重设备连接";
                    Logger.Warn($"无法获取有效重量: {weightStr}");
                    OnLogMessage($"不执行打印，原因: {rejectReason}");
                    return;
                }

                // 使用新的产品规则管理器查找匹配的规则
                // 注意：我们使用category（品类）作为版面信息，而不是panelStatus
                Logger.Info($"开始查找匹配规则: 品类={category}, 鸡舍={chickenHouse}, 客户名={customerName}, 重量={weight}");
                OnLogMessage($"开始查找匹配规则: 品类={category}, 鸡舍={chickenHouse ?? "未指定"}, 客户名={customerName ?? "未指定"}, 重量={weight}");
                ProductRule matchedRule = _productRuleManager.FindMatchingRule(category, chickenHouse, customerName, weight);

                if (matchedRule != null)
                {
                    Logger.Info($"找到匹配规则: ID={matchedRule.Id}, 品名={matchedRule.ProductName}, 规格={matchedRule.Specification}");
                    // 记录匹配ID号，同时使用原始消息内容
                    string logMessage = $"接收到的（消息：{message}，匹配ID号{matchedRule.Id}）";
                    Logger.Info(logMessage);
                    OnLogMessage(logMessage);

                    // 检查是否允许打印
                    if (!matchedRule.AllowPrint)
                    {
                        string rejectReason = $"规则ID={matchedRule.Id}, 品名={matchedRule.ProductName}, 规格={matchedRule.Specification}, 重量范围=[{matchedRule.WeightLowerLimit}-{matchedRule.WeightUpperLimit}]";
                        Logger.Info($"根据规则 {matchedRule.Id} 拒绝打印: 品类={category}, 鸡舍={chickenHouse}, 重量={weight}");
                        OnLogMessage($"根据规则拒绝打印: 品类={category}, 鸡舍={chickenHouse ?? "未指定"}, 重量={weight}, 原因: {rejectReason}");
                        return;
                    }

                    // 打印
                    string traceabilityCode = GenerateTraceabilityCode();
                    Logger.Info($"生成追溯码: {traceabilityCode}");

                    Logger.Info($"开始执行打印: 品名={matchedRule.ProductName}, 规格={matchedRule.Specification}, 重量={weightStr}, 二维码={matchedRule.QRCode}");
                    Pint_model(1,
                               matchedRule.ProductName,
                               matchedRule.Specification,
                               weightStr,
                               traceabilityCode,
                               matchedRule.QRCode);

                    Logger.Info($"使用规则 {matchedRule.Id} 打印: 品名={matchedRule.ProductName}, 规格={matchedRule.Specification}, 二维码={matchedRule.QRCode}, 重量={weightStr}");
                    OnLogMessage($"打印成功: 品名={matchedRule.ProductName}, 规格={matchedRule.Specification}, 重量={weightStr}");
                }
                else
                {
                    // 获取匹配失败的详细原因
                    string failureReason = GetMatchingFailureReason(category, chickenHouse, customerName, weight);

                    string rejectReason = $"未找到匹配规则，品类={category}, 鸡舍={chickenHouse ?? "未指定"}, 客户名={customerName ?? "未指定"}, 重量={weight}";
                    Logger.Info($"未找到匹配规则: 品类={category}, 鸡舍={chickenHouse ?? "未指定"}, 客户名={customerName ?? "未指定"}, 重量={weight}");

                    // 显示更详细的失败原因到界面
                    OnLogMessage($"【匹配失败】不执行打印，原因: {rejectReason}");
                    // 记录匹配失败，同时使用原始消息内容
                    string logMessage = $"接收到的（消息：{message}，未匹配到规则）";
                    Logger.Info(logMessage);
                    OnLogMessage(logMessage);

                    // 如果有详细的失败原因，也显示出来
                    if (!string.IsNullOrEmpty(failureReason))
                    {
                        OnLogMessage($"【详细原因】{failureReason}");
                    }

                    // 如果没有找到匹配的规则，尝试使用旧的模板方式
                    //ProcessMessageWithLegacyMethod(category, chickenHouse, panelStatus, weightStr);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理接收消息时出错");
                OnLogMessage($"处理消息出错: {ex.Message}");
            }
            finally
            {
                Logger.Debug("消息处理完成");
            }
        }

        private void ProcessMessageWithLegacyMethod(string key, string slot, string status, string weightStr)
        {
            Logger.Debug($"开始使用旧方法处理消息: 品类={key}, 鸡舍={slot}, 版面状态={status}, 重量={weightStr}");
            try
            {
                // 旧的处理逻辑
                if (string.IsNullOrWhiteSpace(key))
                {
                    Logger.Warn("品类为空，无法处理");
                    return;
                }

                if (status == "0")
                {
                    string rejectReason = "版面状态为0，表示不需要打印";
                    Logger.Debug("版面状态为0，不执行打印");
                    OnLogMessage($"不执行打印，原因: {rejectReason}");
                    return;
                }

                // ② slot 是否有效
                bool hasSlot = !string.IsNullOrWhiteSpace(slot) &&
                               !slot.Equals("null", StringComparison.OrdinalIgnoreCase);
                Logger.Debug($"鸡舍是否有效: {hasSlot}");

                // ③ xmyjpxjd360 + slot 无效 → 不打印
                if (key.Equals("xmyjpxjd360", StringComparison.OrdinalIgnoreCase) && !hasSlot)
                {
                    string rejectReason = "品类为xmyjpxjd360但鸡舍信息无效，此品类需要有效的鸡舍信息";
                    Logger.Debug("品类为xmyjpxjd360且鸡舍无效，不执行打印");
                    OnLogMessage($"不执行打印，原因: {rejectReason}");
                    return;
                }

                string templateKey = hasSlot ? $"{key}-{slot}" : key;
                Logger.Debug($"生成模板键: {templateKey}");

                string qrOverride = null;        // 默认不改二维码

                // ⑤ 如为 xmyjpxjd360 → 区间 + 二维码校验
                if (key.Equals("xmyjpxjd360", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("品类为xmyjpxjd360，进行重量区间和二维码校验");

                    if (!double.TryParse(weightStr.Replace(',', '.'),
                                         NumberStyles.Any,
                                         CultureInfo.InvariantCulture,
                                         out double w))
                    {
                        string rejectReason = $"无法解析重量值: {weightStr}，请检查重量数据格式";
                        Logger.Warn($"无法解析重量: {weightStr}");
                        OnLogMessage($"不执行打印，原因: {rejectReason}");
                        return;
                    }

                    Logger.Debug($"解析后的重量值: {w}");

                    if (w >= 20.5 && w <= 21.1)
                    {
                        qrOverride = "1790";
                        Logger.Debug($"重量 {w} 在区间 [20.5-21.1]，使用二维码: 1790");
                    }
                    else if (w >= 22.0 && w <= 22.4)
                    {
                        qrOverride = "1791";
                        Logger.Debug($"重量 {w} 在区间 [22.0-22.4]，使用二维码: 1791");
                    }
                    else if (w >= 23.9 && w <= 24.1)
                    {
                        qrOverride = "1792";
                        Logger.Debug($"重量 {w} 在区间 [23.9-24.1]，使用二维码: 1792");
                    }
                    else if (w >= 15.8 && w <= 16.6)
                    {
                        qrOverride = "1793";
                        Logger.Debug($"重量 {w} 在区间 [15.8-16.6]，使用二维码: 1793");
                    }
                    else
                    {
                        string rejectReason = $"重量 {w} 不在任何指定区间内 (有效区间: [20.5-21.1], [22.0-22.4], [23.9-24.1], [15.8-16.6])";
                        Logger.Debug($"重量 {w} 不在任何指定区间内，不执行打印");
                        OnLogMessage($"不执行打印，原因: {rejectReason}");
                        return;
                    }                // 不在三段区间 → 不打印
                }

                // ⑥ 打印
                Logger.Debug($"开始使用模板打印: 模板={templateKey}, 重量={weightStr}, 二维码={qrOverride ?? "默认"}");
                PrintTemplate(templateKey, weightStr, qrOverride);
                Logger.Info($"使用旧方法打印: 模板={templateKey}, 重量={weightStr}, 二维码={qrOverride ?? "默认"}");
                OnLogMessage($"使用旧方法打印: 模板={templateKey}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "使用旧方法处理消息时出错");
                OnLogMessage($"使用旧方法处理消息时出错: {ex.Message}");
            }
            finally
            {
                Logger.Debug("旧方法处理消息完成");
            }
        }





        private void PrintTemplate(string key, string weight, string overrideQrCode = null)
        {
            Logger.Debug($"开始查找模板: {key}");
            var tpl = FindTemplateByKey(key);
            if (tpl != null)
            {
                Logger.Debug($"找到模板: {key}, 品名={tpl.productName}, 规格={tpl.spec}, 默认二维码={tpl.qrcode}");

                string traceabilityCode = GenerateTraceabilityCode();
                Logger.Debug($"生成追溯码: {traceabilityCode}");

                string qrCodeToUse = overrideQrCode ?? tpl.qrcode;
                Logger.Debug($"使用二维码: {qrCodeToUse}" + (overrideQrCode != null ? " (覆盖默认值)" : " (使用默认值)"));

                Logger.Debug($"开始执行打印: 品名={tpl.productName}, 规格={tpl.spec}, 重量={weight}, 二维码={qrCodeToUse}");
                Pint_model(1,
                           tpl.productName,
                           tpl.spec,
                           weight,
                           traceabilityCode,
                           qrCodeToUse);

                Logger.Info($"模板打印成功: 模板={key}, 品名={tpl.productName}, 规格={tpl.spec}, 重量={weight}, 二维码={qrCodeToUse}");
            }
            else
            {
                string rejectReason = $"未找到匹配的打印模板: {key}，请检查模板配置";
                Logger.Warn($"未找到模板: {key}");
                OnLogMessage($"不执行打印，原因: {rejectReason}");
            }
        }



        public void Pint_model(int printnum, string productName = "46无抗鲜鸡蛋",
                             string spec = "360枚", string weight = "12",
                             string date = "20250204", string qrCode = "8879")
        {
            var startTime = DateTime.Now;
            Logger.Info($"开始打印任务: 数量={printnum}, 品名={productName}, 规格={spec}, 重量={weight}, 日期={date}, 二维码={qrCode}");

            // 检查模板是否已打开
            if (!_templateOpened || format == null)
            {
                Logger.Error("模板未打开，请先选择并加载模板文件");
                OnLogMessage("错误: 模板未打开，请先选择并加载模板文件");
                MessageBox.Show("请先选择并加载模板文件", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btn_print.Enabled = false;
            Logger.Debug("禁用打印按钮，防止重复点击");

            try
            {
                // 确保引擎在运行
                if (!engine.IsAlive)
                {
                    Logger.Debug("打印引擎未运行，正在启动...");
                    engine.Start();
                    Logger.Debug("打印引擎启动成功");
                }
                else
                {
                    Logger.Debug("打印引擎已在运行状态");
                }

                for (int i = 0; i < printnum; i++)
                {
                    var itemStartTime = DateTime.Now;
                    Logger.Debug($"开始打印第 {i + 1}/{printnum} 份");

                    try
                    {
                        // 设置打印数据
                        Logger.Debug("开始设置打印数据...");
                        format.SubStrings["品名"].Value = $"品名：{productName}";
                        format.SubStrings["规格"].Value = $"规格：{spec}";
                        format.SubStrings["斤数"].Value = $"{weight} ";
                        format.SubStrings["生产日期"].Value = $"{date}";
                        format.SubStrings["二维码"].Value = qrCode;
                        Logger.Debug("打印数据设置完成");

                        // 执行打印
                        Logger.Debug("开始执行打印...");
                        Result rel = format.Print(); // 获取打印状态

                        if (rel == Result.Success)
                        {
                            var itemDuration = (DateTime.Now - itemStartTime).TotalMilliseconds;
                            Logger.Info($"第 {i + 1}/{printnum} 份打印成功，耗时: {itemDuration:F0}ms");
                        }
                        else
                        {
                            Logger.Error($"第 {i + 1}/{printnum} 份打印失败，返回状态: {rel}");
                            OnLogMessage($"第 {i + 1}/{printnum} 份打印失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"第 {i + 1}/{printnum} 份打印异常: {ex.Message}");
                        OnLogMessage($"第 {i + 1}/{printnum} 份打印异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"打印过程中发生异常: {ex.Message}");
                OnLogMessage($"打印过程中发生异常: {ex.Message}");
            }
            finally
            {
                btn_print.Enabled = true;
                Logger.Debug("启用打印按钮");

                var totalDuration = (DateTime.Now - startTime).TotalMilliseconds;
                Logger.Info($"打印任务完成，总耗时: {totalDuration:F0}ms，打印数量: {printnum}");
                OnLogMessage($"打印任务完成，共 {printnum} 份");
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
            TemplateManagerForm templateManagerForm = new TemplateManagerForm();
            templateManagerForm.ShowDialog();  // 打开模板管理窗口
        }

        private void btnProductRules_Click(object sender, EventArgs e)
        {
            ProductRuleForm form = new ProductRuleForm();
            form.ShowDialog();

            // 重新加载产品规则，但不重置规则的允许打印状态
            _productRuleManager = new ProductRuleManager(resetAllowPrintStatus: false);
            OnLogMessage("已重新加载产品规则，保留了允许打印的设置");
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
            // 检查是否有该品类的规则
            var allRules = _productRuleManager.GetAllRules();
            var categoryRules = allRules.Where(r => r.Version == category).ToList();

            if (categoryRules.Count == 0)
            {
                return $"系统中没有品类为 {category} 的规则，请先添加相关规则";
            }

            // 检查鸡舍号匹配情况
            if (!string.IsNullOrEmpty(chickenHouse))
            {
                var chickenHouseRules = categoryRules.Where(r => r.ChickenHouse == chickenHouse).ToList();
                var nullChickenHouseRules = categoryRules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();

                if (chickenHouseRules.Count == 0 && nullChickenHouseRules.Count == 0)
                {
                    var availableChickenHouses = categoryRules
                        .Where(r => !string.IsNullOrEmpty(r.ChickenHouse))
                        .Select(r => r.ChickenHouse)
                        .Distinct()
                        .ToList();

                    if (availableChickenHouses.Any())
                    {
                        return $"没有鸡舍号为 {chickenHouse} 的规则，当前品类有以下鸡舍号的规则: {string.Join(", ", availableChickenHouses)}";
                    }
                    else
                    {
                        return $"没有鸡舍号为 {chickenHouse} 的规则，也没有通用鸡舍号规则";
                    }
                }

                // 如果有鸡舍号匹配的规则，继续检查客户名
                var matchingRules = chickenHouseRules.Any() ? chickenHouseRules : nullChickenHouseRules;

                // 检查客户名匹配情况
                if (!string.IsNullOrEmpty(customerName))
                {
                    string trimmedCustomerName = customerName.Trim();
                    var customerRules = matchingRules.Where(r =>
                        !string.IsNullOrEmpty(r.CustomerName) &&
                        r.CustomerName.Trim() == trimmedCustomerName
                    ).ToList();

                    var nullCustomerRules = matchingRules.Where(r => string.IsNullOrEmpty(r.CustomerName)).ToList();

                    if (customerRules.Count == 0 && nullCustomerRules.Count == 0)
                    {
                        var availableCustomerNames = matchingRules
                            .Where(r => !string.IsNullOrEmpty(r.CustomerName))
                            .Select(r => r.CustomerName.Trim())
                            .Distinct()
                            .ToList();

                        if (availableCustomerNames.Any())
                        {
                            return $"没有客户名为 {trimmedCustomerName} 的规则，当前条件下有以下客户名的规则: {string.Join(", ", availableCustomerNames)}";
                        }
                        else
                        {
                            return $"没有客户名为 {trimmedCustomerName} 的规则，也没有通用客户名规则";
                        }
                    }

                    // 如果有客户名匹配的规则，继续检查重量
                    matchingRules = customerRules.Any() ? customerRules : nullCustomerRules;
                }
                else
                {
                    // 如果没有提供客户名，只保留那些没有指定客户名的规则
                    var nullCustomerRules = matchingRules.Where(r => string.IsNullOrEmpty(r.CustomerName)).ToList();

                    if (nullCustomerRules.Count == 0)
                    {
                        var availableCustomerNames = matchingRules
                            .Where(r => !string.IsNullOrEmpty(r.CustomerName))
                            .Select(r => r.CustomerName.Trim())
                            .Distinct()
                            .ToList();

                        if (availableCustomerNames.Any())
                        {
                            return $"没有通用客户名规则，当前条件下需要指定以下客户名之一: {string.Join(", ", availableCustomerNames)}";
                        }
                        else
                        {
                            return "没有通用客户名规则，所有规则都需要指定客户名";
                        }
                    }

                    matchingRules = nullCustomerRules;
                }

                // 检查重量范围
                var weightRules = matchingRules.Where(r =>
                    r.WeightLowerLimit <= weight && weight <= r.WeightUpperLimit
                ).ToList();

                if (weightRules.Count == 0)
                {
                    var availableWeightRanges = matchingRules
                        .Select(r => $"[{r.WeightLowerLimit}-{r.WeightUpperLimit}]")
                        .Distinct()
                        .ToList();

                    if (availableWeightRanges.Any())
                    {
                        return $"重量 {weight} 不在任何规则的范围内，当前条件下有以下重量范围的规则: {string.Join(", ", availableWeightRanges)}";
                    }
                    else
                    {
                        return $"没有找到适用的重量范围规则";
                    }
                }
            }
            else
            {
                // 如果没有提供鸡舍号，只保留那些没有指定鸡舍号的规则
                var nullChickenHouseRules = categoryRules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();

                if (nullChickenHouseRules.Count == 0)
                {
                    var availableChickenHouses = categoryRules
                        .Where(r => !string.IsNullOrEmpty(r.ChickenHouse))
                        .Select(r => r.ChickenHouse)
                        .Distinct()
                        .ToList();

                    if (availableChickenHouses.Any())
                    {
                        return $"没有通用鸡舍号规则，需要指定以下鸡舍号之一: {string.Join(", ", availableChickenHouses)}";
                    }
                    else
                    {
                        return "没有通用鸡舍号规则，所有规则都需要指定鸡舍号";
                    }
                }

                // 后续逻辑与上面类似，但为了简化，这里不再重复
            }

            // 如果没有找到具体原因，返回一个通用消息
            return "没有找到匹配的规则，请检查品类、鸡舍号、客户名和重量是否符合已配置的规则";
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