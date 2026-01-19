namespace _10PercentWebProject.Models
{
    public class Cart
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public int TotalItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal PrescriptionFee { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
