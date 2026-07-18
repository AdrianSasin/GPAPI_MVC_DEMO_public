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
    //ERATY

    public string CreateERatyPayment(PaymentRequestModel request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ServicesContainer.RemoveConfig();

        var config = CreateGpApiConfig();
        const string configName = "apmConfig";

        ServicesContainer.ConfigureService(config, configName);

        var paymentMethod = new AlternativePaymentMethod
        {
            AlternativePaymentMethodType = AlternativePaymentType.ERATY,
            ReturnUrl = "https://example.com/return",
            StatusUpdateUrl = "https://example.com/status",
            CancelUrl = "https://example.com/cancel",
            Descriptor = "Demo eRaty transaction",
            Country = "PL",
            AccountHolderName = $"{request.FirstName} {request.LastName}",

            Terms = new Terms
            {
                TimeUnit = "MONTH",
                Count = "10",
                Mode = "NO_INTEREST"
            }
        };

        var customer = new Customer
        {
            Key = Guid.NewGuid().ToString("N"),
            Email = request.Email
        };

        var response = paymentMethod
            .Charge(request.Amount)
            .WithCurrency("PLN")
            .WithDescription("ASP.NET MVC eRaty payment")
            .WithClientTransactionId(Guid.NewGuid().ToString("N"))
            .WithCustomerData(customer)
            .Execute(configName);

        var redirectUrl = response.AlternativePaymentResponse?.RedirectUrl;

        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new InvalidOperationException(
                $"Brak RedirectUrl dla eRaty. " +
                $"ResponseCode: {response.ResponseCode}, " +
                $"ResponseMessage: {response.ResponseMessage}"
            );
        }

        return redirectUrl;
    }

    //ALTERNATIVE PAYMENT METHODS

    public string CreateBlikPayment(PaymentRequestModel request)
    {
        return CreateAlternativePayment(
            request,
            AlternativePaymentType.BLIK,
            "BLIK"
        );
    }

    public string CreateBankPayment(PaymentRequestModel request)
    {
        return CreateAlternativePayment(
            request,
            AlternativePaymentType.OB,
            "Bank Payment"
        );
    }


    private string CreateAlternativePayment(
        PaymentRequestModel request,
        AlternativePaymentType paymentType,
        string paymentName)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ServicesContainer.RemoveConfig();

        var config = CreateGpApiConfig();
        const string configName = "apmConfig";

        ServicesContainer.ConfigureService(config, configName);

        var paymentMethod = new AlternativePaymentMethod
        {
            AlternativePaymentMethodType = paymentType,
            ReturnUrl = "https://example.com/return",
            StatusUpdateUrl = "https://example.com/status",
            Descriptor = $"Demo {paymentName} transaction",
            Country = "PL",
            AccountHolderName = $"{request.FirstName} {request.LastName}".Trim()
        };

        var response = paymentMethod
            .Charge(request.Amount)
            .WithCurrency("PLN")
            .WithDescription($"ASP.NET MVC {paymentName} payment")
            .WithClientTransactionId(Guid.NewGuid().ToString("N"))
            .Execute(configName);

        var redirectUrl = response.AlternativePaymentResponse?.RedirectUrl;

        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new InvalidOperationException(
                $"Brak RedirectUrl dla {paymentName}. " +
                $"ResponseCode: {response.ResponseCode}, " +
                $"ResponseMessage: {response.ResponseMessage}"
            );
        }

        return redirectUrl;
    }

    // Helper methods for configuration
    private GpApiConfig CreateGpApiConfig()
    {
        return new GpApiConfig
        {
            AppId = GetRequiredConfigValue("GlobalPayments:AppId"),
            AppKey = GetRequiredConfigValue("GlobalPayments:AppKey"),
            Channel = Channel.CardNotPresent,
            Country = "PL",
            ServiceUrl = GetRequiredConfigValue("GlobalPayments:ServiceUrl")
        };
    }

    private string GetRequiredConfigValue(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException(
                $"Brak wymaganej konfiguracji: {key}"
            );
    }
}