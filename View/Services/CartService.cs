
using System;
using System.Collections.Generic;
using System.Linq;

namespace View.Services
{
    public class CartProduct
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public byte[]? ProductImageBlob { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryID { get; set; }
    }

    public class CartItem
    {
        public CartProduct Product { get; set; } = new CartProduct();
        public int Quantity { get; set; }
    }

    public class ShoppingCartService
    {
        private readonly List<CartItem> items = new List<CartItem>();

        public event Action? OnCartChanged;

        public List<CartItem> GetItems()
        {
            return items;
        }

        public void AddProduct(CartProduct product)
        {
            CartItem? existingItem = items.FirstOrDefault(
                item => item.Product.ProductID == product.ProductID);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                items.Add(new CartItem
                {
                    Product = product,
                    Quantity = 1
                });
            }

            OnCartChanged?.Invoke();
        }

        public void RemoveProduct(int productID)
        {
            CartItem? item = items.FirstOrDefault(
                item => item.Product.ProductID == productID);

            if (item != null)
            {
                items.Remove(item);
                OnCartChanged?.Invoke();
            }
        }

        public void IncreaseQuantity(int productID)
        {
            CartItem? item = items.FirstOrDefault(
                item => item.Product.ProductID == productID);

            if (item != null)
            {
                item.Quantity++;
                OnCartChanged?.Invoke();
            }
        }

        public void DecreaseQuantity(int productID)
        {
            CartItem? item = items.FirstOrDefault(
                item => item.Product.ProductID == productID);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    items.Remove(item);
                }

                OnCartChanged?.Invoke();
            }
        }

        public int TotalQuantity()
        {
            return items.Sum(item => item.Quantity);
        }

        public decimal TotalPrice()
        {
            return items.Sum(item => item.Product.Price * item.Quantity);
        }

        public void Clear()
        {
            items.Clear();
            OnCartChanged?.Invoke();
        }
    }
}
