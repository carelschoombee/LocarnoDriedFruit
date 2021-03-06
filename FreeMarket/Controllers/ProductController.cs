﻿using FreeMarket.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Controllers
{
    [RequireHttps]
    public class ProductController : Controller
    {
        FreeMarketEntities db = new FreeMarketEntities();

        // GET: Product
        public ActionResult View(int id)
        {
            ProductCollection model = ProductCollection.GetProductsByDepartment(id);

            return View("Index", model);
        }

        public ActionResult AllProductsIndex()
        {
            ProductCollection model = ProductCollection.GetAllProducts();

            PagedList<Product> products = (PagedList<Product>)model.Products.ToPagedList(1, 12);

            ProductCollectionPaged pagedProducts = new ProductCollectionPaged();

            pagedProducts.Products = products;

            return View("AllProductsIndex", pagedProducts);
        }

        public ActionResult IndexPlain()
        {
            return View();
        }

        public ActionResult Departments()
        {
            List<Department> departments = Department.GetModel();

            if (departments != null && departments.Count > 0)
                return PartialView("_Departments", departments);

            return PartialView("_Departments");
        }

        public ActionResult FilterProduct(int? PageNumber, ProductCollectionPaged model)
        {
            int pageNumber = (PageNumber ?? 1);
            int pageSizeNo = 12;

            ViewBag.pageNumber = pageNumber;
            ViewBag.pageSizeNo = pageSizeNo;

            if (ModelState.IsValid)
            {
                if (model.SelectedDepartment == 9999)
                {
                    if (!string.IsNullOrEmpty(model.ProductSearchCriteria))
                    {
                        ProductCollectionPaged products = ProductCollectionPaged.GetProductsFiltered(pageNumber, pageSizeNo, model.ProductSearchCriteria);
                        products.SelectedDepartment = model.SelectedDepartment;
                        products.ProductSearchCriteria = model.ProductSearchCriteria;

                        return View("AllProductsIndex", products);
                    }
                    else
                    {
                        return View("AllProductsIndex", new ProductCollectionPaged());
                    }
                }
                else if (model.SelectedDepartment != 0)
                {
                    ProductCollectionPaged products = ProductCollectionPaged.GetProductsByDepartment(pageNumber, pageSizeNo, model.SelectedDepartment);
                    products.SelectedDepartment = model.SelectedDepartment;
                    products.ProductSearchCriteria = model.ProductSearchCriteria;

                    return View("AllProductsIndex", products);
                }
                else
                {
                    ProductCollection pcp = ProductCollection.GetAllProducts();

                    PagedList<Product> products = (PagedList<Product>)pcp.Products.ToPagedList(pageNumber, pageSizeNo);

                    ProductCollectionPaged pagedProducts = new ProductCollectionPaged();

                    pagedProducts.Products = products;

                    pagedProducts.SelectedDepartment = model.SelectedDepartment;
                    pagedProducts.ProductSearchCriteria = model.ProductSearchCriteria;

                    return View("AllProductsIndex", pagedProducts);
                }
            }
            else
            {
                return View("AllProductsIndex", new ProductCollectionPaged());
            }
        }

        [ChildActionOnly]
        public ActionResult GetAllProducts()
        {
            ProductCollection products = ProductCollection.GetAllProducts();

            if (products.Products != null && products.Products.Count > 0)
                return PartialView("_ShowAllProducts", products);
            else
                return PartialView("_ShowAllProducts", new ProductCollection());
        }

        public ActionResult GetFullDescription(int productNumber, int supplierNumber)
        {
            return Content(Product.GetFullDescription(productNumber, supplierNumber));
        }


        public ActionResult GetDimensions(int productNumber)
        {
            string toReturn = "";
            Product product;

            if (productNumber != 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    product = db.Products
                       .Where(c => c.ProductNumber == productNumber)
                       .FirstOrDefault();

                    if (product != null)
                    {
                        if (product.Weight < 1)
                            toReturn = string.Format("{0} {1}", Math.Round(product.Weight.Value * 1000, 0), "Grams");
                        else
                            toReturn = string.Format("{0} {1}", product.Weight, "KG");
                    }
                }
            }

            return Content(toReturn);
        }

        public ActionResult GetAverageRating(int productNumber, int supplierNumber)
        {
            string toReturn = "";
            double? averageRating = ProductReviewsCollection.CalculateAverageRatingOnly(productNumber, supplierNumber);

            if (averageRating == null || averageRating == 0)
                toReturn = "No reviews written yet.";
            else
                toReturn = averageRating.ToString() + " / 3";

            return Content(toReturn);
        }

        public ActionResult GetReviews(int productNumber, int supplierNumber, int size = 4)
        {
            ProductReviewsCollection reviews = ProductReviewsCollection.GetReviewsOnly(productNumber, supplierNumber, size);

            return PartialView("_RatingPartial", reviews);
        }

        [HttpPost]
        public JsonResult LoadMoreReviews(int productNumber, int supplierNumber, int size)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                ProductReviewsCollection model = new ProductReviewsCollection();

                model.ProductNumber = productNumber;
                model.SupplierNumber = supplierNumber;

                List<ProductReview> collection = db.ProductReviews
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber && c.Approved == true)
                    .OrderByDescending(p => p.ReviewId)
                    .Skip(size)
                    .Take(size)
                    .ToList();

                model.Reviews = collection;

                int modelCount = db.ProductReviews
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber && c.Approved == true)
                    .Count();

                if (model.Reviews.Any())
                {
                    string modelString = RenderRazorViewToString("_RatingPartialLoadMore", model);
                    return Json(new { ModelString = modelString, ModelCount = modelCount });
                }
                return Json(model);
            }
        }

        public ActionResult GetAllowedSizes(int id)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                ProductSize size = db.ProductSizes.Find(id);

                if (size == null)
                    return Content("");

                return Content(size.Description);
            }
        }

        [ChildActionOnly]
        public ActionResult MostPopularProducts()
        {
            ProductCollection products = ProductCollection.GetMostPopularProducts();

            if (products.Products != null && products.Products.Count > 0)
                return PartialView("_ShowAllProducts", products);
            else
                return PartialView("_ShowAllProducts", new ProductCollection());
        }

        public string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext =
                     new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}