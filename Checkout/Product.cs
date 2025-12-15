using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkout
{
    public class Product
    {
        public int Id { get; set; }                // ⬅ Behövs för databas
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }

        public Product() { }                        // ⬅ Behövs för databas-laddning

        public Product(string name, decimal price, string category)
        {
            Name = name;
            Price = price;
            Category = category;
        }

        public override string ToString()
        {
            return $"{Name} - {Price} kr";
        }

        public class CartItem
        {
            public Product Product { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => Product.Price * Quantity;
        }
    }
}
