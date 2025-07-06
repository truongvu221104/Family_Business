using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Family_Business.Models
{
    public partial class ProductCategory
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = null!;

        // Quan hệ 1–nhiều với Product
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
