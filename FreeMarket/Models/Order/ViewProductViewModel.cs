using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Models
{
    public class ViewProductViewModel
    {
        [DisplayName("Quantity")]
        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        public bool? CannotDeliver { get; set; }
        public int ProductNumber { get; set; }
        public int SupplierNumber { get; set; }
        public Product ProductInstance { get; set; }
        public Supplier SupplierInstance { get; set; }
        public int ReviewPageSize { get; set; }
        public int CustodianQuantityOnHand { get; set; }
        public int CustodianNumber { get; set; }
        public ProductReviewsCollection Reviews { get; set; }
        public int SelectedPackageType { get; set; }
        public List<SelectListItem> ProductSizes { get; set; }

        public void InitializeDefault()
        {
            Quantity = 0;
            ProductNumber = 0;
            SupplierNumber = 0;
        }

        public ViewProductViewModel()
        {
            InitializeDefault();
        }

        public ViewProductViewModel(int productNumber, int supplierNumber, int quantityRequested, int orderNumber = 0)
        {
            // Validate
            if (productNumber == 0 || supplierNumber == 0 || quantityRequested < 1)
                InitializeDefault();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal minValue = db.ProductSuppliers
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber)
                    .Min(c => c.PricePerUnit);

                // Validate
                ProductSupplier productSupplierTemp = db.ProductSuppliers
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber && c.PricePerUnit == minValue)
                    .FirstOrDefault();

                if (productSupplierTemp == null)
                {
                    InitializeDefault();
                    return;
                }

                Quantity = quantityRequested;
                ProductNumber = productNumber;
                SupplierNumber = supplierNumber;

                ProductCustodian custodian = ShoppingCart.GetStockAvailable(productNumber, supplierNumber, quantityRequested, productSupplierTemp.SizeType);
                if (custodian != null)
                {
                    CannotDeliver = false;
                    CustodianQuantityOnHand = custodian.QuantityOnHand;
                    CustodianNumber = custodian.CustodianNumber;
                }
                else
                {
                    CannotDeliver = true;
                    CustodianQuantityOnHand = 0;
                    CustodianNumber = 0;
                }

                SetInstances(productNumber, supplierNumber);
                ReviewPageSize = 4;

                ProductSizes = db.GetProductSizes(productNumber, supplierNumber)
                    .Select(c => new SelectListItem
                    {
                        Text = c.Description,
                        Value = c.SizeId.ToString(),
                        Selected = productSupplierTemp.SizeType == c.SizeId ? true : false
                    })
                    .ToList();
            }
        }

        public void SetInstances(int productNumber, int supplierNumber)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                Product product = Product.GetProduct(productNumber, supplierNumber);
                Supplier supplier = db.Suppliers.Find(supplierNumber);

                if (product == null || supplier == null)
                    return;

                ProductInstance = product;
                SupplierInstance = supplier;
            }
        }
    }
}