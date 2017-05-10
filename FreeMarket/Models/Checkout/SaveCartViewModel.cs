﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Models
{
    public class SaveCartViewModel
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public Nullable<DateTime> prefDeliveryDateTime { get; set; }

        [DisplayName("Delivery Address")]
        public int SelectedAddress { get; set; }

        public List<SelectListItem> AddressNameOptions { get; set; }

        public string AddressName { get; set; }
        public CustomerAddress Address { get; set; }

        public DeliveryType DeliveryOptions { get; set; }

        public int DaysToAddToMinDate { get; set; }

        public string OrderStatus { get; set; }

        public string TextBlock1 { get; set; }

        public SaveCartViewModel() { }

        public SaveCartViewModel(string customerNumber, OrderHeader order, decimal courierFee, decimal postalFee)
        {
            SetModel(customerNumber, order, courierFee, postalFee);
        }

        public void SetModel(string customerNumber, OrderHeader order, decimal courierFee, decimal postalFee)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                OrderStatus = order.OrderStatus;

                SetDeliveryOptions(order, courierFee, postalFee);

                List<CustomerAddress> addresses = db.CustomerAddresses
                    .Where(c => c.CustomerNumber == customerNumber)
                    .ToList();

                CustomerAddress address = addresses
                    .Where(c => c.ToString() == order.DeliveryAddress)
                    .FirstOrDefault();

                int selectedAddressNumber = 0;
                string addressName = "";

                if (address == null)
                {
                    selectedAddressNumber = 0;
                    addressName = "Current";
                }
                else
                {
                    selectedAddressNumber = address.AddressNumber;
                    addressName = address.AddressName;
                }

                SetAddressNameOptions(customerNumber, selectedAddressNumber);

                if (address == null)
                {
                    AddressNameOptions.Add(new SelectListItem { Text = "Current", Value = "0", Selected = true });
                    Address = new CustomerAddress
                    {
                        AddressCity = order.DeliveryAddressCity,
                        AddressLine1 = order.DeliveryAddressLine1,
                        AddressLine2 = order.DeliveryAddressLine2,
                        AddressLine3 = order.DeliveryAddressLine3,
                        AddressLine4 = order.DeliveryAddressLine3,
                        AddressName = "Current",
                        AddressNumber = 0,
                        AddressPostalCode = order.DeliveryAddressPostalCode,
                        AddressSuburb = order.DeliveryAddressSuburb,
                        CustomerNumber = customerNumber
                    };
                }
                else
                {
                    Address = address;
                }

                if (order.DeliveryDate == null)
                {
                    prefDeliveryDateTime = OrderHeader.GetSpecialSuggestedDeliveryTime();
                }
                else
                {
                    if (order.DeliveryDate < OrderHeader.GetSpecialSuggestedDeliveryTime())
                    {
                        prefDeliveryDateTime = OrderHeader.GetSpecialSuggestedDeliveryTime();
                    }
                    else
                    {
                        prefDeliveryDateTime = order.DeliveryDate;
                    }
                }

                AddressName = AddressNameOptions
                    .Where(c => c.Selected == true)
                    .Select(c => c.Text)
                    .FirstOrDefault();

                SelectedAddress = selectedAddressNumber;

                DaysToAddToMinDate = OrderHeader.GetDaysToMinDate();

                SetTextBlocks();
            }
        }

        public void SetTextBlocks()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                TextBlock1 = db.SiteConfigurations.Where(c => c.Key == "CheckoutDetailsTextBlock1")
                        .FirstOrDefault()
                        .Value;
            }
        }

        public void SetAddressNameOptions(string customerNumber, int selectedAddressNumber)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                AddressNameOptions = db.CustomerAddresses
                       .Where(c => c.CustomerNumber == customerNumber)
                       .Select
                       (c => new SelectListItem
                       {
                           Text = c.AddressName,
                           Value = c.AddressNumber.ToString(),
                           Selected = (c.AddressNumber == selectedAddressNumber ? true : false)
                       }).ToList();

                AddressName = AddressNameOptions
                   .Where(c => c.Selected == true)
                   .Select(c => c.Text)
                   .FirstOrDefault();
            }
        }

        public void SetDeliveryOptions(OrderHeader order, decimal courierFee, decimal postalFee)
        {
            DeliveryOptions = new DeliveryType()
            {
                SelectedDeliveryType = order.DeliveryType,
                CourierCost = courierFee,
                PostOfficeCost = postalFee
            };
        }
    }
}