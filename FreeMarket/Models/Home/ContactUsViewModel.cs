using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;

namespace FreeMarket.Models
{
    public class ContactUsViewModel
    {
        public Support SupportInfo { get; set; }

        [DisplayName("Your Name")]
        [StringLength(100)]
        public string Name { get; set; }
        [StringLength(100)]
        [EmailAddress]
        [DisplayName("Your Email Address")]
        public string FromEmail { get; set; }
        public string DestinationEmail { get; set; }
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        [DisplayName("Message")]
        public string Message { get; set; }

        public ContactUsViewModel()
        {
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                SupportInfo = db.Supports.FirstOrDefault();
                DestinationEmail = ConfigurationManager.AppSettings["systemEmail"];
            }
        }
    }
}