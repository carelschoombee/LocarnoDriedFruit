﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarket.Models
{
    public class SalesInfo
    {
        public decimal TotalSalesGateway { get; set; }
        public decimal TotalSalesOrders { get; set; }
        public decimal TotalShippingOrders { get; set; }
        public Dictionary<string, decimal> SalesDetails { get; set; }
        public Dictionary<string, int> TransactionDetails { get; set; }
        public Dictionary<string, int> PaymentDetails { get; set; }

        public int TotalConfirmedOrders { get; set; }
        public int TotalCompletedOrders { get; set; }
        public int TotalCancelledOrders { get; set; }
        public int TotalLockedOrders { get; set; }
        public int TotalUnconfirmedOrders { get; set; }
        public int TotalRefundedOrders { get; set; }
        public int TotalInvoicedOrders { get; set; }

        public int NumberOrdersSingleItem { get; set; }
        public int NumberOrdersTwoToFive { get; set; }
        public int NumberOrdersGreaterFive { get; set; }

        public Dictionary<string, decimal> CalculateSalesDetails(int year, List<OrderHeader> orders, List<CashOrder> cashOrders)
        {
            Dictionary<string, decimal> sales;

            if (year == 0)
            {
                DateTime? minDate = orders.Min(c => c.OrderDatePlaced);
                DateTime? maxDate = orders.Max(c => c.OrderDatePlaced);
                DateTime? maxDate2 = cashOrders.Max(c => c.DatePlaced);

                if (maxDate2 > maxDate)
                    maxDate = maxDate2;

                int minYear = 0;

                if (minDate == null)
                    minYear = DateTime.Now.Year - 1;
                else
                    minYear = minDate.Value.Year - 1;

                int maxYear = 0;

                if (maxDate == null)
                    maxYear = DateTime.Now.Year;
                else
                    maxYear = maxDate.Value.Year;

                sales = new Dictionary<string, decimal>();

                int i = minYear;

                while (i <= maxYear)
                {
                    sales.Add(i.ToString(), 0);
                    i++;
                }

                foreach (OrderHeader o in orders)
                {
                    if (sales.ContainsKey(((DateTime)o.OrderDatePlaced).Year.ToString()))
                    {
                        sales[((DateTime)o.OrderDatePlaced).Year.ToString()] += o.TotalOrderValue;
                    }
                }

                foreach (CashOrder o in cashOrders)
                {
                    if (sales.ContainsKey(((DateTime)o.DatePlaced).Year.ToString()))
                    {
                        sales[((DateTime)o.DatePlaced).Year.ToString()] += (decimal)o.Total;
                    }
                }
            }
            else
            {
                sales = new Dictionary<string, decimal>()
                    {
                        { "January", 0},
                        { "February", 0},
                        { "March", 0},
                        { "April", 0},
                        { "May", 0 },
                        { "June",0 },
                        { "July",0 },
                        { "August",0 },
                        { "September",0 },
                        { "October", 0 },
                        { "November", 0 },
                        { "December", 0 }
                    };

                foreach (OrderHeader o in orders)
                {
                    if (sales.ContainsKey(((DateTime)o.OrderDatePlaced).ToString("MMMM")) && (o.OrderDatePlaced.Value.Year == year))
                    {
                        sales[((DateTime)o.OrderDatePlaced).ToString("MMMM")] += o.TotalOrderValue;
                    }
                }

                foreach (CashOrder o in cashOrders)
                {
                    if (sales.ContainsKey(((DateTime)o.DatePlaced).ToString("MMMM")) && (o.DatePlaced.Value.Year == year))
                    {
                        sales[((DateTime)o.DatePlaced).ToString("MMMM")] += (decimal)o.Total;
                    }
                }
            }

            if (sales == null)
            {
                sales = new Dictionary<string, decimal>();
            }

            return sales;
        }

        public Dictionary<string, decimal> CalculateSalesDetails(DateTime date, List<OrderHeader> orders, List<CashOrder> cashOrders)
        {
            Dictionary<string, decimal> sales;

            sales = new Dictionary<string, decimal>()
                {
                    { "1", 0 },
                    { "2", 0 },
                    { "3", 0 },
                    { "4", 0 },
                    { "5", 0 },
                };

            foreach (OrderHeader o in orders)
            {
                if (sales.ContainsKey(((DateTime)o.OrderDatePlaced).GetWeekOfMonth().ToString()) && (o.OrderDatePlaced.Value.Year == date.Year) && (o.OrderDatePlaced.Value.Month == date.Month))
                {
                    sales[((DateTime)o.OrderDatePlaced).GetWeekOfMonth().ToString()] += o.TotalOrderValue;
                }
            }

            foreach (CashOrder o in cashOrders)
            {
                if (sales.ContainsKey(((DateTime)o.DatePlaced).GetWeekOfMonth().ToString()) && (o.DatePlaced.Value.Year == date.Year) && (o.DatePlaced.Value.Month == date.Month))
                {
                    sales[((DateTime)o.DatePlaced).GetWeekOfMonth().ToString()] += (decimal)o.Total;
                }
            }

            if (sales == null)
            {
                sales = new Dictionary<string, decimal>();
            }

            return sales;
        }

        public Dictionary<string, int> GetTransactionDetails(int? year, DateTime? date, List<PaymentGatewayMessage> messages)
        {
            List<PaymentGatewayTransactionStatu> statuses = new List<PaymentGatewayTransactionStatu>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                statuses = db.PaymentGatewayTransactionStatus.ToList();
            }

            Dictionary<string, int> tempTranscationCodes = new Dictionary<string, int>();
            Dictionary<string, int> transactionCodes = new Dictionary<string, int>();

            foreach (PaymentGatewayTransactionStatu status in statuses)
            {
                tempTranscationCodes.Add(status.TransactionCode.ToString(), 0);
            }

            foreach (PaymentGatewayMessage o in messages)
            {
                if (o.TransactionStatus != null && o.Transaction_Date != null)
                {
                    if (date == null)
                    {
                        if (year == 0)
                        {
                            if (tempTranscationCodes.ContainsKey(o.TransactionStatus.ToString()))
                            {
                                tempTranscationCodes[o.TransactionStatus.ToString()] += 1;
                            }
                        }
                        else
                        {
                            if (tempTranscationCodes.ContainsKey(o.TransactionStatus.ToString())
                                && DateTime.Parse(o.Transaction_Date).Year == year)
                            {
                                tempTranscationCodes[o.TransactionStatus.ToString()] += 1;
                            }
                        }
                    }
                    else
                    {
                        if (tempTranscationCodes.ContainsKey(o.TransactionStatus.ToString())
                            && DateTime.Parse(o.Transaction_Date).Year == date.Value.Year
                            && DateTime.Parse(o.Transaction_Date).Month == date.Value.Month)
                        {
                            tempTranscationCodes[o.TransactionStatus.ToString()] += 1;
                        }
                    }
                }
            }

            foreach (PaymentGatewayTransactionStatu status in statuses)
            {
                transactionCodes.Add(status.Status, 0);
                transactionCodes[status.Status] = tempTranscationCodes[status.TransactionCode.ToString()];
            }

            if (transactionCodes == null)
            {
                transactionCodes = new Dictionary<string, int>();
            }

            return transactionCodes;
        }

        public Dictionary<string, int> GetPaymentDetails(int? year, DateTime? date, List<PaymentGatewayMessage> messages, List<CashOrder> cashOrders)
        {
            List<PaymentGatewayPaymentMethod> methods = new List<PaymentGatewayPaymentMethod>();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                methods = db.PaymentGatewayPaymentMethods.ToList();
            }

            Dictionary<string, int> tempPaymentMethods = new Dictionary<string, int>();
            Dictionary<string, int> paymentMethods = new Dictionary<string, int>();

            foreach (PaymentGatewayPaymentMethod method in methods)
            {
                tempPaymentMethods.Add(method.PayMethod.ToString(), 0);
            }

            tempPaymentMethods.Add("Cash", 0);

            foreach (PaymentGatewayMessage o in messages)
            {
                if (o.Pay_Method != null && o.Transaction_Date != null)
                {

                    if (date == null)
                    {
                        if (year == 0)
                        {
                            if (tempPaymentMethods.ContainsKey(o.Pay_Method.ToString()))
                            {
                                tempPaymentMethods[o.Pay_Method.ToString()] += 1;
                            }
                        }
                        else
                        {
                            if (tempPaymentMethods.ContainsKey(o.Pay_Method.ToString())
                                && DateTime.Parse(o.Transaction_Date).Year == year)
                            {
                                tempPaymentMethods[o.Pay_Method.ToString()] += 1;
                            }
                        }
                    }
                    else
                    {
                        if (tempPaymentMethods.ContainsKey(o.Pay_Method.ToString())
                            && DateTime.Parse(o.Transaction_Date).Year == date.Value.Year
                            && DateTime.Parse(o.Transaction_Date).Month == date.Value.Month)
                        {
                            tempPaymentMethods[o.Pay_Method.ToString()] += 1;
                        }
                    }
                }
            }

            foreach (CashOrder o in cashOrders)
            {
                if (date == null)
                {
                    if (year == 0)
                    {
                        if (tempPaymentMethods.ContainsKey("Cash"))
                        {
                            tempPaymentMethods["Cash"] += 1;
                        }
                    }
                    else
                    {
                        if (tempPaymentMethods.ContainsKey("Cash")
                            && o.DatePlaced.Value.Year == year)
                        {
                            tempPaymentMethods["Cash"] += 1;
                        }
                    }
                }
                else
                {
                    if (tempPaymentMethods.ContainsKey("Cash")
                            && o.DatePlaced.Value.Year == date.Value.Year
                            && o.DatePlaced.Value.Month == date.Value.Month)
                    {
                        tempPaymentMethods["Cash"] += 1;
                    }
                }
            }

            foreach (PaymentGatewayPaymentMethod method in methods)
            {
                paymentMethods.Add(method.Description, 0);
                paymentMethods[method.Description] = tempPaymentMethods[method.PayMethod.ToString()];
            }

            paymentMethods.Add("Cash", 0);
            paymentMethods["Cash"] = tempPaymentMethods["Cash"];

            if (paymentMethods == null)
            {
                paymentMethods = new Dictionary<string, int>();
            }

            return paymentMethods;
        }

        public SalesInfo(int year)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                List<OrderHeader> orders = new List<OrderHeader>();
                List<CashOrder> cashOrders = new List<CashOrder>();
                List<PaymentGatewayMessage> messages = new List<PaymentGatewayMessage>();

                if (year == 0)
                {
                    messages = db.PaymentGatewayMessages
                        .Where(c => c.TransactionStatus != null)
                        .ToList();

                    TotalSalesGateway = messages
                        .Where(c => c.TransactionStatus == 1)
                        .Sum(c => c.Amount) ?? 0;

                    TotalSalesGateway = TotalSalesGateway / 100;

                    orders = db.OrderHeaders
                        .Where(c => (c.OrderStatus == "Confirmed" || c.OrderStatus == "Completed"))
                        .ToList();

                    cashOrders = db.CashOrders
                        .Where(c => (c.Status == "Completed"))
                        .ToList();

                    List<OrderHeader> tempCompleted = db.OrderHeaders.Where(c => c.OrderStatus == "Completed").ToList();
                    TotalCompletedOrders = tempCompleted.Count;

                    List<OrderHeader> tempConfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed").ToList();
                    TotalConfirmedOrders = tempConfirmed.Count;

                    List<OrderHeader> tempCancelled = db.OrderHeaders.Where(c => c.OrderStatus == "Cancelled").ToList();
                    TotalCancelledOrders = tempCancelled.Count;

                    List<OrderHeader> tempLocked = db.OrderHeaders.Where(c => c.OrderStatus == "Locked").ToList();
                    TotalLockedOrders = tempLocked.Count;

                    List<OrderHeader> tempUnconfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Unconfirmed").ToList();
                    TotalUnconfirmedOrders = tempUnconfirmed.Count;

                    List<OrderHeader> tempRefunded = db.OrderHeaders.Where(c => c.OrderStatus == "Refunded").ToList();
                    TotalRefundedOrders = tempRefunded.Count;

                    List<OrderHeader> tempInvoiced = db.OrderHeaders.Where(c => c.OrderStatus == "Invoiced").ToList();
                    TotalInvoicedOrders = tempInvoiced.Count;
                }
                else
                {
                    try
                    {
                        orders = db.OrderHeaders
                           .Where(c => (c.OrderStatus == "Confirmed" || c.OrderStatus == "Completed")
                           && c.OrderDatePlaced.Value.Year == year)
                           .ToList();

                        cashOrders = db.CashOrders
                            .Where(c => (c.Status == "Completed"))
                            .ToList();

                        messages = db.PaymentGatewayMessages
                            .Where(c => c.TransactionStatus != null && c.Transaction_Date != null)
                            .ToList();

                        TotalSalesGateway = messages
                            .Where(c => c.TransactionStatus == 1 && DateTime.Parse(c.Transaction_Date).Year == year)
                            .Sum(c => c.Amount) ?? 0;

                        List<OrderHeader> tempCompleted = db.OrderHeaders.Where(c => c.OrderStatus == "Completed" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalCompletedOrders = tempCompleted.Count;

                        List<OrderHeader> tempConfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalConfirmedOrders = tempConfirmed.Count;

                        List<OrderHeader> tempCancelled = db.OrderHeaders.Where(c => c.OrderStatus == "Cancelled" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalCancelledOrders = tempCancelled.Count;

                        List<OrderHeader> tempLocked = db.OrderHeaders.Where(c => c.OrderStatus == "Locked" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalLockedOrders = tempLocked.Count;

                        List<OrderHeader> tempUnconfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Unconfirmed" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalUnconfirmedOrders = tempUnconfirmed.Count;

                        List<OrderHeader> tempRefunded = db.OrderHeaders.Where(c => c.OrderStatus == "Refunded" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalRefundedOrders = tempRefunded.Count;

                        List<OrderHeader> tempInvoiced = db.OrderHeaders.Where(c => c.OrderStatus == "Invoiced" && c.OrderDatePlaced.Value.Year == year).ToList();
                        TotalInvoicedOrders = tempInvoiced.Count;

                        TotalSalesGateway = TotalSalesGateway / 100;
                    }
                    catch (Exception e)
                    {
                        ExceptionLogging.LogException(e);
                    }
                }

                TotalSalesOrders = orders.Sum(c => c.TotalOrderValue);

                TotalShippingOrders = (decimal)orders.Sum(c => c.ShippingTotal);

                SalesDetails = CalculateSalesDetails(year, orders, cashOrders);
                TransactionDetails = GetTransactionDetails(year, null, messages);
                PaymentDetails = GetPaymentDetails(year, null, messages, cashOrders);

                CalculateNumberOfItems(year);
            }
        }

        private void CalculateNumberOfItems(int year)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                NumberOrdersSingleItem = 0;
                NumberOrdersTwoToFive = 0;
                NumberOrdersGreaterFive = 0;

                List<GetNumberOfItemsStatistic_Result> result = db.GetNumberOfItemsStatistic().ToList();

                foreach (GetNumberOfItemsStatistic_Result res in result)
                {
                    if (year == 0)
                    {
                        if (res.SumItems == 1)
                            NumberOrdersSingleItem++;
                        else if (res.SumItems > 1 && res.SumItems <= 5)
                            NumberOrdersTwoToFive++;
                        else
                            NumberOrdersGreaterFive++;
                    }
                    else
                    {
                        if (res.OrderDatePlaced.HasValue && res.OrderDatePlaced.Value.Year == year)
                        {
                            if (res.SumItems == 1)
                                NumberOrdersSingleItem++;
                            else if (res.SumItems > 1 && res.SumItems <= 5)
                                NumberOrdersTwoToFive++;
                            else
                                NumberOrdersGreaterFive++;
                        }
                    }
                }
            }
        }

        public SalesInfo(DateTime date)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                List<OrderHeader> orders = new List<OrderHeader>();
                List<CashOrder> cashOrders = new List<CashOrder>();
                List<PaymentGatewayMessage> messages = new List<PaymentGatewayMessage>();

                try
                {
                    orders = db.OrderHeaders
                        .Where(c => (c.OrderStatus == "Confirmed" || c.OrderStatus == "Completed")
                        && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month)
                        .ToList();

                    cashOrders = db.CashOrders
                        .Where(c => (c.Status == "Completed"))
                        .ToList();

                    messages = db.PaymentGatewayMessages
                        .Where(c => c.TransactionStatus != null && c.Transaction_Date != null)
                        .ToList();

                    TotalSalesGateway = messages
                        .Where(c => c.TransactionStatus == 1 && DateTime.Parse(c.Transaction_Date).Year == date.Year && DateTime.Parse(c.Transaction_Date).Month == date.Month)
                        .Sum(c => c.Amount) ?? 0;

                    List<OrderHeader> tempCompleted = db.OrderHeaders.Where(c => c.OrderStatus == "Completed" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalCompletedOrders = tempCompleted.Count;

                    List<OrderHeader> tempConfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Confirmed" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalConfirmedOrders = tempConfirmed.Count;

                    List<OrderHeader> tempCancelled = db.OrderHeaders.Where(c => c.OrderStatus == "Cancelled" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalCancelledOrders = tempCancelled.Count;

                    List<OrderHeader> tempLocked = db.OrderHeaders.Where(c => c.OrderStatus == "Locked" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalLockedOrders = tempLocked.Count;

                    List<OrderHeader> tempUnconfirmed = db.OrderHeaders.Where(c => c.OrderStatus == "Unconfirmed" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalUnconfirmedOrders = tempUnconfirmed.Count;

                    List<OrderHeader> tempRefunded = db.OrderHeaders.Where(c => c.OrderStatus == "Refunded" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalRefundedOrders = tempRefunded.Count;

                    List<OrderHeader> tempInvoiced = db.OrderHeaders.Where(c => c.OrderStatus == "Invoiced" && c.OrderDatePlaced.Value.Year == date.Year && c.OrderDatePlaced.Value.Month == date.Month).ToList();
                    TotalInvoicedOrders = tempInvoiced.Count;

                    TotalSalesGateway = TotalSalesGateway / 100;
                }
                catch (Exception e)
                {
                    ExceptionLogging.LogException(e);
                }

                TotalSalesOrders = orders.Sum(c => c.TotalOrderValue);

                TotalShippingOrders = (decimal)orders.Sum(c => c.ShippingTotal);

                SalesDetails = CalculateSalesDetails(date, orders, cashOrders);
                TransactionDetails = GetTransactionDetails(null, date, messages);
                PaymentDetails = GetPaymentDetails(null, date, messages, cashOrders);

                CalculateNumberOfItems(date);
            }
        }

        private void CalculateNumberOfItems(DateTime date)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                NumberOrdersSingleItem = 0;
                NumberOrdersTwoToFive = 0;
                NumberOrdersGreaterFive = 0;

                List<GetNumberOfItemsStatistic_Result> result = db.GetNumberOfItemsStatistic().ToList();

                foreach (GetNumberOfItemsStatistic_Result res in result)
                {
                    if (res.OrderDatePlaced.HasValue &&
                        res.OrderDatePlaced.Value.Year == date.Year &&
                        res.OrderDatePlaced.Value.Month == date.Month)
                    {
                        if (res.SumItems == 1)
                            NumberOrdersSingleItem++;
                        else if (res.SumItems > 1 && res.SumItems <= 5)
                            NumberOrdersTwoToFive++;
                        else
                            NumberOrdersGreaterFive++;
                    }

                }
            }
        }
    }
}