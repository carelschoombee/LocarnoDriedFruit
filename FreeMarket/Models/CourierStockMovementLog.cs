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
    
    public partial class CourierStockMovementLog
    {
        public int StockMovementNumber { get; set; }
        public int ProductNumber { get; set; }
        public int CustodianNumber { get; set; }
        public int CourierNumber { get; set; }
        public string CourierEmployeeName { get; set; }
        public Nullable<System.DateTime> DateAddedStock { get; set; }
        public Nullable<System.DateTime> DateSubtractedStock { get; set; }
        public int Quantity { get; set; }
    
        public virtual Courier Courier { get; set; }
        public virtual Custodian Custodian { get; set; }
        public virtual Product Product { get; set; }
    }
}
