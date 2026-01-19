namespace _10PercentWebProject.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public int MedicineId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Icon { get; set; }
        public string? PrescriptionType { get; set; }
        public string? PackageInfo { get; set; }
        public decimal Total
        {
            get { return Price * Quantity; }
        }
    }
}
