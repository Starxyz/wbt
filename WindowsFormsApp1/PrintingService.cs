using Seagull.BarTender.Print;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // For loading templates.json

namespace WindowsFormsApp1
{
    public class PrintingService : IPrintingService, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Engine _engine = null;
        private LabelFormatDocument _format = null;
        private string _selectedFilePath = null;
        private bool _templateOpened = false;
        private List<PrintTemplate> _templates = new List<PrintTemplate>();

        public event Action<string> LogMessageRequested;
        public event Action<string> PrintSuccessOccurred;
        public event Action<string> PrintFailureOccurred;

        public PrintingService()
        {
            try
            {
                LogMessageRequested?.Invoke("Initializing BarTender Engine...");
                // Starting the engine can sometimes fail if BarTender is not installed correctly.
                _engine = new Engine(true); // Start engine immediately
                LogMessageRequested?.Invoke("BarTender Engine initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize BarTender Engine.");
                LogMessageRequested?.Invoke($"FATAL: Failed to initialize BarTender Engine: {ex.Message}");
                // Optional: rethrow or set a flag indicating critical failure
                throw; // Critical failure, service cannot operate
            }
            
            // Default path, can be made configurable if needed
            LoadPrintTemplatesFromJson(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates.json"));
        }

        public async Task AutoLoadDefaultTemplateAsync(string defaultTemplatePath)
        {
            LogMessageRequested?.Invoke($"Attempting to auto-load default template: {defaultTemplatePath}");
            if (string.IsNullOrEmpty(defaultTemplatePath))
            {
                LogMessageRequested?.Invoke("Default template path is not configured or empty.");
                Logger.Warn("Default template path is null or empty in AutoLoadDefaultTemplateAsync.");
                return;
            }

            if (File.Exists(defaultTemplatePath))
            {
                LogMessageRequested?.Invoke($"Default template found. Loading: {defaultTemplatePath}");
                bool success = await LoadTemplateAsync(defaultTemplatePath);
                if (success)
                {
                    LogMessageRequested?.Invoke($"Default template '{defaultTemplatePath}' loaded successfully.");
                }
                else
                {
                    LogMessageRequested?.Invoke($"Failed to load default template '{defaultTemplatePath}'.");
                }
            }
            else
            {
                Logger.Warn($"Default template file not found: {defaultTemplatePath}");
                LogMessageRequested?.Invoke($"Default template file not found: {defaultTemplatePath}");
            }
        }

        public async Task<bool> LoadTemplateAsync(string filePath)
        {
            LogMessageRequested?.Invoke($"Loading template from: {filePath}");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogMessageRequested?.Invoke("File path is null or empty.");
                Logger.Warn("LoadTemplateAsync called with null or empty file path.");
                return false;
            }

            CloseCurrentTemplate(); // Close any previously opened template

            try
            {
                if (_engine == null || !_engine.IsAlive)
                {
                    LogMessageRequested?.Invoke("BarTender engine is not running or null. Attempting to start...");
                    _engine = new Engine(true); // Ensure engine is started
                    LogMessageRequested?.Invoke("BarTender engine (re)started.");
                }

                // Load the template in a background thread
                await Task.Run(() =>
                {
                    _format = _engine.Documents.Open(filePath);
                });

                _selectedFilePath = filePath;
                _templateOpened = true;
                LogMessageRequested?.Invoke($"Template '{filePath}' loaded successfully.");
                Logger.Info($"Template loaded: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to load template file: {filePath}");
                LogMessageRequested?.Invoke($"Error loading template '{filePath}': {ex.Message}");
                _templateOpened = false;
                _format = null;
                _selectedFilePath = null;
                return false;
            }
        }

        public void CloseCurrentTemplate()
        {
            if (_format != null)
            {
                try
                {
                    _format.Close(SaveOptions.DoNotSaveChanges);
                    LogMessageRequested?.Invoke($"Closed template: {_selectedFilePath}");
                    Logger.Info($"Closed template: {_selectedFilePath}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error closing template: {_selectedFilePath}");
                    LogMessageRequested?.Invoke($"Error closing template '{_selectedFilePath}': {ex.Message}");
                }
                finally
                {
                    _format = null;
                    _templateOpened = false;
                    _selectedFilePath = null;
                }
            }
            // Note: Engine is kept running until service disposal.
        }

