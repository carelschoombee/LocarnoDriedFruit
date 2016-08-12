//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FreeMarket.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class OrderDetail
    {
        public int ItemNumber { get; set; }
        public int OrderNumber { get; set; }
        public int SupplierNumber { get; set; }
        public int ProductNumber { get; set; }
        public Nullable<int> CourierNumber { get; set; }
        public Nullable<bool> Settled { get; set; }
        public Nullable<bool> PaySupplier { get; set; }
        public Nullable<bool> PayCourier { get; set; }
        public Nullable<bool> PaidSupplier { get; set; }
        public Nullable<bool> PaidCourier { get; set; }
        public string OrderItemStatus { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal OrderItemValue { get; set; }
        public Nullable<System.DateTime> DeliveryDateAgreed { get; set; }
        public Nullable<System.DateTime> DeliveryDateActual { get; set; }
        public Nullable<decimal> CourierFee { get; set; }
        public string CustomerProductQualityRating { get; set; }
        public string CustomerCourierOnTimeDeliveryRating { get; set; }
    
        public virtual Courier Courier { get; set; }
        public virtual Product Product { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual OrderHeader OrderHeader { get; set; }
    }
}
