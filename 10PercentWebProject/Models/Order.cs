namespace _10PercentWebProject.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // "Pending", "Processing", "Shipped", "Delivered"
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
