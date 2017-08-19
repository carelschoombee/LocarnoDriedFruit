﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace FreeMarket.Models
{
    public class ShoppingCart : ICloneable
    {
        public OrderHeader Order { get; set; }
        public CartBody Body { get; set; }

        public ShoppingCart(int orderNumber)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                Order = db.OrderHeaders.Find(orderNumber);
                Order.VATPercentage = db.VATPercentages.FirstOrDefault().VATPercentage1;
                Body = CartBody.GetDetailsForShoppingCart(Order.OrderNumber);
            }
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public OrderDetail GetOrderDetail(int productNumber, int supplierNumber)
        {
            return Body.OrderDetails
                .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber)
                .FirstOrDefault();
        }

        public void UpdateSelectedProperty(ShoppingCart cart, bool clear)
        {
            if (clear)
            {
                foreach (OrderDetail thisDetail in Body.OrderDetails)
                {
                    thisDetail.Selected = false;
                }
            }
            else
            {
                foreach (OrderDetail thisDetail in Body.OrderDetails)
                {
                    bool selected = cart.Body.OrderDetails
                        .Where(c => c.ProductNumber == thisDetail.ProductNumber && c.SupplierNumber == thisDetail.SupplierNumber)
                        .FirstOrDefault()
                        .Selected;

                    thisDetail.Selected = selected;
                }
            }
        }

        public void Initialize(string userId)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                // Get the order and body from the database with all joining fields

                Order = OrderHeader.GetOrderForShoppingCart(userId);
                Order.VATPercentage = db.VATPercentages.FirstOrDefault().VATPercentage1;
                Body = CartBody.GetDetailsForShoppingCart(Order.OrderNumber);
            }
        }

        public FreeMarketObject AddItemFromProduct(int productNumber, int supplierNumber, int quantity, int custodianNumber, int sizeType)
        {
            // Validate
            if (productNumber == 0 || supplierNumber == 0)
                return new FreeMarketObject { Result = FreeMarketResult.Failure, Argument = null, Message = "No products could be found." };

            FreeMarketObject res = new FreeMarketObject();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                // Check whether the item already exists
                OrderDetail existingItem = Body.OrderDetails
                    .Where(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber && c.SizeType == sizeType)
                    .FirstOrDefault();

                if (existingItem != null)
                {
                    // Update the existing item
                    existingItem.Update(quantity);

                    // Setup return object
                    var productInfo = db.GetProduct(productNumber, supplierNumber, sizeType).FirstOrDefault();

                    if (productInfo == null)
                        return new FreeMarketObject { Result = FreeMarketResult.Failure, Argument = null, Message = "Product, Supplier or Price does not exist." };

                    res.Result = FreeMarketResult.Success;
                    res.Message = string.Format("{0} ({1}) has been added to your cart.", productInfo.Description, quantity);
                }
                else
                {
                    // A new OrderDetail must be created
                    var productInfo = db.GetProduct(productNumber, supplierNumber, sizeType)
                        .FirstOrDefault();

                    if (productInfo == null)
                        return new FreeMarketObject { Result = FreeMarketResult.Failure, Argument = null, Message = "No products could be found." };

                    string status = "Unconfirmed";

                    // Add a small image for the CartBody
                    int imageNumber = db.ProductPictures
                            .Where(c => c.ProductNumber == productInfo.ProductNumber && c.Dimensions == PictureSize.Small.ToString())
                            .Select(c => c.PictureNumber)
                            .FirstOrDefault();

                    // Add the new item to the Session variable
                    Body.OrderDetails.Add(
                        new OrderDetail()
                        {
                            CourierName = null,
                            CustodianNumber = custodianNumber,
                            OrderItemStatus = status,
                            OrderItemValue = productInfo.PricePerUnit * quantity,
                            OrderNumber = Order.OrderNumber,
                            PaidCourier = null,
                            PaidSupplier = null,
                            PayCourier = null,
                            PaySupplier = null,
                            Price = productInfo.PricePerUnit,
                            ProductNumber = productInfo.ProductNumber,
                            ProductDescription = productInfo.Description,
                            ProductDepartment = productInfo.DepartmentName,
                            Quantity = quantity,
                            Settled = null,
                            SupplierNumber = productInfo.SupplierNumberID,
                            SupplierName = productInfo.SupplierName,
                            SizeType = sizeType,
                            MainImageNumber = imageNumber
                        });

                    res.Result = FreeMarketResult.Success;
                    res.Message = string.Format("{0} ({1}) has been added to your cart.", productInfo.Description, quantity);
                }

            }

            // Keep the OrderTotal in sync
            UpdateTotal();

            return res;
        }

        public FreeMarketObject RemoveItem(int itemNumber, int productNumber, int supplierNumber, int sizeType, string userId = null)
        {
            // If the item is in the database
            FreeMarketResult resultDatabase = RemoveItemFromDatabase(itemNumber, userId);

            // Remove the item from the Session as well
            FreeMarketResult resultSession = RemoveItemFromSession(productNumber, supplierNumber, sizeType);

            if (resultDatabase == FreeMarketResult.Success && resultSession == FreeMarketResult.Success)
            {
                Product product = new Product();

                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    product = db.Products.Find(itemNumber);
                }

                // Keep the OrderTotal in sync
                UpdateTotal();

                return new FreeMarketObject { Result = FreeMarketResult.Success, Argument = product };
            }
            else
            {
                UpdateTotal();

                return new FreeMarketObject { Result = FreeMarketResult.Failure, Argument = null };
            }
        }

        private FreeMarketResult RemoveItemFromDatabase(int itemNumber, string userId = null)
        {
            FreeMarketResult result = FreeMarketResult.NoResult;

            if (itemNumber != 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    OrderDetail item = db.OrderDetails.Find(itemNumber);

                    if (item != null)
                    {
                        FreeStock(item.ProductNumber, item.SupplierNumber, (int)item.CustodianNumber, item.Quantity, item.SizeType);

                        db.OrderDetails.Remove(item);
                        db.SaveChanges();

                        AuditUser.LogAudit(8, string.Format("Order number: {0}", Order.OrderNumber), userId);

                        result = FreeMarketResult.Success;
                    }
                    else
                    {
                        result = FreeMarketResult.Failure;
                    }
                }
            }
            else
            {
                result = FreeMarketResult.Success;
            }

            return result;
        }

        private FreeMarketResult RemoveItemFromSession(int productNumber, int supplierNumber, int sizeType)
        {
            Debug.Write(string.Format("Removing Product {0} from Session...", productNumber));

            Product product = new Product();
            FreeMarketResult result = FreeMarketResult.NoResult;

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                product = db.Products.Find(productNumber);

                if (product != null)
                {
                    Body.OrderDetails.RemoveAll(c => c.ProductNumber == productNumber && c.SupplierNumber == supplierNumber && c.SizeType == sizeType);
                    result = FreeMarketResult.Success;
                }
                else
                {
                    result = FreeMarketResult.Failure;
                }
            }

            return result;
        }

        public void SetQuantityOnHand(List<OrderDetail> items)
        {
            foreach (OrderDetail item in items)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    item.QuantityOnHand = db.ProductCustodians
                        .Where(c => c.ProductNumber == item.ProductNumber && c.SupplierNumber == item.SupplierNumber
                        && c.SizeType == item.SizeType && c.CustodianNumber == item.CustodianNumber)
                        .Select(c => c.QuantityOnHand)
                        .FirstOrDefault();
                }
            }
        }

        public FreeMarketObject UpdateQuantities(List<OrderDetail> changedItems)
        {
            FreeMarketObject res = new FreeMarketObject();
            OrderDetail temp = new OrderDetail();

            if (changedItems != null && changedItems.Count > 0)
            {
                SetQuantityOnHand(changedItems);

                foreach (OrderDetail detail in changedItems)
                {
                    temp = Body.OrderDetails
                        .Where(c => c.ProductNumber == detail.ProductNumber
                        && c.SupplierNumber == detail.SupplierNumber && c.SizeType == detail.SizeType)
                        .FirstOrDefault();

                    if (temp != null)
                    {
                        int oldQuantity = temp.Quantity;

                        if (oldQuantity > detail.Quantity)
                        {
                            int difference = oldQuantity - detail.Quantity;

                            FreeStock(detail.ProductNumber, detail.SupplierNumber, (int)detail.CustodianNumber, difference, detail.SizeType);
                        }
                        else
                        {
                            int difference = detail.Quantity - oldQuantity;

                            if (detail.QuantityOnHand > difference)
                                ReserveStock(detail.ProductNumber, detail.SupplierNumber, (int)detail.CustodianNumber, difference, detail.SizeType);
                            else
                            {
                                res.Result = FreeMarketResult.Failure;
                                res.Message += string.Format("\n {0} is out of stock. Please try a smaller quantity.", detail.ProductDescription);
                                continue;
                            }
                        }

                        Body.OrderDetails
                        .Where(c => c.ProductNumber == detail.ProductNumber && c.SupplierNumber == detail.SupplierNumber && c.SizeType == detail.SizeType)
                        .FirstOrDefault()
                        .Quantity = detail.Quantity;
                    }
                }
            }

            // Keep the OrderTotal in sync
            UpdateTotal();

            return res;
        }

        public void UpdatePrices()
        {
            if (Order != null && Order.OrderNumber != 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    Debug.Write(string.Format("Updating prices for order {0} ...", Order.OrderNumber));

                    foreach (OrderDetail item in Body.OrderDetails)
                    {
                        item.ProductPrice = db.ProductSuppliers
                            .Find(item.ProductNumber, item.SupplierNumber, item.SizeType)
                            .PricePerUnit;
                    }
                }

                // Keep the OrderTotal in sync
                UpdateTotal();
            }
        }

        public void Save(string userId = null)
        {
            if (Order.OrderNumber == 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    db.OrderHeaders.Add(Order);
                    db.SaveChanges();
                }
            }

            // Compare the Session cart to the database cart and resolve differences
            Compare();

            // Re-initialize the Body
            Body = CartBody.GetDetailsForShoppingCart(Order.OrderNumber);

            // Keep the total order value in sync
            UpdateTotal();

            // Save the Order total
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                db.Entry(Order).State = EntityState.Modified;
                db.SaveChanges();
            }

            AuditUser.LogAudit(6, string.Format("Order number: {0}", Order.OrderNumber), userId);
        }

        public void Compare()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                // Get a list of items which are on both the Session and database
                List<OrderDetail> existingItems = Body.OrderDetails.FindAll(c => c.ItemNumber != 0);

                if (existingItems != null && existingItems.Count > 0)
                {
                    foreach (OrderDetail temp in existingItems)
                    {
                        OrderDetail tempDb = db.OrderDetails.Find(temp.ItemNumber);

                        if (tempDb != null)
                        {
                            if (!temp.Equals(tempDb))
                            {
                                // If the item has changed update it
                                tempDb.Quantity = temp.Quantity;
                                tempDb.OrderItemValue = temp.OrderItemValue;

                                db.Entry(tempDb).State = EntityState.Modified;
                                db.SaveChanges();

                                AuditUser.LogAudit(7, string.Format("Order number: {0}", Order.OrderNumber));
                            }
                        }
                    }

                }

                // Get a list of items that are on the Session variable but not in the database
                List<OrderDetail> newItems = Body.OrderDetails.FindAll(c => c.ItemNumber == 0);

                if (newItems != null && newItems.Count > 0)
                {
                    foreach (OrderDetail tempB in newItems)
                    {
                        tempB.OrderNumber = Order.OrderNumber;

                        ReserveStock(tempB.ProductNumber, tempB.SupplierNumber, (int)tempB.CustodianNumber, tempB.Quantity, tempB.SizeType);

                        db.OrderDetails.Add(tempB);
                    }

                    db.SaveChanges();

                    AuditUser.LogAudit(7, string.Format("Order number: {0}", Order.OrderNumber));
                }
            }
        }

        public void Merge(string userId, CartBody tempBody)
        {
            if (Order.OrderStatus != "Locked")
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    // Compare the session body with the database
                    foreach (OrderDetail item in tempBody.OrderDetails)
                    {
                        OrderDetail existingItem = Body.OrderDetails
                            .Where(c => c.ProductNumber == item.ProductNumber
                            && c.SupplierNumber == item.SupplierNumber && c.SizeType == item.SizeType)
                            .FirstOrDefault();

                        // If the item does not exist, add it
                        if (existingItem == null)
                            Body.OrderDetails.Add(item);
                        // Otherwise update the quantity only
                        else
                            Body.OrderDetails.Where(c => c.ProductNumber == item.ProductNumber
                            && c.SupplierNumber == item.SupplierNumber && c.SizeType == item.SizeType)
                                .FirstOrDefault()
                                .Quantity += item.Quantity;
                    }
                }

                UpdateTotal();
            }
        }

        public void UpdateDeliveryDetails(SaveCartViewModel model)
        {
            Order.UpdateDeliveryDetails(model);
            Save();
        }

        public static ProductCustodian GetStockAvailable(int productNumber, int supplierNumber, int quantity, int size)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                return db.ProductCustodians
                    .Where(c => c.ProductNumber == productNumber
                    && c.SupplierNumber == supplierNumber && c.SizeType == size && c.QuantityOnHand >= quantity)
                    .FirstOrDefault();
            }
        }

        public decimal CalculateCourierFee()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal totalWeight = GetTotalWeightOfOrder();
                if (totalWeight == 0)
                    return 0;
                else
                    return (decimal)db.CalculateDeliveryFee(totalWeight, Order.OrderNumber).FirstOrDefault();
            }
        }

        public decimal CalculateCourierFeeAdhoc(int postalCode)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal totalWeight = GetTotalWeightOfOrder();
                if (totalWeight == 0)
                    return 0;
                else
                    return (decimal)db.CalculateDeliveryFeeAdhoc(totalWeight, postalCode).FirstOrDefault();
            }
        }

        public decimal CalculateLocalCourierFee()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal totalWeight = GetTotalWeightOfOrder();
                if (totalWeight == 0)
                    return 0;
                else
                    return (decimal)db.CalculateLocalDeliveryFee(totalWeight, Order.OrderNumber).FirstOrDefault();
            }
        }

        public decimal CalculateLocalCourierFeeAdhoc(int postalCode)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal totalWeight = GetTotalWeightOfOrder();
                if (totalWeight == 0)
                    return 0;
                else
                    return (decimal)db.CalculateLocalDeliveryFeeAdhoc(totalWeight, postalCode).FirstOrDefault();
            }
        }

        public decimal CalculatePostalFee()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                decimal totalWeight = GetTotalWeightOfOrder();
                if (totalWeight == 0)
                    return 0;
                else
                    return (decimal)db.CalculatePostOfficeFee(totalWeight).FirstOrDefault();
            }
        }

        public decimal CalculateDeliveryFee()
        {
            // If the user is logged in
            if (Order.OrderNumber != 0)
            {
                if (Order.DeliveryType == "Courier")
                {
                    return CalculateCourierFee();
                }
                //else if (Order.DeliveryType == "LocalCourier")
                //{
                //    decimal fee = CalculateLocalCourierFee();
                //    if (fee > 0)
                //        return fee;
                //    else
                //        return CalculateCourierFee();
                //}
                else // Post Office
                    return CalculatePostalFee();
            }
            else
                return Order.ShippingTotal ?? 0;
        }

        public decimal GetTotalWeightOfOrder()
        {
            decimal totalWeight = 0;
            foreach (OrderDetail od in Body.OrderDetails)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    decimal? weight = db.ProductSizes.Find(od.SizeType).Weight;

                    Product product = db.Products.Find(od.ProductNumber);

                    if (!product.IsVirtual)
                        totalWeight += weight.Value * od.Quantity;
                }
            }
            return totalWeight;
        }

        public void UpdateTotal()
        {
            if (GetTotalWeightOfOrder() > 29)
                Order.DeliveryType = "Courier";

            Body.OrderDetails.ForEach(c => c.OrderItemValue = c.Price * c.Quantity);
            Order.SubTotal = Body.OrderDetails.Sum(c => c.OrderItemValue);
            Order.ShippingTotal = CalculateDeliveryFee();
            Order.TaxTotal = CalculateVAT();
            Order.TotalOrderValue = (Order.SubTotal ?? 0) + (Order.ShippingTotal ?? 0) + (Order.TaxTotal ?? 0);
        }

        public decimal? CalculateVAT()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                VATPercentage VATPercentage = db.VATPercentages.FirstOrDefault();

                if (VATPercentage != null)
                    return Math.Round((Order.SubTotal + Order.ShippingTotal) * (VATPercentage.VATPercentage1 / 100) ?? 0, 2);
                else
                    return 0;
            }
        }

        public FreeMarketResult ReserveStock(int productNumber, int supplierNumber, int custodianNumber, int quantityRequested, int sizeType)
        {
            // Client does not want stock system

            //using (FreeMarketEntities db = new FreeMarketEntities())
            //{
            //    ProductCustodian custodian = db.ProductCustodians.Where(c => c.ProductNumber == productNumber
            //    && c.SupplierNumber == supplierNumber && c.CustodianNumber == custodianNumber && c.SizeType == sizeType)
            //        .FirstOrDefault();

            //    if (custodian == null)
            //        return FreeMarketResult.Failure;

            //    if (custodian.QuantityOnHand >= quantityRequested)
            //    {
            //        if (custodian.StockReservedForOrders == null)
            //            custodian.StockReservedForOrders = 0;

            //        custodian.QuantityOnHand -= quantityRequested;
            //        custodian.StockReservedForOrders += quantityRequested;

            //        db.Entry(custodian).State = EntityState.Modified;
            //        db.SaveChanges();

            return FreeMarketResult.Success;
            //    }
            //    else
            //    {
            //        return FreeMarketResult.Failure;
            //    }
            //}
        }

        public FreeMarketResult FreeStock(int productNumber, int supplierNumber, int custodianNumber, int quantityRequested, int sizeType)
        {
            // Client does not want stock system

            //using (FreeMarketEntities db = new FreeMarketEntities())
            //{
            //    ProductCustodian custodian = db.ProductCustodians.Where(c => c.ProductNumber == productNumber
            //    && c.SupplierNumber == supplierNumber && c.CustodianNumber == custodianNumber && c.SizeType == sizeType)
            //        .FirstOrDefault();

            //    if (custodian == null)
            //        return FreeMarketResult.Failure;

            //    if (custodian.StockReservedForOrders == null)
            //        custodian.StockReservedForOrders = 0;

            //    custodian.QuantityOnHand += quantityRequested;
            //    custodian.StockReservedForOrders -= quantityRequested;

            //    db.Entry(custodian).State = EntityState.Modified;
            //    db.SaveChanges();

            return FreeMarketResult.Success;
            //}
        }

        public static void ReleaseAllStock(int orderNumber)
        {
            // Client does not want stock system

            //using (FreeMarketEntities db = new FreeMarketEntities())
            //{
            //    OrderHeader order = db.OrderHeaders.Find(orderNumber);

            //    if (order == null)
            //        return;

            //    List<OrderDetail> details = db.OrderDetails
            //        .Where(c => c.OrderNumber == orderNumber)
            //        .ToList();

            //    if (details == null || details.Count == 0)
            //        return;

            //    foreach (OrderDetail detail in details)
            //    {
            //        ReleaseStock(detail.ProductNumber, detail.SupplierNumber, detail.CustodianNumber, detail.Quantity, detail.SizeType);
            //    }
            //}
        }

        private static void ReleaseStock(int productNumber, int supplierNumber, int? custodianNumber, int quantity, int sizeType)
        {
            // Client does not want stock system

            //using (FreeMarketEntities db = new FreeMarketEntities())
            //{
            //    ProductCustodian custodian = db.ProductCustodians.Where(c => c.ProductNumber == productNumber
            //    && c.SupplierNumber == supplierNumber && c.CustodianNumber == custodianNumber && c.SizeType == sizeType)
            //            .FirstOrDefault();

            //    if (custodian == null)
            //        return;

            //    if (custodian.StockReservedForOrders == null)
            //        custodian.StockReservedForOrders = 0;

            //    custodian.StockReservedForOrders -= quantity;

            //    if (custodian.StockReservedForOrders < 0)
            //        custodian.StockReservedForOrders = 0;

            //    db.Entry(custodian).State = EntityState.Modified;
            //    db.SaveChanges();
            //}
        }

        public ShoppingCart(string userId)
        {
            Initialize(userId);
        }

        public ShoppingCart()
        {
            Body = new CartBody();
            Order = new OrderHeader();
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                Order.VATPercentage = db.VATPercentages.FirstOrDefault().VATPercentage1;
            }
        }

        public bool IsAllVirtualOrder()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                foreach (OrderDetail detail in Body.OrderDetails)
                {
                    Product product = db.Products
                        .Where(c => c.ProductNumber == detail.ProductNumber)
                        .FirstOrDefault();

                    if (product != null)
                    {
                        if (!product.IsVirtual)
                            return false;
                    }
                }

                return true;
            }
        }

        public void SetOrderConfirmed(string userId)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                OrderHeader order = db.OrderHeaders.Find(Order.OrderNumber);

                if (order == null)
                    return;

                order.OrderStatus = "Confirmed";
                order.OrderDatePlaced = DateTime.Now;
                order.PaymentReceived = true;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

                ReleaseAllStock(order.OrderNumber);

                OrderHeader.UpdatePopularItems(order.OrderNumber);

                Initialize(userId);
            }
        }

        public static void SetOrderConfirmedFromNotify(int orderNumber)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                OrderHeader order = db.OrderHeaders.Find(orderNumber);

                if (order == null)
                    return;

                order.OrderStatus = "Confirmed";
                order.OrderDatePlaced = DateTime.Now;
                order.PaymentReceived = true;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

                ReleaseAllStock(order.OrderNumber);
            }
        }

        public void CancelOrder(string userId)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                OrderHeader order = db.OrderHeaders.Find(Order.OrderNumber);

                if (order == null)
                    return;

                order.OrderStatus = "Cancelled";
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

                foreach (OrderDetail temp in Body.OrderDetails)
                {
                    temp.OrderItemStatus = "Cancelled";
                    db.Entry(temp).State = EntityState.Modified;
                }

                db.SaveChanges();
            }

            Initialize(userId);
        }

        public override string ToString()
        {
            string toString = "";

            if (Order != null && Body != null)
            {
                toString += "\nShopping Cart Contents:\n";
                toString += string.Format("\n{0}\n{1}\n", Order.ToString(), Body.ToString());
            }

            return toString;
        }
    }
}