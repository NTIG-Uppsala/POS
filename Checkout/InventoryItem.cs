using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkout
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int Inventory { get; set; }
        public int Sold { get; set; }
    }
}
