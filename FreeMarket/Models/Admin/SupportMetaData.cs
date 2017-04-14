using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FreeMarket.Models
{
    [MetadataType(typeof(SupportMetaData))]
    public partial class Support
    {
        public ProductCollection ActivatedProducts { get; set; }

        public static Support GetSupport()
        {
            Support support = new Support();

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                support = db.Supports.FirstOrDefault();

                if (support == null)
                    return new Support();

            }

            return support;
        }

        public static void SaveModel(Support support)
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
        }
    }

    public class SupportMetaData
    {
        [Required]
        [StringLength(50)]
        [DisplayName("Land Line")]
        public string Landline { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Cellphone")]
        public string Cellphone { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Email for Orders")]
        public string OrdersEmail { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Email for Information")]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Main Contact Name")]
        public string MainContactName { get; set; }

        [Required]
        [StringLength(200)]
        [DisplayName("Street Address")]
        public string StreetAddress { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Province")]
        public string Province { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        [StringLength(200)]
        [DisplayName("Town Name")]
        public string TownName { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Latitude")]
        public string Latitude { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Longitude")]
        public string Longitude { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Store Name")]
        public string StoreName { get; set; }

        [Required]
        [DisplayName("Default Google Map Zoom Level")]
        public int DefaultZoomLevel { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Fax Number")]
        public string Fax { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Manager Email 1")]
        public string ManagingEmail1 { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Manager Email 2")]
        public string ManagingEmail2 { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Accounts Person Email")]
        public string AccountsEmail { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Manager Name 1")]
        public string Manager1 { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Manager Name 2")]
        public string Manager2 { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Accounts Person Name")]
        public string AccountsPerson { get; set; }

        [Required]
        [StringLength(700)]
        [DisplayName("Physical Address")]
        public string PhysicalAddress { get; set; }

        [Required]
        [StringLength(700)]
        [DisplayName("Postal Address")]
        public string PostalAddress { get; set; }
    }
}