﻿using FreeMarket.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FreeMarket.Controllers
{
    [RequireHttps]
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            Dashboard model = new Dashboard(null, "Year");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Dashboard(Dashboard data, string yearView, string monthView)
        {
            Dashboard model = new Dashboard();
            string periodType = "";

            if (!string.IsNullOrEmpty(yearView))
            {
                periodType = "Year";
            }
            else if (!string.IsNullOrEmpty(monthView))
            {
                periodType = "Month";
            }

            if (periodType == "Year")
            {
                model = new Dashboard(data.SelectedYear, periodType);
            }
            else
            {
                model = new Dashboard(data.SelectedMonth, periodType);
            }
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkComplete(List<OrderHeader> orders)
        {
            List<OrderHeader> selected = orders
                .Where(c => c.Selected)
                .ToList();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (selected.Count > 0)
                {
                    foreach (OrderHeader oh in selected)
                    {
                        OrderHeader order = db.OrderHeaders.Find(oh.OrderNumber);

                        if (order != null)
                        {
                            order.OrderStatus = "Complete";
                            db.Entry(order).State = System.Data.Entity.EntityState.Modified;

                            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(oh.CustomerNumber);
                            if (user.UnsubscribeFromRatings == false)
                            {
                                OrderHeader.SendRatingEmail(order.CustomerNumber, order.OrderNumber);
                            }
                        }
                    }

                    db.SaveChanges();
                }
            }

            return RedirectToAction("DeliverPartialTable", "Admin");
        }

        public ActionResult DeliverPartialTable()
        {
            List<OrderHeader> confirmedOrders = new List<OrderHeader>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                confirmedOrders = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed").ToList();
            }

            return PartialView("_ConfirmedOrders", confirmedOrders);
        }

        public ActionResult ProductsIndex()
        {
            ProductCollection collection = ProductCollection.GetAllProducts();

            return View(collection);
        }

        public ActionResult SuppliersIndex()
        {
            SuppliersCollection collection = SuppliersCollection.GetAllSuppliers();

            return View(collection);
        }

        public ActionResult CreateProduct()
        {
            Product product = Product.GetNewProduct();

            return View(product);
        }

        public ActionResult CreateSupplier()
        {
            Supplier supplier = Supplier.GetNewSupplier();

            return View(supplier);
        }

        public ActionResult DownloadReport()
        {
            DownloadReportViewModel model = new DownloadReportViewModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadReportProcess(DownloadReportViewModel model)
        {
            MemoryStream stream = new MemoryStream();

            if (ModelState.IsValid)
            {
                Dictionary<MemoryStream, string> obj = OrderHeader.GetOrderReport(model.OrderNumber);

                if (obj == null || obj.Count == 0)
                {
                    TempData["errorMessage"] = "An error occurred during report creation.";
                    return View("DownloadReport", model);
                }

                return File(obj.FirstOrDefault().Key, obj.FirstOrDefault().Value, string.Format("Order {0}.pdf", model.OrderNumber));
            }
            else
            {
                TempData["errorMessage"] = "That report does not exist";
                return View("DownloadReport", model);
            }
        }

        public ActionResult DownloadReportConfirmed(int orderNumber)
        {
            MemoryStream stream = new MemoryStream();

            if (ModelState.IsValid)
            {
                Dictionary<MemoryStream, string> obj = OrderHeader.GetDeliveryInstructions(orderNumber);

                if (obj == null || obj.Count == 0)
                {
                    TempData["errorMessage"] = "An error occurred during report creation.";
                    return View("Index");
                }

                return File(obj.FirstOrDefault().Key, obj.FirstOrDefault().Value, string.Format("Order {0}.pdf", orderNumber));
            }
            else
            {
                TempData["errorMessage"] = "That report does not exist";
                return View("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProductProcess(Product product, HttpPostedFileBase imagePrimary, HttpPostedFileBase imageSecondary)
        {
            if (ModelState.IsValid)
            {
                Product.CreateNewProduct(product);

                FreeMarketResult resultPrimary = FreeMarketResult.NoResult;
                FreeMarketResult resultSecondary = FreeMarketResult.NoResult;

                if (imagePrimary != null)
                    resultPrimary = Product.SaveProductImage(product.ProductNumber, PictureSize.Medium, imagePrimary);

                if (imageSecondary != null)
                    resultSecondary = Product.SaveProductImage(product.ProductNumber, PictureSize.Small, imageSecondary);

                if (resultPrimary == FreeMarketResult.Success && resultSecondary == FreeMarketResult.Success)
                    TempData["message"] = string.Format("Images uploaded and product saved for product {0}.", product.ProductNumber);

                return RedirectToAction("ProductsIndex", "Admin");
            }

            product.InitializeDropDowns("create");

            return View("CreateProduct", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSupplierProcess(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                Supplier.CreateNewSupplier(supplier);

                return RedirectToAction("SuppliersIndex", "Admin");
            }

            return View("CreateSupplier", supplier);
        }

        public ActionResult EditProduct(int productNumber, int supplierNumber)
        {
            if (productNumber == 0 || supplierNumber == 0)
                return RedirectToAction("ProductsIndex", "Admin");

            Product product = Product.GetProduct(productNumber, supplierNumber);

            return View(product);
        }

        public ActionResult EditSupplier(int supplierNumber)
        {
            if (supplierNumber == 0)
                return RedirectToAction("SuppliersIndex", "Admin");

            Supplier supplier = Supplier.GetSupplier(supplierNumber);

            return View(supplier);
        }

        public ActionResult GetCustomerName(int orderNumber)
        {
            OrderHeader order = new OrderHeader();
            ApplicationUser user = new ApplicationUser();
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                order = db.OrderHeaders.Find(orderNumber);

                if (order == null)
                    return Content("Customer");

                user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(order.CustomerNumber);
            }

            return Content(user.Name);
        }

        public ActionResult GetCustomerPhone(int orderNumber)
        {
            OrderHeader order = new OrderHeader();
            ApplicationUser user = new ApplicationUser();
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                order = db.OrderHeaders.Find(orderNumber);

                if (order == null)
                    return Content("Customer");

                user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(order.CustomerNumber);
            }

            return Content(user.PhoneNumber);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProductProcess(Product product, HttpPostedFileBase imagePrimary, HttpPostedFileBase imageSecondary)
        {
            if (ModelState.IsValid)
            {
                Product.SaveProduct(product);

                FreeMarketResult resultPrimary = FreeMarketResult.NoResult;
                FreeMarketResult resultSecondary = FreeMarketResult.NoResult;

                if (imagePrimary != null)
                    resultPrimary = Product.SaveProductImage(product.ProductNumber, PictureSize.Medium, imagePrimary);

                if (imageSecondary != null)
                    resultSecondary = Product.SaveProductImage(product.ProductNumber, PictureSize.Small, imageSecondary);

                if (resultPrimary == FreeMarketResult.Success && resultSecondary == FreeMarketResult.Success)
                    TempData["message"] = string.Format("Images uploaded and product saved for product {0}.", product.ProductNumber);

                return RedirectToAction("ProductsIndex", "Admin");
            }

            product.InitializeDropDowns("edit");

            return View("EditProduct", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSupplierProcess(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                Supplier.SaveSupplier(supplier);

                return RedirectToAction("SuppliersIndex", "Admin");
            }

            return View("EditSupplier", supplier);
        }
    }
}