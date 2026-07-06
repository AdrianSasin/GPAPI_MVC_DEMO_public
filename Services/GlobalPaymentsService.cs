using GlobalPayments.Api;
using GlobalPayments.Api.Entities;
using GlobalPayments.Api.Entities.Enums;
using GlobalPayments.Api.Services;
using GlobalPayments.Api.Utils;
using GPAPI_MVC_DEMO.Models;
using Microsoft.Extensions.Configuration;
using GlobalPayments.Api.PaymentMethods;

namespace GPAPI_MVC_DEMO.Services;

public class GlobalPaymentsService
{
    private readonly IConfiguration _configuration;

    public GlobalPaymentsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    //HPP LINK
    public string CreatePaymentLink(PaymentRequestModel request)
    {
        ServicesContainer.RemoveConfig();

        var config = new GpApiConfig
        {
            AppId = _configuration["GlobalPayments:AppId"],
            AppKey = _configuration["GlobalPayments:AppKey"],
            Channel = Channel.CardNotPresent,
            Country = "PL",
            ServiceUrl = _configuration["GlobalPayments:ServiceUrl"]
        };

        ServicesContainer.ConfigureService(config);

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Language = request.Language,
            Status = "NEW"
        };

        var billingAddress = new Address
        {
            StreetAddress1 = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = "PL"
        };

        var payByLink = new PayByLinkData
        {
            Type = PayByLinkType.HOSTED_PAYMENT_PAGE,
            UsageMode = PaymentMethodUsageMode.Single,
            
            AllowedPaymentMethods = new[]
            {
                PaymentMethodName.Card,
                //PaymentMethodName.DigitalWallet
                //PaymentMethodName.BLIK,
                //PaymentMethodName.BankPayment
            },
           
            UsageLimit = 1,
            Name = "Demo payment link",
            ReturnUrl = "https://example.com/return",
            CancelUrl = "https://example.com/cancel",
            StatusUpdateUrl = "https://example.com/status",
            Configuration = new PaymentMethodConfiguration
            {
                IsBillingAddressRequired = true,
                StorageMode = StorageMode.OFF
            }
        };

        var response = PayByLinkService.Create(payByLink, request.Amount)
            .WithCurrency("PLN")
            .WithClientTransactionId(Guid.NewGuid().ToString("N"))
            .WithAddress(billingAddress, AddressType.Billing)
            .WithCustomerData(customer)
            .WithDescription("ASP.NET MVC demo payment link")
            .Execute();

        if (response.PayByLinkResponse?.Url == null)
        {
            throw new Exception("Brak URL płatności. Response: " + response.ResponseMessage);
        }

        return response.PayByLinkResponse.Url;
    }

    // BLIK via API
    public string CreateBlikPayment(PaymentRequestModel request)
    {
        ServicesContainer.RemoveConfig();

        var config = new GpApiConfig
        {
            AppId = _configuration["GlobalPayments:AppId"],
            AppKey = _configuration["GlobalPayments:AppKey"],
            Channel = Channel.CardNotPresent,
            Country = "PL",
            ServiceUrl = _configuration["GlobalPayments:ServiceUrl"]
        };

        ServicesContainer.ConfigureService(config, "blikConfig");

        var paymentMethodDetails = new AlternativePaymentMethod
        {
            AlternativePaymentMethodType = AlternativePaymentType.BLIK,
            ReturnUrl = "https://example.com/return",
            StatusUpdateUrl = "https://example.com/status",
            Descriptor = "Demo BLIK transaction",
            Country = "PL",
            AccountHolderName = $"{request.FirstName} {request.LastName}"
        };

        var response = paymentMethodDetails.Charge(request.Amount)
            .WithCurrency("PLN")
            .WithDescription("ASP.NET MVC BLIK payment")
            .Execute("blikConfig");

        if (response.AlternativePaymentResponse?.RedirectUrl == null)
        {
            throw new Exception("Brak RedirectUrl dla BLIK. Response: " + response.ResponseMessage);
        }

        return response.AlternativePaymentResponse.RedirectUrl;
    }
}