using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FreeMarket.Models
{
    [MetadataType(typeof(ProductCustodianMetaData))]
    public partial class ProductCustodian
    {
        public string CustodianName { get; set; }
        public string SupplierName { get; set; }
        public string ProductName { get; set; }

        [DisplayName("Amount of stock to be added / removed:")]
        public int QuantityChange { get; set; }

        public bool InStock { get; set; }
        public int MainImageNumber { get; set; }

        public static List<ProductCustodian> GetAllProductCustodians()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                List<ProductCustodian> custodians = new List<ProductCustodian>();
                List<GetAllProductCustodians_Result> result = db.GetAllProductCustodians().ToList();

                foreach (GetAllProductCustodians_Result item in result)
                {
                    ProductCustodian productCustodianDB = db.ProductCustodians
                        .FirstOrDefault(c => c.ProductNumber == item.ProductNumber && c.SupplierNumber == item.SupplierNumber);

                    if (productCustodianDB == null)
                        return new List<ProductCustodian>();

                    bool inStock = productCustodianDB.QuantityOnHand > 0 ? true : false;

                    Product product = db.Products.Find(item.ProductNumber);
                    product.GetProductImages(product);

                    Supplier supplier = db.Suppliers.Find(item.SupplierNumber);

                    custodians.Add(new ProductCustodian
                    {
                        ProductName = product.Description,
                        ProductNumber = item.ProductNumber,
                        SupplierNumber = item.SupplierNumber,
                        MainImageNumber = product.MainImageNumber,
                        SupplierName = product.SupplierName,
                        InStock = inStock
                    });
                }

                return custodians;
            }
        }

        public static void AddStock(int productNumber, int supplierNumber, int custodianNumber, int quantity)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                ProductCustodian custodian = db.ProductCustodians.Where(c => c.CustodianNumber == custodianNumber &&
                                                c.ProductNumber == productNumber &&
                                                c.SupplierNumber == supplierNumber)
                                                .FirstOrDefault();

                custodian.QuantityOnHand += quantity;
                custodian.DateLastIncreasedBySupplier = DateTime.Now;
                custodian.AmountLastIncreasedBySupplier = quantity;

                db.Entry(custodian).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
        }

        public static void RemoveStock(int productNumber, int supplierNumber, int custodianNumber, int quantity)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                ProductCustodian custodian = db.ProductCustodians.Where(c => c.CustodianNumber == custodianNumber &&
                                                c.ProductNumber == productNumber &&
                                                c.SupplierNumber == supplierNumber)
                                                .FirstOrDefault();

                custodian.QuantityOnHand -= quantity;
                custodian.DateLastIncreasedBySupplier = DateTime.Now;
                custodian.AmountLastIncreasedBySupplier = quantity;

                if (custodian.QuantityOnHand < 0)
                    custodian.QuantityOnHand = 0;

                db.Entry(custodian).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
        }
    }

    public class ProductCustodianMetaData
    {
        [DisplayName("Custodian Name")]
        public string CustodianName { get; set; }

        [DisplayName("Product Name")]
        public string ProductName { get; set; }

        [DisplayName("Supplier Name")]
        public string SupplierName { get; set; }

        [DisplayName("Quantity On Hand")]
        public int QuantityOnHand { get; set; }

        [DisplayName("Stock Reserved")]
        public int StockReservedForOrders { get; set; }

        [DisplayName("Last Modified")]
        public DateTime DateLastIncreasedBySupplier { get; set; }

        [DisplayName("Last Modified Amount")]
        public int AmountLastIncreasedBySupplier { get; set; }
    }
}