        public async Task<bool> ExecutePrintJobAsync(PrintJobData jobData)
        {
            if (jobData == null)
            {
                LogMessageRequested?.Invoke("Print job data is null.");
                Logger.Warn("ExecutePrintJobAsync called with null jobData.");
                PrintFailureOccurred?.Invoke("Print job data was not provided.");
                return false;
            }

            if (!_templateOpened || _format == null)
            {
                LogMessageRequested?.Invoke("No template is currently loaded. Cannot print.");
                Logger.Error("ExecutePrintJobAsync: No template loaded.");
                PrintFailureOccurred?.Invoke("Printing failed: No template is loaded.");
                return false;
            }
            
            if (_engine == null || !_engine.IsAlive)
            {
                 LogMessageRequested?.Invoke("BarTender engine is not available. Cannot print.");
                 Logger.Error("ExecutePrintJobAsync: BarTender engine not available.");
                 PrintFailureOccurred?.Invoke("Printing failed: BarTender engine not available.");
                 return false;
            }

            LogMessageRequested?.Invoke($"Starting print job: {jobData.ProductName}, Qty: {jobData.Quantity}");
            Logger.Info($"Executing print job: Name='{jobData.ProductName}', Spec='{jobData.Specification}', Weight='{jobData.Weight}', Date='{jobData.DateCode}', QR='{jobData.QrCode}', Qty='{jobData.Quantity}' using template '{_selectedFilePath}'");

            try
            {
                bool allSucceeded = true;
                for (int i = 0; i < jobData.Quantity; i++)
                {
                    var itemStartTime = DateTime.Now;
                    Logger.Debug($"Printing item {i + 1} of {jobData.Quantity} for {jobData.ProductName}");

                    _format.SubStrings["品名"].Value = $"品名：{jobData.ProductName}";
                    _format.SubStrings["规格"].Value = $"规格：{jobData.Specification}";
                    _format.SubStrings["斤数"].Value = $"{jobData.Weight} "; // Assuming space is intentional
                    _format.SubStrings["生产日期"].Value = $"{jobData.DateCode}";
                    _format.SubStrings["二维码"].Value = jobData.QrCode;
                    
                    // Execute print in a background thread
                    Result result = await Task.Run(() => _format.Print());

                    var itemDuration = (DateTime.Now - itemStartTime).TotalMilliseconds;
                    if (result == Result.Success)
                    {
                        Logger.Info($"Item {i + 1}/{jobData.Quantity} printed successfully for {jobData.ProductName}. Duration: {itemDuration:F0}ms");
                        PrintSuccessOccurred?.Invoke($"Successfully printed item {i+1} for '{jobData.ProductName}'.");
                    }
                    else
                    {
                        allSucceeded = false;
                        Logger.Error($"Failed to print item {i + 1}/{jobData.Quantity} for {jobData.ProductName}. Result: {result}. Duration: {itemDuration:F0}ms");
                        PrintFailureOccurred?.Invoke($"Printing failed for item {i+1} of '{jobData.ProductName}': {result}");
                        // Optionally break or collect all errors
                    }
                }
                return allSucceeded;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Exception during print job for {jobData.ProductName} with template {_selectedFilePath}.");
                LogMessageRequested?.Invoke($"Error during print job for '{jobData.ProductName}': {ex.Message}");
                PrintFailureOccurred?.Invoke($"Printing error for '{jobData.ProductName}': {ex.Message}");
                return false;
            }
        }

        public void LoadPrintTemplatesFromJson(string jsonFilePath)
        {
            try
            {
                if (!File.Exists(jsonFilePath))
                {
                    _templates = new List<PrintTemplate>(); // Ensure it's an empty list
                    Logger.Warn($"Templates JSON file not found: {jsonFilePath}");
                    LogMessageRequested?.Invoke($"Warning: Templates JSON file not found at '{jsonFilePath}'. Simple template functionality will be limited.");
                    return;
                }
                string json = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                _templates = JsonConvert.DeserializeObject<List<PrintTemplate>>(json) ?? new List<PrintTemplate>();
                LogMessageRequested?.Invoke($"Loaded {_templates.Count} print templates from '{jsonFilePath}'.");
                Logger.Info($"Loaded {_templates.Count} print templates from '{jsonFilePath}'.");
            }
            catch (Exception ex)
            {
                _templates = new List<PrintTemplate>(); // Ensure it's an empty list on error
                Logger.Error(ex, $"Failed to load templates from JSON file: {jsonFilePath}");
                LogMessageRequested?.Invoke($"Error loading templates from '{jsonFilePath}': {ex.Message}");
            }
        }

        public PrintTemplate FindTemplateByKey(string key)
        {
            var template = _templates.FirstOrDefault(t => t.key == key);
            if (template == null)
            {
                Logger.Warn($"Print template with key '{key}' not found.");
                // LogMessageRequested?.Invoke($"Template with key '{key}' not found."); // Can be noisy
            }
            return template;
        }

        public List<PrintTemplate> GetLoadedPrintTemplates()
        {
            return _templates;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogMessageRequested?.Invoke("Disposing PrintingService...");
                Logger.Info("Disposing PrintingService.");

                CloseCurrentTemplate();

                if (_engine != null)
                {
                    try
                    {
                        if (_engine.IsAlive) // Check if it's alive before trying to stop
                        {
                           LogMessageRequested?.Invoke("Stopping BarTender engine...");
                           _engine.Stop();
                           LogMessageRequested?.Invoke("BarTender engine stopped.");
                        }
                        _engine.Dispose();
                        LogMessageRequested?.Invoke("BarTender engine disposed.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Exception during BarTender engine Stop/Dispose.");
                        LogMessageRequested?.Invoke($"Error disposing BarTender engine: {ex.Message}");
                    }
                    finally
                    {
                        _engine = null;
                    }
                }
                LogMessageRequested?.Invoke("PrintingService disposed.");
            }
        }
    }
}
