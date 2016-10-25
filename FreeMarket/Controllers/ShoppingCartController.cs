﻿using FreeMarket.Infrastructure;
using FreeMarket.Model;
using FreeMarket.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FreeMarket.Controllers
{
    [RequireHttps]
    public class ShoppingCartController : Controller
    {
        public const string sessionKey = "cart";
        public const string anonymous = "Anonymous";

        public ShoppingCart GetCartFromSession(string userId)
        {
            userId = userId ?? anonymous;

            ShoppingCart tempCart = null;

            if (Session != null)
                tempCart = (ShoppingCart)Session[sessionKey];

            if (tempCart == null)
            {
                if (userId == anonymous)
                    tempCart = new ShoppingCart();
                else
                    tempCart = new ShoppingCart(userId);

                Session[sessionKey] = tempCart;
            }

            return tempCart;
        }

        public ActionResult Cart()
        {
            string userId = User.Identity.GetUserId();

            ShoppingCart cart = GetCartFromSession(userId);
            ShoppingCartViewModel model = new ShoppingCartViewModel();
            model = new ShoppingCartViewModel() { Cart = cart, ReturnUrl = Url.Action("Index", "Product") };

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult CartTotals(ShoppingCart cart)
        {
            return PartialView("_CartTotals", cart);
        }

        public ActionResult ViewProductModal(int id, int supplierNumber, int quantity)
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart cart = GetCartFromSession(userId);

            ViewProductViewModel model = new ViewProductViewModel(id, supplierNumber, quantity, cart.Order.OrderNumber);
            if (model == null)
                return RedirectToAction("Index", "Product");

            return PartialView("_ViewProductModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(ViewProductViewModel viewModel)
        {
            // Validate
            if (viewModel.ProductNumber == 0 || viewModel.SupplierNumber == 0)
            {
                TempData["errorMessage"] = "Error: We could not add the item to the cart.";
                return JavaScript("window.location = window.location.href;");
            }

            if (ModelState.IsValid)
            {
                string userId = User.Identity.GetUserId();
                bool anonymousUser = (userId == null);
                ShoppingCart cart = GetCartFromSession(userId);

                if (cart.Order.OrderStatus == "Locked")
                {
                    TempData["errorMessage"] = "Your cart is locked because you are in the process of checking out. Open your cart to complete or cancel the checkout process.";
                    return JavaScript("window.location = window.location.href;");
                }

                // CheckQuantity
                if (viewModel.CustodianQuantityOnHand < viewModel.Quantity)
                {
                    viewModel.SetInstances(viewModel.ProductNumber, viewModel.SupplierNumber);

                    return PartialView("_CourierSelectionModal", viewModel);
                }

                FreeMarketObject result;
                result = cart.AddItemFromProduct(viewModel.ProductNumber, viewModel.SupplierNumber, viewModel.Quantity, viewModel.CustodianNumber);

                if (result.Result == FreeMarketResult.Success)
                    // New item added
                    if (!string.IsNullOrEmpty(result.Message))
                        TempData["message"] = result.Message;
                    else
                    if (!string.IsNullOrEmpty(result.Message))
                        TempData["errorMessage"] = result.Message;

                return JavaScript("window.location = window.location.href;");
            }
            // Validation Error
            else
            {
                // Prepare
                viewModel.SetInstances(viewModel.ProductNumber, viewModel.SupplierNumber);

                return PartialView("_CourierSelectionModal", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateCart(ShoppingCart cart, string returnUrl)
        {
            // Prepare
            string userId = User.Identity.GetUserId();
            bool anonymousUser = (userId == null);
            ShoppingCart sessionCart = GetCartFromSession(userId);
            ShoppingCartViewModel model;

            if (cart.Order.OrderStatus == "Locked")
            {
                TempData["errorMessage"] = "Your cart is locked because you are in the process of checking out. Complete or cancel your checkout process.";
                return JavaScript("window.location = window.location.href;");
            }

            if (ModelState.IsValid)
            {
                // Prepare
                FreeMarketObject resultRemove = new FreeMarketObject();
                FreeMarketObject resultQuantity = new FreeMarketObject();

                // Update Selected Property
                sessionCart.UpdateSelectedProperty(cart, false);

                // Remove selected Items
                List<OrderDetail> selectedItems = cart.Body.OrderDetails
                    .Where(c => c.Selected || c.Quantity <= 0)
                    .ToList();

                if (selectedItems.Count > 0)
                {
                    foreach (OrderDetail detail in selectedItems)
                    {
                        resultRemove = sessionCart.RemoveItem(detail.ItemNumber, detail.ProductNumber, detail.SupplierNumber, userId);
                    }
                }

                // Update Quantity
                List<OrderDetail> changedItems = cart.Body.OrderDetails
                    .Where(c => !c.Selected && c.Quantity > 0)
                    .ToList();

                if (changedItems.Count > 0)
                    resultQuantity = sessionCart.UpdateQuantities(changedItems);

                sessionCart.Save();
                sessionCart.UpdateSelectedProperty(cart, true);

                if (string.IsNullOrEmpty(resultQuantity.Message))
                    TempData["message"] = "Cart has been updated.";
                else
                    TempData["errorMessage"] = resultQuantity.Message;

                model = new ShoppingCartViewModel { Cart = sessionCart, ReturnUrl = returnUrl };
                return RedirectToAction("Cart", "ShoppingCart");
            }

            model = new ShoppingCartViewModel { Cart = sessionCart, ReturnUrl = returnUrl };

            return View("Cart", model);
        }

        [Authorize]
        public ActionResult SecureDeliveryDetails()
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart sessionCart = GetCartFromSession(userId);

            decimal courierCost = sessionCart.CalculateCourierFee();
            decimal postOfficeCost = sessionCart.CalculatePostalFee();

            SaveCartViewModel model = new SaveCartViewModel(userId, sessionCart.Order, courierCost, postOfficeCost);
            if (model == null)
                return RedirectToAction("Index", "Product");

            return View("CheckoutDeliveryDetails", model);
        }

        [HttpPost]
        public ActionResult GetDeliveryOptions(string id, string selectedDeliveryType)
        {
            SaveCartViewModel model = new SaveCartViewModel();
            DeliveryType options = new DeliveryType();

            //postalcode
            if (!string.IsNullOrEmpty(id) && id.Length == 4)
            {
                try
                {
                    int.Parse(id);
                }
                catch (Exception e)
                {
                    return null;
                }

                string userId = User.Identity.GetUserId();
                ShoppingCart sessionCart = GetCartFromSession(userId);

                decimal courierCost = sessionCart.CalculateCourierFeeAdhoc(int.Parse(id));
                decimal postOfficeCost = sessionCart.CalculatePostalFee();

                options = new DeliveryType()
                {
                    SelectedDeliveryType = selectedDeliveryType,
                    CourierCost = courierCost,
                    PostOfficeCost = postOfficeCost
                };

                model.DeliveryOptions = options;

                return PartialView("_CartModifyDeliveryTypeDetails", model);
            }

            return null;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDeliveryDetails(SaveCartViewModel model)
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart sessionCart = GetCartFromSession(userId);
            FreeMarketObject result;

            TimeSpan startTime = new TimeSpan(8, 0, 0);
            TimeSpan endTime = new TimeSpan(17, 0, 0);


            if (ModelState.IsValid)
            {
                if (!(model.prefDeliveryDateTime.Value.TimeOfDay > startTime && model.prefDeliveryDateTime.Value.TimeOfDay < endTime &&
                    model.prefDeliveryDateTime > DateTime.Now))
                {
                    model.SetAddressNameOptions(userId, model.SelectedAddress);

                    return View("CheckoutDeliveryDetails", model);
                }

                sessionCart.UpdateDeliveryDetails(model);
                result = CustomerAddress.AddOrUpdateAddress(model, userId);

                if (result.Result == FreeMarketResult.Success)
                    TempData["message"] = result.Message;
                else
                    TempData["errorMessage"] = result.Message;

                if (Request.IsAjaxRequest())
                {
                    return JavaScript("window.location.reload();");
                }
                else
                {
                    return RedirectToAction("ConfirmShoppingCart", "ShoppingCart");
                }
            }

            model.SetAddressNameOptions(userId, model.SelectedAddress);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SaveCartModal", model);
            }
            else
            {
                return View("CheckoutDeliveryDetails", model);
            }
        }

        [Authorize]
        public ActionResult ConfirmShoppingCart()
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart sessionCart = GetCartFromSession(userId);

            bool specialDelivery = false;
            int postalCode = 0;

            try
            {
                postalCode = int.Parse(sessionCart.Order.DeliveryAddressPostalCode);
            }
            catch (Exception e)
            {

            }

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (db.ValidateSpecialDeliveryCode(postalCode).First() == 1)
                {
                    specialDelivery = true;
                }
            }

            ConfirmOrderViewModel model = new ConfirmOrderViewModel(sessionCart);
            model.SpecialDelivery = specialDelivery;

            return View("ConfirmShoppingCart", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LockOrder(ConfirmOrderViewModel confirmModel)
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart sessionCart = GetCartFromSession(userId);

            if (ModelState.IsValid)
            {
                sessionCart.Order.OrderStatus = "Locked";
                sessionCart.Save();

                bool specialDelivery = false;
                string reference = sessionCart.Order.OrderNumber.ToString();
                decimal amount = sessionCart.Order.TotalOrderValue * 100;
                ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

                int postalCode = 0;

                try
                {
                    postalCode = int.Parse(sessionCart.Order.DeliveryAddressPostalCode);
                }
                catch (Exception e)
                {

                }

                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    if (db.ValidateSpecialDeliveryCode(postalCode).First() == 1)
                    {
                        specialDelivery = true;
                    }
                }

                PaymentGatewayIntegration payObject = new PaymentGatewayIntegration(reference, amount, user.Email);

                payObject.Execute();

                if (!string.IsNullOrEmpty(payObject.Pay_Request_Id) && !string.IsNullOrEmpty(payObject.Checksum))
                {
                    ConfirmOrderViewModel model = new ConfirmOrderViewModel(sessionCart, payObject.Pay_Request_Id, payObject.Checksum, specialDelivery);
                    model.TermsAndConditions = confirmModel.TermsAndConditions;

                    return View("PayShoppingCart", model);
                }
                else
                {
                    ConfirmOrderViewModel model = new ConfirmOrderViewModel(sessionCart);

                    return View("ConfirmShoppingCart", model);
                }
            }
            else
            {
                ConfirmOrderViewModel model = new ConfirmOrderViewModel(sessionCart);
                model.TermsAndConditions = confirmModel.TermsAndConditions;

                return View("ConfirmShoppingCart", model);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Notify(int PAYGATE_ID, string PAY_REQUEST_ID, string REFERENCE, int TRANSACTION_STATUS,
            int RESULT_CODE, string AUTH_CODE, string CURRENCY, decimal AMOUNT, string RESULT_DESC, int TRANSACTION_ID,
            string RISK_INDICATOR, string PAY_METHOD, string PAY_METHOD_DETAIL, string USER1, string USER2, string USER3,
            string VAULT_ID, string PAYVAULT_DATA_1, string PAYVAULT_DATA_2, string CHECKSUM)
        {
            bool checksumPassed = false;

            PaymentGatewayParameter param = PaymentGatewayIntegration.GetParameters();

            string check = PAYGATE_ID.ToString() + PAY_REQUEST_ID + REFERENCE + TRANSACTION_STATUS.ToString()
                + RESULT_CODE.ToString() + AUTH_CODE + CURRENCY + AMOUNT + RESULT_DESC + TRANSACTION_ID
                + RISK_INDICATOR + PAY_METHOD + PAY_METHOD_DETAIL + USER1 + USER2 + USER3
                + VAULT_ID + PAYVAULT_DATA_1 + PAYVAULT_DATA_2 + param.Key;

            string checksum = Extensions.CreateMD5(check);

            if (CHECKSUM == checksum)
                checksumPassed = true;
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                if (!string.IsNullOrEmpty(REFERENCE))
                {
                    int orderNumber = int.Parse(REFERENCE);

                    PaymentGatewayMessage message = new PaymentGatewayMessage
                    {
                        PayGate_ID = PAYGATE_ID,
                        Pay_Request_ID = PAY_REQUEST_ID,
                        Reference = REFERENCE,
                        TransactionStatus = TRANSACTION_STATUS,
                        Result_Code = RESULT_CODE,
                        Auth_Code = AUTH_CODE,
                        Currency = CURRENCY,
                        Amount = AMOUNT,
                        Result_Desc = RESULT_DESC,
                        Transaction_ID = TRANSACTION_ID,
                        Risk_Indicator = RISK_INDICATOR,
                        Pay_Method = PAY_METHOD,
                        Pay_Method_Detail = PAY_METHOD_DETAIL,
                        User1 = USER1,
                        User2 = USER2,
                        User3 = USER3,
                        Vault_ID = VAULT_ID,
                        Pay_Vault_Data1 = PAYVAULT_DATA_1,
                        Pay_Vault_Data2 = PAYVAULT_DATA_2,
                        Checksum_Passed = checksumPassed
                    };

                    db.PaymentGatewayMessages.Add(message);
                    db.SaveChanges();

                    if (RESULT_CODE == 1)
                    {
                        ShoppingCart.SetOrderConfirmed(orderNumber);

                        string customerNumber = db.OrderHeaders.Where(c => c.OrderNumber == orderNumber)
                        .FirstOrDefault()
                        .CustomerNumber;

                        if (!string.IsNullOrEmpty(customerNumber))
                        {
                            OrderHeader.SendConfirmationEmail(customerNumber, orderNumber);
                        }
                    }
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public ActionResult CancelOrder()
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart cart = GetCartFromSession(userId);
            cart.CancelOrder(userId);

            return RedirectToAction("Index", "Home");
        }

        public ActionResult TransactionComplete(string PAY_REQUEST_ID, int TRANSACTION_STATUS, string CHECKSUM)
        {
            ThankYouViewModel model = new ThankYouViewModel { TransactionStatus = TRANSACTION_STATUS };
            return View("ThankYou", model);
        }

        public ActionResult UpdateCart()
        {
            string userId = User.Identity.GetUserId();

            ShoppingCart cart = GetCartFromSession(userId);
            ShoppingCartViewModel model = new ShoppingCartViewModel();
            model = new ShoppingCartViewModel() { Cart = cart, ReturnUrl = Url.Action("Index", "Product") };

            return View("Cart", model);
        }

        [HttpPost]
        public ActionResult GetAddress(int AddressNumber)
        {
            string toReturn = "";
            CustomerAddress address;

            if (AddressNumber != 0)
            {
                using (FreeMarketEntities db = new FreeMarketEntities())
                {
                    address = db.CustomerAddresses
                       .Where(c => c.AddressNumber == AddressNumber)
                       .FirstOrDefault();

                    if (address != null)
                        toReturn = address
                            .ToString();
                }
            }

            return Content(toReturn);
        }

        public ActionResult GetDeliveryAddress()
        {
            string toReturn = "";

            ShoppingCart cart = GetCartFromSession(User.Identity.GetUserId());
            toReturn = cart.Order.DeliveryAddress.ToString();

            return Content(toReturn);
        }

        [HttpPost]
        public ActionResult GetAddressPartial(int id)
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart sessionCart = GetCartFromSession(userId);
            CustomerAddress address = new CustomerAddress();

            if (id == 0)
            {
                address = new CustomerAddress
                {
                    AddressCity = sessionCart.Order.DeliveryAddressCity,
                    AddressLine1 = sessionCart.Order.DeliveryAddressLine1,
                    AddressLine2 = sessionCart.Order.DeliveryAddressLine2,
                    AddressLine3 = sessionCart.Order.DeliveryAddressLine3,
                    AddressLine4 = sessionCart.Order.DeliveryAddressLine3,
                    AddressName = "Current",
                    AddressNumber = 0,
                    AddressPostalCode = sessionCart.Order.DeliveryAddressPostalCode,
                    AddressSuburb = sessionCart.Order.DeliveryAddressSuburb,
                    CustomerNumber = userId
                };
            }
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                address = db.CustomerAddresses
                    .Where(c => c.CustomerNumber == userId && c.AddressNumber == id)
                    .FirstOrDefault();
            }

            SaveCartViewModel model = new SaveCartViewModel { Address = address, AddressName = address.AddressName };

            return PartialView("_CartModifyDeliveryDetails", model);
        }

        public int PerformStockCheck(int id, int supplierNumber, int quantityRequested)
        {
            int currentStock = 0;

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                currentStock = db.ProductCustodians
                    .Where(c => c.ProductNumber == id && c.SupplierNumber == supplierNumber && c.QuantityOnHand >= quantityRequested)
                    .Select(c => c.QuantityOnHand)
                    .FirstOrDefault();
            }

            return currentStock;
        }

        public ActionResult CheckStock(int id, int supplierNumber, int quantityRequested)
        {
            int currentStock = PerformStockCheck(id, supplierNumber, quantityRequested);

            List<string> response = new List<string>();
            response.Add(currentStock.ToString());

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SmallCartBody()
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart cart = GetCartFromSession(userId);
            ShoppingCartViewModel model = new ShoppingCartViewModel();
            model = new ShoppingCartViewModel() { Cart = cart, ReturnUrl = Url.Action("Index", "Product") };

            return PartialView("_SmallCartBody", model);
        }

        public ActionResult GetItemsCount()
        {
            string userId = User.Identity.GetUserId();
            ShoppingCart cart = GetCartFromSession(userId);

            return Content(cart.Body.OrderDetails.Count().ToString());
        }
    }
}