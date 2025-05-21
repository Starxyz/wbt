using System.Threading.Tasks;
using System.Collections.Generic; // For List<PrintTemplate>

namespace WindowsFormsApp1
{
    public interface IPrintingService
    {
        event System.Action<string> LogMessageRequested;
        event System.Action<string> PrintSuccessOccurred; // For successful print
        event System.Action<string> PrintFailureOccurred; // For failed print

        Task AutoLoadDefaultTemplateAsync(string defaultTemplatePath);
        Task<bool> LoadTemplateAsync(string filePath); // Changed from LoadTemplateFromFileDialogAsync
        void CloseCurrentTemplate();
        Task<bool> ExecutePrintJobAsync(PrintJobData jobData);

        // Methods for managing simple templates (templates.json)
        List<PrintTemplate> GetLoadedPrintTemplates();
        PrintTemplate FindTemplateByKey(string key);
        void LoadPrintTemplatesFromJson(string jsonFilePath);

        void Dispose(); // For Seagull Engine
    }
}
