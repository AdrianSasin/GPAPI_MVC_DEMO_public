using GlobalPayments.Api;
using GlobalPayments.Api.Entities;
using GlobalPayments.Api.Entities.Enums;
using GlobalPayments.Api.PaymentMethods;
using GlobalPayments.Api.Services;
using GlobalPayments.Api.Utils;
using GPAPI_MVC_DEMO.Models;
using Microsoft.Extensions.Configuration;

namespace GPAPI_MVC_DEMO.Services;

public class GlobalPaymentsService
{
    private const string ApmConfigName = "apmConfig";

    private readonly IConfiguration _configuration;

    public GlobalPaymentsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // =========================================================
    // HPP / PAY BY LINK
    // =========================================================

    public string CreatePaymentLink(PaymentRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ServicesContainer.RemoveConfig();

        var config = CreateGpApiConfig();

        ServicesContainer.ConfigureService(config);

        var callbackUrls = GetCallbackUrls();

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
                PaymentMethodName.Card

                // Możesz później włączyć kolejne metody:
                // PaymentMethodName.DigitalWallet,
                // PaymentMethodName.BLIK,
                // PaymentMethodName.BankPayment
            },

            UsageLimit = 1,
            Name = "Demo payment link",

            ReturnUrl = callbackUrls.ReturnUrl,
            CancelUrl = callbackUrls.CancelUrl,
            StatusUpdateUrl = callbackUrls.StatusUrl,

            Configuration = new PaymentMethodConfiguration
            {
                IsBillingAddressRequired = true,
                StorageMode = StorageMode.OFF
            }
        };

        var response = PayByLinkService
            .Create(payByLink, request.Amount)
            .WithCurrency("PLN")
            .WithClientTransactionId(CreateTransactionReference())
            .WithAddress(billingAddress, AddressType.Billing)
            .WithCustomerData(customer)
            .WithDescription("ASP.NET MVC demo payment link")
            .Execute();

        var paymentUrl = response.PayByLinkResponse?.Url;

        if (string.IsNullOrWhiteSpace(paymentUrl))
        {
            throw new InvalidOperationException(
                $"Brak URL płatności HPP. " +
                $"ResponseCode: {response.ResponseCode}, " +
                $"ResponseMessage: {response.ResponseMessage}"
            );
        }

        return paymentUrl;
    }

    // =========================================================
    // eRATY
    // =========================================================

    public string CreateERatyPayment(PaymentRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ServicesContainer.RemoveConfig();

        var config = CreateGpApiConfig();

        ServicesContainer.ConfigureService(config, ApmConfigName);

        var callbackUrls = GetCallbackUrls();

        var paymentMethod = new AlternativePaymentMethod
        {
            AlternativePaymentMethodType = AlternativePaymentType.ERATY,

            ReturnUrl = callbackUrls.ReturnUrl,
            CancelUrl = callbackUrls.CancelUrl,
            StatusUpdateUrl = callbackUrls.StatusUrl,

            Descriptor = "Demo eRaty transaction",
            Country = "PL",
            AccountHolderName = GetAccountHolderName(request),

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
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        var response = paymentMethod
            .Charge(request.Amount)
            .WithCurrency("PLN")
            .WithDescription("ASP.NET MVC eRaty payment")
            .WithClientTransactionId(CreateTransactionReference())
            .WithCustomerData(customer)
            .Execute(ApmConfigName);

        return GetAlternativePaymentRedirectUrl(
            response,
            "eRaty"
        );
    }

    // =========================================================
    // ALTERNATIVE PAYMENT METHODS
    // =========================================================

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
        ArgumentNullException.ThrowIfNull(request);

        ServicesContainer.RemoveConfig();

        var config = CreateGpApiConfig();

        ServicesContainer.ConfigureService(config, ApmConfigName);

        var callbackUrls = GetCallbackUrls();

        var paymentMethod = new AlternativePaymentMethod
        {
            AlternativePaymentMethodType = paymentType,

            ReturnUrl = callbackUrls.ReturnUrl,
            CancelUrl = callbackUrls.CancelUrl,
            StatusUpdateUrl = callbackUrls.StatusUrl,

            Descriptor = $"Demo {paymentName} transaction",
            Country = "PL",
            AccountHolderName = GetAccountHolderName(request)
        };

        var response = paymentMethod
            .Charge(request.Amount)
            .WithCurrency("PLN")
            .WithDescription($"ASP.NET MVC {paymentName} payment")
            .WithClientTransactionId(CreateTransactionReference())
            .Execute(ApmConfigName);

        return GetAlternativePaymentRedirectUrl(
            response,
            paymentName
        );
    }

    // =========================================================
    // CALLBACK URLS
    // =========================================================

    private PaymentCallbackUrls GetCallbackUrls()
    {
        var baseUrl = GetBaseUrl();

        return new PaymentCallbackUrls(
            ReturnUrl: $"{baseUrl}/Payment/Return",
            CancelUrl: $"{baseUrl}/Payment/Cancel",
            StatusUrl: $"{baseUrl}/Payment/Status"
        );
    }

    private string GetBaseUrl()
    {
        var baseUrl = GetRequiredConfigValue(
            "PaymentUrls:BaseUrl"
        );

        return baseUrl.TrimEnd('/');
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    private GpApiConfig CreateGpApiConfig()
    {
        return new GpApiConfig
        {
            AppId = GetRequiredConfigValue(
                "GlobalPayments:AppId"
            ),

            AppKey = GetRequiredConfigValue(
                "GlobalPayments:AppKey"
            ),

            Channel = Channel.CardNotPresent,
            Country = "PL",

            ServiceUrl = GetRequiredConfigValue(
                "GlobalPayments:ServiceUrl"
            )
        };
    }

    private string GetRequiredConfigValue(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Brak wymaganej konfiguracji: {key}"
            );
        }

        return value;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static string GetAccountHolderName(
        PaymentRequestModel request)
    {
        return $"{request.FirstName} {request.LastName}".Trim();
    }

    private static string CreateTransactionReference()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string GetAlternativePaymentRedirectUrl(
        Transaction response,
        string paymentName)
    {
        var redirectUrl =
            response.AlternativePaymentResponse?.RedirectUrl;

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

    private sealed record PaymentCallbackUrls(
        string ReturnUrl,
        string CancelUrl,
        string StatusUrl
    );
}