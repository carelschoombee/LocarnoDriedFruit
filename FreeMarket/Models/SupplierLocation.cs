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
    
    public partial class SupplierLocation
    {
        public int SupplierNumber { get; set; }
        public int LocationNumber { get; set; }
        public string Name { get; set; }
    
        public virtual Location Location { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}