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
    
    public partial class FilterCustomers_Result
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PreferredCommunicationMethod { get; set; }
        public string SecondaryPhoneNumber { get; set; }
        public string UnConfirmedEmail { get; set; }
        public string DefaultAddress { get; set; }
        public Nullable<bool> UnsubscribeFromAllCorrespondence { get; set; }
        public Nullable<bool> UnsubscribeFromRatings { get; set; }
        public Nullable<System.DateTime> LastVisited { get; set; }
        public string Email { get; set; }
        public Nullable<bool> EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string PhoneNumber { get; set; }
        public Nullable<bool> PhoneNumberConfirmed { get; set; }
        public Nullable<bool> TwoFactorEnabled { get; set; }
        public Nullable<System.DateTime> LockoutEndDateUtc { get; set; }
        public Nullable<bool> LockoutEnabled { get; set; }
        public Nullable<int> AccessFailedCount { get; set; }
        public string UserName { get; set; }
    }
}