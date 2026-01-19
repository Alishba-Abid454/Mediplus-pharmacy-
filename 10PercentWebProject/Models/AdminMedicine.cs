namespace _10PercentWebProject.Models
{
    public class AdminMedicine : Medicine
    {
        // Extra fields for admin management
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Supplier { get; set; }
        public string? BatchNumber { get; set; }
        public int MinStockLevel { get; set; } = 10;
        public string? BrandName { get; set; }

        public string Status { get; set; } = "Active";
    }
}