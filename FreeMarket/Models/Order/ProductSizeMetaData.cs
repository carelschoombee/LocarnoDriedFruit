using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FreeMarket.Models
{
    [MetadataType(typeof(ProductSizeMetaData))]
    public partial class ProductSize
    {
        public decimal PricePerUnit { get; set; }
        public decimal SpecialPricePerUnit { get; set; }
        public bool Activated { get; set; }

        public static List<ProductSize> GetNewProductSizes()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                List<ProductSize> productSizes = db.ProductSizes.ToList();

                foreach (ProductSize item in productSizes)
                {
                    item.Activated = false;
                    item.PricePerUnit = 0;
                }

                return productSizes;
            }
        }

        public static List<ProductSize> GetExistingProductSizes(int productNumber, int supplierNumber)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                List<ProductSize> prices = GetNewProductSizes();

                List<ProductSupplier> productSizes = db.ProductSuppliers
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber)
                    .ToList();

                foreach (ProductSupplier item in productSizes)
                {
                    prices.Where(c => c.SizeId == item.SizeType).FirstOrDefault().PricePerUnit = item.PricePerUnit;
                    prices.Where(c => c.SizeId == item.SizeType).FirstOrDefault().SpecialPricePerUnit = item.SpecialPricePerUnit ?? 0.00M;
                    prices.Where(c => c.SizeId == item.SizeType).FirstOrDefault().Activated = true;
                }

                return prices;
            }
        }
    }

    public class ProductSizeMetaData
    {
        [DisplayName("ID")]
        public int SizeId { get; set; }

        [StringLength(150)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Dimensions { get; set; }

        [DisplayName("Weight (KG)")]
        public decimal Weight { get; set; }
    }
}