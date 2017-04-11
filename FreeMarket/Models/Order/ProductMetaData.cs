﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FreeMarket.Models
{
    [MetadataType(typeof(ProductMetaData))]
    public partial class Product
    {
        public int MainImageNumber { get; set; }
        public int SecondaryImageNumber { get; set; }
        public int AdditionalImageNumber { get; set; }

        [DisplayName("Supplier Number")]
        public int SupplierNumber { get; set; }

        [DisplayName("Supplier Name")]
        public string SupplierName { get; set; }

        [DisplayName("Department Name")]
        public string DepartmentName { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        [DisplayName("Price Per Unit")]
        public decimal PricePerUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        [DisplayName("Special Price Per Unit")]
        public decimal SpecialPricePerUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        [DisplayName("Retail Price Per Unit")]
        public decimal RetailPricePerUnit { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string SelectedDepartment { get; set; }
        public List<SelectListItem> Departments { get; set; }

        public string SelectedSupplier { get; set; }
        public List<SelectListItem> Suppliers { get; set; }

        public int? ReviewId { get; set; }
        public int? ProductRating { get; set; }

        [DataType(DataType.MultilineText)]
        public string ProductReviewText { get; set; }
        public int? PriceRating { get; set; }
        public decimal PriceOrder { get; set; }

        public int ProductReviewsCount { get; set; }

        public bool CustodianActivated { get; set; }

        [Required]
        [DisplayName("Custodian")]
        public int SelectedCustodianNumber { get; set; }

        public List<SelectListItem> Custodians { get; set; }
        public Dictionary<string, int> CustodianInfo { get; set; }

        [DisplayName("Qty")]
        public int CashQuantity { get; set; }
        public string SelectedPrice { get; set; }
        public List<SelectListItem> Prices { get; set; }

        public List<ProductSize> SizeVariations { get; set; }

        public static string GetFullDescription(int productNumber, int supplierNumber)
        {
            if (productNumber == 0 || supplierNumber == 0)
                return "";

            string toReturn = "";

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                Product product = db.Products.Find(productNumber);
                Supplier supplier = db.Suppliers.Find(supplierNumber);

                if (product != null && supplier != null)
                {
                    //toReturn = String.Format("{0} {1} {2} supplied by {3}", product.Weight, "KG", product.Description, supplier.SupplierName);
                    toReturn = String.Format("{0} - {1}", product.Description, supplier.SupplierName);
                }
            }

            return toReturn;
        }

        public static Product GetProduct(int productNumber, int supplierNumber)
        {
            Product product = new Product();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                var productInfo = db.GetProduct(productNumber, supplierNumber)
                    .FirstOrDefault();

                product = new Product
                {
                    Activated = productInfo.Activated,
                    DateAdded = productInfo.DateAdded,
                    DateModified = productInfo.DateModified,
                    DepartmentName = productInfo.DepartmentName,
                    DepartmentNumber = productInfo.DepartmentNumber,
                    Description = productInfo.Description,
                    PricePerUnit = productInfo.PricePerUnit,
                    ProductNumber = productInfo.ProductNumber,
                    Size = "",
                    SupplierName = productInfo.SupplierName,
                    SupplierNumber = productInfo.SupplierNumberID,
                    SpecialPricePerUnit = productInfo.SpecialPricePerUnit ?? productInfo.PricePerUnit,
                    RetailPricePerUnit = productInfo.RetailPricePerUnit ?? productInfo.PricePerUnit,
                    Weight = 0,
                    LongDescription = productInfo.LongDescription,
                    IsVirtual = productInfo.IsVirtual
                };

                product.MainImageNumber = db.ProductPictures
                    .Where(c => c.ProductNumber == product.ProductNumber && c.Dimensions == PictureSize.Medium.ToString())
                    .Select(c => c.PictureNumber)
                    .FirstOrDefault();

                product.SecondaryImageNumber = db.ProductPictures
                    .Where(c => c.ProductNumber == product.ProductNumber && c.Dimensions == PictureSize.Small.ToString())
                    .Select(c => c.PictureNumber)
                    .FirstOrDefault();

                product.AdditionalImageNumber = db.ProductPictures
                    .Where(c => c.ProductNumber == product.ProductNumber && c.Dimensions == PictureSize.Large.ToString())
                    .Select(c => c.PictureNumber)
                    .FirstOrDefault();

                product.Departments = db.Departments
                    .Select(c => new SelectListItem
                    {
                        Text = "(" + c.DepartmentNumber + ") " + c.DepartmentName,
                        Value = c.DepartmentNumber.ToString(),
                        Selected = c.DepartmentNumber == product.DepartmentNumber ? true : false
                    })
                    .ToList();

                product.Custodians = db.Custodians
                    .Select(c => new SelectListItem
                    {
                        Text = "(" + c.CustodianNumber + ") " + c.CustodianName,
                        Value = c.CustodianNumber.ToString(),
                        Selected = c.CustodianNumber == product.SelectedCustodianNumber ? true : false
                    })
                    .ToList();
            }

            return product;
        }

        public void InitializeDropDowns(string mode)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (mode == "edit")
                {
                    Departments = db.Departments
                        .Select(c => new SelectListItem
                        {
                            Text = "(" + c.DepartmentNumber + ") " + c.DepartmentName,
                            Value = c.DepartmentNumber.ToString(),
                            Selected = (c.DepartmentNumber == DepartmentNumber) ? true : false
                        })
                        .ToList();

                    CustodianInfo = db.GetCustodianInfo(this.ProductNumber, this.SupplierNumber)
                        .Select(c => new { c.CustodianName, c.QuantityOnHand })
                        .ToDictionary(c => c.CustodianName, c => c.QuantityOnHand);
                }
                else
                {
                    Departments = db.Departments
                        .Select(c => new SelectListItem
                        {
                            Text = "(" + c.DepartmentNumber + ") " + c.DepartmentName,
                            Value = c.DepartmentNumber.ToString(),
                        })
                        .ToList();
                }

                if (mode == "create")
                {
                    Suppliers = db.Suppliers
                        .Select(c => new SelectListItem
                        {
                            Text = "(" + c.SupplierNumber + ") " + c.SupplierName,
                            Value = c.SupplierNumber.ToString()
                        })
                        .ToList();

                    Custodians = db.Custodians
                        .Select(c => new SelectListItem
                        {
                            Text = "(" + c.CustodianNumber + ") " + c.CustodianName,
                            Value = c.CustodianNumber.ToString(),
                        })
                        .ToList();

                    SizeVariations = ProductSize.GetNewProductSizes();
                }
            }
        }

        public static Product GetNewProduct()
        {
            Product product = new Product();

            product.DateAdded = DateTime.Now;

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                product.Departments = db.Departments
                    .Select(c => new SelectListItem
                    {
                        Text = "(" + c.DepartmentNumber + ")" + c.DepartmentName,
                        Value = c.DepartmentNumber.ToString()
                    })
                    .ToList();

                product.Suppliers = db.Suppliers
                    .Select(c => new SelectListItem
                    {
                        Text = "(" + c.SupplierNumber + ")" + c.SupplierName,
                        Value = c.SupplierNumber.ToString()
                    })
                    .ToList();

                product.Custodians = db.Custodians
                    .Select(c => new SelectListItem
                    {
                        Text = "(" + c.CustodianNumber + ") " + c.CustodianName,
                        Value = c.CustodianNumber.ToString(),
                    })
                    .ToList();

                product.SizeVariations = ProductSize.GetNewProductSizes();

            }

            return product;
        }

        public static void CreateNewProduct(Product product)
        {
            try
            {
                product.DateAdded = DateTime.Now;
                product.DateModified = DateTime.Now;
                product.DepartmentNumber = int.Parse(product.SelectedDepartment);
            }
            catch
            {
                return;
            }

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                db.Products.Add(product);
                db.SaveChanges();

                foreach (ProductSize item in product.SizeVariations)
                {
                    if (item.PricePerUnit > 0 && item.Activated == true)
                    {
                        ProductSupplier productSupplierDb = new ProductSupplier()
                        {
                            ProductNumber = product.ProductNumber,
                            SupplierNumber = int.Parse(product.SelectedSupplier),
                            PricePerUnit = item.PricePerUnit,
                            SpecialPricePerUnit = 0,
                            RetailPricePerUnit = 0,
                            SizeType = item.SizeId
                        };

                        db.ProductSuppliers.Add(productSupplierDb);
                        db.SaveChanges();
                    }
                }

                Custodian custodian = db.Custodians.Find(product.SelectedCustodianNumber);

                if (custodian == null)
                    return;
                try
                {
                    foreach (ProductSize item in product.SizeVariations)
                    {
                        if (item.PricePerUnit > 0 && item.Activated == true)
                        {
                            ProductCustodian productCustodianDb = new ProductCustodian()
                            {
                                AmountLastIncreasedBySupplier = null,
                                CustodianNumber = product.SelectedCustodianNumber,
                                DateLastIncreasedBySupplier = null,
                                ProductNumber = product.ProductNumber,
                                SupplierNumber = int.Parse(product.SelectedSupplier),
                                QuantityOnHand = 0,
                                StockReservedForOrders = 0,
                                SizeType = item.SizeId
                            };

                            db.ProductCustodians.Add(productCustodianDb);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception e)
                {
                    ExceptionLogging.LogException(e);
                }
            }
        }

        public static void SaveProduct(Product product)
        {
            Product productDb = new Product();
            ProductSupplier productSupplierDb = new ProductSupplier();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                productDb = db.Products.Find(product.ProductNumber);

                if (productDb != null)
                {
                    productDb.Activated = product.Activated;
                    productDb.DateModified = DateTime.Now;
                    productDb.DepartmentNumber = int.Parse(product.SelectedDepartment);
                    productDb.Description = product.Description;
                    productDb.LongDescription = product.LongDescription;
                    productDb.Size = product.Size;
                    productDb.Weight = product.Weight;
                    productDb.IsVirtual = product.IsVirtual;
                    db.Entry(productDb).State = EntityState.Modified;
                }

                productSupplierDb = db.ProductSuppliers.Find(product.ProductNumber, product.SupplierNumber);

                if (product.PricePerUnit != productSupplierDb.PricePerUnit)
                {
                    PriceHistory history = new PriceHistory()
                    {
                        OldPrice = productSupplierDb.PricePerUnit,
                        NewPrice = product.PricePerUnit,
                        ProductNumber = product.ProductNumber,
                        SupplierNumber = product.SupplierNumber,
                        Date = DateTime.Now,
                        Type = "Normal"
                    };

                    db.PriceHistories.Add(history);
                }

                if (product.SpecialPricePerUnit != productSupplierDb.SpecialPricePerUnit)
                {
                    PriceHistory history = new PriceHistory()
                    {
                        OldPrice = productSupplierDb.SpecialPricePerUnit,
                        NewPrice = product.SpecialPricePerUnit,
                        ProductNumber = product.ProductNumber,
                        SupplierNumber = product.SupplierNumber,
                        Date = DateTime.Now,
                        Type = "Special"
                    };

                    db.PriceHistories.Add(history);
                }

                if (product.RetailPricePerUnit != productSupplierDb.RetailPricePerUnit)
                {
                    PriceHistory history = new PriceHistory()
                    {
                        OldPrice = productSupplierDb.RetailPricePerUnit,
                        NewPrice = product.RetailPricePerUnit,
                        ProductNumber = product.ProductNumber,
                        SupplierNumber = product.SupplierNumber,
                        Date = DateTime.Now,
                        Type = "Retail"
                    };

                    db.PriceHistories.Add(history);
                }

                if (productSupplierDb != null)
                {
                    productSupplierDb.PricePerUnit = product.PricePerUnit;
                    productSupplierDb.SpecialPricePerUnit = product.SpecialPricePerUnit;
                    productSupplierDb.RetailPricePerUnit = product.RetailPricePerUnit;
                    db.Entry(productSupplierDb).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }

        public static FreeMarketResult SaveProductImage(int productNumber, PictureSize size, HttpPostedFileBase image)
        {
            if (image != null && productNumber != 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    ProductPicture picture = new ProductPicture();

                    Product product = db.Products.Find(productNumber);

                    if (product == null)
                        return FreeMarketResult.Failure;

                    picture = db.ProductPictures
                    .Where(c => c.ProductNumber == productNumber && c.Dimensions == size.ToString())
                    .FirstOrDefault();

                    try
                    {
                        // No picture exists for this dimension/product
                        if (picture == null)
                        {
                            picture = new ProductPicture();
                            picture.Picture = new byte[image.ContentLength];
                            picture.Annotation = product.Description;
                            picture.Dimensions = size.ToString();
                            image.InputStream.Read(picture.Picture, 0, image.ContentLength);
                            picture.PictureMimeType = image.ContentType;
                            picture.ProductNumber = productNumber;

                            db.ProductPictures.Add(picture);
                            db.SaveChanges();

                            AuditUser.LogAudit(9, string.Format("Product: {0}", productNumber));
                            return FreeMarketResult.Success;
                        }
                        else
                        {
                            picture.Annotation = product.Description;
                            picture.Picture = new byte[image.ContentLength];
                            image.InputStream.Read(picture.Picture, 0, image.ContentLength);
                            picture.PictureMimeType = image.ContentType;

                            db.Entry(picture).State = EntityState.Modified;
                            db.SaveChanges();

                            AuditUser.LogAudit(9, string.Format("Product: {0}", productNumber));
                            return FreeMarketResult.Success;
                        }
                    }
                    catch (Exception e)
                    {
                        ExceptionLogging.LogException(e);
                        return FreeMarketResult.Failure;
                    }
                }
            }

            return FreeMarketResult.Failure;
        }

        public override string ToString()
        {
            string toReturn = "";

            if (ProductNumber != 0 && SupplierNumber != 0 && Description != null && PricePerUnit != 0)
            {
                toReturn += string.Format("\nProduct Number     : {0}", ProductNumber);
                toReturn += string.Format("\nSupplier Number    : {0}", SupplierNumber);
                toReturn += string.Format("\nDescription        : {0}", Description);
                toReturn += string.Format("\nPrice Per Unit     : {0}", PricePerUnit);
            }

            return toReturn;
        }
    }
    public class ProductMetaData
    {
        [Required]
        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Is this an Advert?")]
        public bool IsVirtual { get; set; }

        [DisplayName("Size")]
        public string Size { get; set; }

        [DisplayName("Weight (KG)")]
        public decimal Weight { get; set; }

        [DisplayName("Product Number")]
        public int ProductNumber { get; set; }

        [DisplayName("Date Added")]
        public DateTime DateAdded { get; set; }

        [DisplayName("Date Modified")]
        public DateTime DateModified { get; set; }

        [DisplayName("Department Number")]
        public DateTime DepartmentNumber { get; set; }

        [DisplayName("Long Description")]
        public string LongDescription { get; set; }
    }
}