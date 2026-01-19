// Models/Medicine.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace _10PercentWebProject.Models
{
    public class Medicine
    {
        public int MedicineId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? StockStatus { get; set; } // "In Stock", "Out of Stock", "Low Stock"
        public bool IsFeatured { get; set; }
        public bool IsOnSale { get; set; }
        public string? BadgeType { get; set; } // "sale", "new", "featured", "out-of-stock"
        public bool IsActive { get; set; } = true;

        // Simple properties only (no complex calculations)
        public int Quantity { get; set; }
        public string? BrandName { get; set; }
    }
}