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
    
    public partial class ValidatePaymentAmount_Result
    {
        public int MessageNumber { get; set; }
        public string Reference { get; set; }
        public string Pay_Request_ID { get; set; }
        public Nullable<decimal> PayGate_ID { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string Currency { get; set; }
        public string ReturnUrl { get; set; }
        public string Transaction_Date { get; set; }
        public string Locale { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string Pay_Method { get; set; }
        public string Pay_Method_Detail { get; set; }
        public string Notify_Url { get; set; }
        public string User1 { get; set; }
        public string User2 { get; set; }
        public string User3 { get; set; }
        public Nullable<bool> Vault { get; set; }
        public string Vault_ID { get; set; }
        public Nullable<decimal> TransactionStatus { get; set; }
        public Nullable<decimal> Result_Code { get; set; }
        public string Auth_Code { get; set; }
        public string Result_Desc { get; set; }
        public Nullable<decimal> Transaction_ID { get; set; }
        public string Risk_Indicator { get; set; }
        public string Pay_Vault_Data1 { get; set; }
        public string Pay_Vault_Data2 { get; set; }
        public Nullable<bool> Checksum_Passed { get; set; }
        public Nullable<bool> PriceSameAsRequest { get; set; }
    }
}
