namespace Model
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public byte[]? ProductImageBlob { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryID { get; set; }
    }
}