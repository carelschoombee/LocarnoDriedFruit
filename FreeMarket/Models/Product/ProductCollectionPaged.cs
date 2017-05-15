using FreeMarket.Infrastructure;
using PagedList;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Models
{
    public class ProductCollectionPaged
    {
        public PagedList<Product> Products { get; set; }
        public List<ExternalWebsite> Websites { get; set; }

        [RegularExpression(@"^[A-Za-z0-9\,\s]+$", ErrorMessage = "Alphabetic characters, Numbers, Spaces and commas only")]
        [StringLength(256)]
        [DisplayName("Search for products (for example 'Almond', 'Apricot', 'Fruit' or 'Raisin'.)")]
        public string ProductSearchCriteria { get; set; }

        [DisplayName("Browse by category")]
        public int SelectedDepartment { get; set; }
        public List<SelectListItem> Departments { get; set; }

        public ProductCollectionPaged()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                Departments = db.Departments
                        .Select(c => new SelectListItem
                        {
                            Text = c.DepartmentName,
                            Value = c.DepartmentNumber.ToString()
                        })
                        .ToList();

                Departments.Add(new SelectListItem { Text = "Text Search", Value = "9999" });
            }
        }

        public static ProductCollection GetAllProducts()
        {
            ProductCollection products = new ProductCollection();
            List<GetAllProductsDistinct_Result> result = new List<GetAllProductsDistinct_Result>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                result = db.GetAllProductsDistinct()
                    .ToList();

                products = SetAllProductDataDistinct(result);

                return products;
            }
        }

        public static ProductCollection GetMostPopularProducts()
        {
            ProductCollection products = new ProductCollection();
            List<GetAllProductsDistinct_Result> result = new List<GetAllProductsDistinct_Result>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                result = db.GetAllProductsDistinct()
                    .ToList();

                products = SetAllProductDataDistinct(result);
                products.Products = products.Products
                    .Where(c => c.NumberSold != 0)
                    .OrderByDescending(c => c.NumberSold)
                    .Take(12)
                    .ToList();

                return products;
            }
        }

        public static ProductCollectionPaged GetProductsFiltered(int pageNumber, int pageSize, string filter)
        {
            ProductCollectionPaged products = new ProductCollectionPaged();
            List<GetAllProductsDistinctFilter_Result> result = new List<GetAllProductsDistinctFilter_Result>();

            if (string.IsNullOrEmpty(filter))
                return products;

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                result = db.GetAllProductsDistinctFilter(filter)
                    .ToList();

                products = SetAllProductDataDistinctFilter(pageNumber, pageSize, result);

                return products;
            }
        }

        public static ProductCollectionPaged GetProductsByDepartment(int pageNumber, int pageSize, int departmentNumber)
        {
            ProductCollectionPaged departmentProducts = new ProductCollectionPaged();
            List<GetAllProductsByDepartmentDistinct_Result> result = new List<GetAllProductsByDepartmentDistinct_Result>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                result = db.GetAllProductsByDepartmentDistinct(departmentNumber)
                    .ToList();

                departmentProducts = SetProductDataDistinct(pageNumber, pageSize, result);

                return departmentProducts;
            }
        }

        public static ProductCollection GetAllProductsIncludingDeactivated()
        {
            ProductCollection departmentProducts = new ProductCollection();
            List<GetAllProductsIncludingDeactivatedDistinct_Result> result = new List<GetAllProductsIncludingDeactivatedDistinct_Result>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                result = db.GetAllProductsIncludingDeactivatedDistinct()
                    .ToList();

                departmentProducts = SetProductIncludingDeactivatedDataDistinct(result);

                return departmentProducts;
            }
        }

        public static ProductCollection GetProductsInOrder(int orderNumber)
        {
            ProductCollection allProductsTemp = new ProductCollection();
            ProductCollection allProducts = new ProductCollection();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                allProductsTemp.Products = db.GetAllProductsInOrder(orderNumber)
                    .Select(c => new Product
                    {
                        Activated = c.Activated,
                        DateAdded = c.DateAdded,
                        DateModified = c.DateModified,
                        DepartmentName = c.DepartmentName,
                        DepartmentNumber = c.DepartmentNumber,
                        Description = c.Description,
                        LongDescription = c.LongDescription,
                        PricePerUnit = c.PricePerUnit,
                        ProductNumber = c.ProductNumberID,
                        Size = "",
                        SupplierName = c.SupplierName,
                        SupplierNumber = c.SupplierNumberID,
                        Weight = 0,
                        ProductRating = c.ProductRating ?? 0,
                        ProductReviewText = c.ProductReviewText,
                        PriceRating = c.PriceRating ?? 0,
                        ReviewId = c.ReviewId,
                        PriceOrder = c.Price,
                        IsVirtual = c.IsVirtual
                    }
                    ).ToList();

                allProducts.Products = allProductsTemp.Products
                    .DistinctBy(c => new { c.ProductNumber, c.SupplierNumber })
                    .ToList();

                SetProductData(allProducts);

                return allProducts;
            }
        }

        public static void SetProductData(ProductCollection allProducts)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts.Products != null && allProducts.Products.Count > 0)
                {
                    foreach (Product product in allProducts.Products)
                    {
                        int imageNumber = db.ProductPictures
                            .Where(c => c.ProductNumber == product.ProductNumber && c.Dimensions == PictureSize.Medium.ToString())
                            .Select(c => c.PictureNumber)
                            .FirstOrDefault();

                        int imageNumberSecondary = db.ProductPictures
                            .Where(c => c.ProductNumber == product.ProductNumber && c.Dimensions == PictureSize.Small.ToString())
                            .Select(c => c.PictureNumber)
                            .FirstOrDefault();

                        product.MainImageNumber = imageNumber;
                        product.SecondaryImageNumber = imageNumberSecondary;
                    }
                }
            }
        }

        public static ProductCollection SetProductIncludingDeactivatedDataDistinct(List<GetAllProductsIncludingDeactivatedDistinct_Result> allProducts)
        {
            ProductCollection collection = new ProductCollection();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts != null && allProducts.Count > 0)
                {
                    foreach (GetAllProductsIncludingDeactivatedDistinct_Result product in allProducts)
                    {
                        Product prodTemp = Product.GetShallowProduct(product.ProductNumber, product.SupplierNumber);
                        prodTemp.MinPrice = product.minPrice;
                        prodTemp.MaxPrice = product.maxPrice;

                        if (prodTemp != null)
                            collection.Products.Add(prodTemp);
                    }
                }

                return collection;
            }
        }

        public static ProductCollectionPaged SetProductDataDistinct(int pageNumber, int pageSize, List<GetAllProductsByDepartmentDistinct_Result> allProducts)
        {
            ProductCollectionPaged collection = new ProductCollectionPaged();
            List<Product> temp = new List<Product>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts != null && allProducts.Count > 0)
                {
                    foreach (GetAllProductsByDepartmentDistinct_Result product in allProducts)
                    {
                        Product prodTemp = Product.GetShallowProduct(product.ProductNumber, product.SupplierNumber);
                        prodTemp.MinPrice = product.minPrice;
                        prodTemp.MaxPrice = product.maxPrice;

                        PopularProduct pp = db.PopularProducts.Where(c => c.ProductNumber == prodTemp.ProductNumber
                            && c.SupplierNumber == prodTemp.SupplierNumber).FirstOrDefault();

                        if (pp != null)
                            prodTemp.NumberSold = pp.NumberSold ?? 0;

                        if (prodTemp != null)
                            temp.Add(prodTemp);
                    }
                }

                collection.Products = (PagedList<Product>)temp.ToPagedList(pageNumber, pageSize);

                return collection;
            }
        }

        public static ProductCollection SetAllProductDataDistinct(List<GetAllProductsDistinct_Result> allProducts)
        {
            ProductCollection collection = new ProductCollection();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts != null && allProducts.Count > 0)
                {
                    foreach (GetAllProductsDistinct_Result product in allProducts)
                    {
                        Product prodTemp = Product.GetShallowProduct(product.ProductNumber, product.SupplierNumber);
                        prodTemp.MinPrice = product.minPrice;
                        prodTemp.MaxPrice = product.maxPrice;

                        PopularProduct pp = db.PopularProducts.Where(c => c.ProductNumber == prodTemp.ProductNumber
                            && c.SupplierNumber == prodTemp.SupplierNumber).FirstOrDefault();

                        if (pp != null)
                            prodTemp.NumberSold = pp.NumberSold ?? 0;

                        if (prodTemp != null)
                            collection.Products.Add(prodTemp);
                    }
                }

                return collection;
            }
        }

        public static ProductCollectionPaged SetAllProductDataDistinctFilter(int pageNumber, int pageSize, List<GetAllProductsDistinctFilter_Result> allProducts)
        {
            ProductCollectionPaged collection = new ProductCollectionPaged();
            List<Product> temp = new List<Product>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts != null && allProducts.Count > 0)
                {
                    foreach (GetAllProductsDistinctFilter_Result product in allProducts)
                    {
                        Product prodTemp = Product.GetShallowProduct((int)product.ProductNumber, (int)product.SupplierNumber);
                        prodTemp.MinPrice = product.minPrice;
                        prodTemp.MaxPrice = product.maxPrice;

                        if (prodTemp != null)
                            temp.Add(prodTemp);
                    }
                }

                collection.Products = (PagedList<Product>)temp.ToPagedList(pageNumber, pageSize);

                return collection;
            }
        }

        public static void SetWebsiteData(ProductCollection allProducts)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (allProducts.Websites != null && allProducts.Websites.Count > 0)
                {
                    foreach (ExternalWebsite website in allProducts.Websites)
                    {
                        int imageNumber = db.ExternalWebsitePictures
                            .Where(c => c.WebsiteNumber == website.LinkId && c.Dimensions == PictureSize.Medium.ToString())
                            .Select(c => c.PictureNumber)
                            .FirstOrDefault();

                        int imageNumberSecondary = db.ExternalWebsitePictures
                            .Where(c => c.WebsiteNumber == website.LinkId && c.Dimensions == PictureSize.Large.ToString())
                            .Select(c => c.PictureNumber)
                            .FirstOrDefault();

                        website.MainImageNumber = imageNumber;
                        website.AdditionalImageNumber = imageNumberSecondary;
                    }
                }
            }
        }

        public override string ToString()
        {
            string toReturn = "";

            if (Products != null && Products.Count > 0)
            {
                foreach (Product product in Products)
                {
                    toReturn += string.Format("{0}", product.ToString());
                }

            }
            return toReturn;
        }
    }
}