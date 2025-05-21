namespace WindowsFormsApp1
{
    public class PrintJobData
    {
        public string ProductName { get; set; }
        public string Specification { get; set; }
        public string Weight { get; set; }
        public string DateCode { get; set; } // e.g., traceability code
        public string QrCode { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
