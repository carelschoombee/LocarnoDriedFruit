using System.Configuration;
using FreeMarket.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreeMarket.UnitTests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void PaymentGatewayCall()
        {
            string reference = "1";
            decimal amount = 1;
            string email = ConfigurationManager.AppSettings["developerIdentity"];
            PaymentGatewayIntegration payObject = new PaymentGatewayIntegration(reference, amount, email);

            payObject.Execute();

            Assert.IsTrue(!(string.IsNullOrEmpty(payObject.Pay_Request_Id)));
            Assert.IsTrue(!(string.IsNullOrEmpty(payObject.Checksum)));
        }
    }
}
