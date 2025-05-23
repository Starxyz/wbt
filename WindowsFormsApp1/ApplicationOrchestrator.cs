using System;
using System.Globalization;
using System.Threading.Tasks;
using NLog;

namespace WindowsFormsApp1
{
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        private readonly IModbusService _modbusService;
        private readonly ProductRuleManager _productRuleManager;
        private readonly IPrintingService _printingService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public event Action<string> LogMessageRequested;
        public event Action<string> DetailedMatchFailureLogRequested;
        public event Action<string> WeightAvailable;

        public ApplicationOrchestrator(
            IModbusService modbusService,
            ProductRuleManager productRuleManager,
            IPrintingService printingService)
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
            _productRuleManager = productRuleManager ?? throw new ArgumentNullException(nameof(productRuleManager));
            _printingService = printingService ?? throw new ArgumentNullException(nameof(printingService));
        }

        public async Task ProcessIncomingMessageAsync(string rawMessage)
        {
            Logger.Debug($"Orchestrator: Starting to process raw message: {rawMessage}");
            LogMessageRequested?.Invoke($"Orchestrator: Received raw message: {rawMessage}");

            try
            {
                if (string.IsNullOrWhiteSpace(rawMessage))
                {
                    Logger.Warn("Orchestrator: Received empty or whitespace message, ignoring.");
                    LogMessageRequested?.Invoke("Orchestrator: Received empty message, ignoring.");
                    return;
                }

                // Assuming CONNECT and DISCONNECT messages are handled by Form1 or TcpService directly
                // and not passed to the orchestrator. If they are, add handling here or ensure Form1 filters them.

                var parts = rawMessage.Split(new[] { '|' }, StringSplitOptions.None);
                string category = parts.Length > 0 ? parts[0] : null;
                string chickenHouse = (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) && !parts[1].Equals("null", StringComparison.OrdinalIgnoreCase)) ? parts[1] : null;
                string panelStatus = (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) && !parts[2].Equals("null", StringComparison.OrdinalIgnoreCase)) ? parts[2] : null;
                string customerName = null;
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && !parts[3].Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    customerName = parts[3].Length <= 20 ? parts[3] : null;
                    if (customerName == null) Logger.Warn($"Orchestrator: Customer name '{parts[3]}' too long, ignored.");
                }

                LogMessageRequested?.Invoke($"Orchestrator: Parsed message - Category='{category}', ChickenHouse='{chickenHouse}', PanelStatus='{panelStatus}', Customer='{customerName}'");

                if (string.IsNullOrWhiteSpace(category))
                {
                    Logger.Warn("Orchestrator: Message missing category, cannot process.");
                    LogMessageRequested?.Invoke("Orchestrator: Message missing category, print rejected.");
                    return;
                }

                if ("0".Equals(panelStatus, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Orchestrator: Panel status is 0, print not required.");
                    LogMessageRequested?.Invoke("Orchestrator: Panel status is 0, print not required.");
                    return;
                }

                LogMessageRequested?.Invoke("Orchestrator: Fetching weight...");
                string weightStr = (await _modbusService.GetWeightAsync()).Trim();
                WeightAvailable?.Invoke(weightStr); // Invoke event with the retrieved weight string
                // ModbusService events should handle logging of raw weight, this logs the outcome
                LogMessageRequested?.Invoke($"Orchestrator: Weight received: {weightStr}");


                if (weightStr == "--.-" || !double.TryParse(weightStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                {
                    string reason = $"Orchestrator: Invalid weight value '{weightStr}'. Print rejected.";
                    Logger.Warn(reason);
                    LogMessageRequested?.Invoke(reason);
                    return;
                }

                Logger.Info($"Orchestrator: Attempting to find matching rule for Category='{category}', ChickenHouse='{chickenHouse}', Customer='{customerName}', Weight={weight}");
                var matchResult = _productRuleManager.FindMatchingRule(category, chickenHouse, customerName, weight);

                if (matchResult.IsSuccess)
                {
                    var matchedRule = matchResult.MatchedRule;
                    Logger.Info($"Orchestrator: Rule ID '{matchedRule.Id}' matched. Product='{matchedRule.ProductName}'.");
                    LogMessageRequested?.Invoke($"Orchestrator: Rule ID '{matchedRule.Id}' matched. Product='{matchedRule.ProductName}'. Preparing to print.");

                    string traceabilityCode = TraceabilityCodeGenerator.GenerateTraceabilityCode();
                    Logger.Info($"Orchestrator: Generated traceability code: {traceabilityCode}");

                    PrintJobData jobData = new PrintJobData
                    {
                        ProductName = matchedRule.ProductName,
                        Specification = matchedRule.Specification,
                        Weight = weightStr,
                        DateCode = traceabilityCode,
                        QrCode = matchedRule.QRCode,
                        Quantity = 1
                    };
                    
                    await _printingService.ExecutePrintJobAsync(jobData);
                    // Success/failure is logged by PrintingService events.
                    LogMessageRequested?.Invoke($"Orchestrator: Print job for rule '{matchedRule.Id}' submitted.");
                }
                else
                {
                    string generalRejectReason = $"Category='{category}', ChickenHouse='{chickenHouse ?? "N/A"}', Customer='{customerName ?? "N/A"}', Weight={weight}";
                    Logger.Info($"Orchestrator: No rule matched for: {generalRejectReason}. Failure reason: {matchResult.FailureReason}");
                    LogMessageRequested?.Invoke($"【匹配失败】不执行打印，原因: {generalRejectReason}");
                    LogMessageRequested?.Invoke($"接收到的（消息：{rawMessage}，未匹配到规则）");


                    if (!string.IsNullOrEmpty(matchResult.FailureReason))
                    {
                        DetailedMatchFailureLogRequested?.Invoke($"【详细原因】{matchResult.FailureReason}");
                    }
                    
                    LogMessageRequested?.Invoke("Orchestrator: No rule matched. Attempting legacy processing.");
                    await ProcessMessageWithLegacyMethodAsync(category, chickenHouse, panelStatus, weightStr, rawMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Orchestrator: Error processing message '{rawMessage}'.");
                LogMessageRequested?.Invoke($"Orchestrator: Critical error processing message: {ex.Message}");
            }
            finally
            {
                Logger.Debug($"Orchestrator: Finished processing raw message: {rawMessage}");
            }
        }

        private async Task ProcessMessageWithLegacyMethodAsync(string key, string slot, string status, string weightStr, string originalRawMessage)
        {
            Logger.Debug($"Orchestrator (Legacy): Processing Category='{key}', Slot='{slot}', Status='{status}', Weight='{weightStr}'");
            LogMessageRequested?.Invoke($"Orchestrator (Legacy): Attempting fallback for {key}.");
            try
            {
                // Validations (already performed for category and panelStatus in main method, re-check if direct call possible)
                bool hasSlot = !string.IsNullOrWhiteSpace(slot);
                if (key.Equals("xmyjpxjd360", StringComparison.OrdinalIgnoreCase) && !hasSlot)
                {
                    string reason = "Legacy: Category 'xmyjpxjd360' requires a slot. Print rejected.";
                    Logger.Info(reason);
                    LogMessageRequested?.Invoke(reason);
                    return;
                }

                string templateKey = hasSlot ? $"{key}-{slot}" : key;
                string qrOverride = null;

                if (key.Equals("xmyjpxjd360", StringComparison.OrdinalIgnoreCase))
                {
                    if (!double.TryParse(weightStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double w))
                    {
                        string reason = $"Legacy: Invalid weight '{weightStr}' for 'xmyjpxjd360'. Print rejected.";
                        Logger.Warn(reason);
                        LogMessageRequested?.Invoke(reason);
                        return;
                    }
                    // Determine QR override based on weight for 'xmyjpxjd360'
                    if (w >= 20.5 && w <= 21.1) qrOverride = "1790";
                    else if (w >= 22.0 && w <= 22.4) qrOverride = "1791";
                    else if (w >= 23.9 && w <= 24.1) qrOverride = "1792";
                    else if (w >= 15.8 && w <= 16.6) qrOverride = "1793";
                    else
                    {
                        string reason = $"Legacy: Weight {w} for 'xmyjpxjd360' is out of defined ranges. Print rejected.";
                        Logger.Info(reason);
                        LogMessageRequested?.Invoke(reason);
                        return;
                    }
                    Logger.Info($"Legacy: QR override for 'xmyjpxjd360' with weight {w} is '{qrOverride}'.");
                }

                var tpl = _printingService.FindTemplateByKey(templateKey);
                if (tpl != null)
                {
                    string traceabilityCode = TraceabilityCodeGenerator.GenerateTraceabilityCode();
                    string qrCodeToUse = qrOverride ?? tpl.qrcode;

                    PrintJobData jobData = new PrintJobData
                    {
                        ProductName = tpl.productName,
                        Specification = tpl.spec,
                        Weight = weightStr,
                        DateCode = traceabilityCode,
                        QrCode = qrCodeToUse,
                        Quantity = 1
                    };
                    Logger.Info($"Legacy: Printing template '{templateKey}' for Product='{tpl.productName}', QR='{qrCodeToUse}'.");
                    await _printingService.ExecutePrintJobAsync(jobData);
                    LogMessageRequested?.Invoke($"Legacy: Print job for template key '{templateKey}' submitted.");
                }
                else
                {
                    string reason = $"Legacy: Template key '{templateKey}' not found. Print rejected.";
                    Logger.Warn(reason);
                    LogMessageRequested?.Invoke(reason);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Orchestrator (Legacy): Error processing for key '{key}'.");
                LogMessageRequested?.Invoke($"Orchestrator (Legacy): Critical error for '{key}': {ex.Message}");
            }
        }
    }
}
