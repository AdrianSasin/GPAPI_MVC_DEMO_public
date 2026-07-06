using GPAPI_MVC_DEMO.Models;
using GPAPI_MVC_DEMO.Services;
using Microsoft.AspNetCore.Mvc;

namespace GPAPI_MVC_DEMO.Controllers;

public class PaymentController : Controller
{
    private readonly GlobalPaymentsService _paymentsService;

    public PaymentController(GlobalPaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new PaymentRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(PaymentRequestModel model)
    {
        try
        {
            var paymentUrl = _paymentsService.CreatePaymentLink(model);

            return Redirect(paymentUrl);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View(model);
        }
    }

    // BLIK PO API

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Blik(PaymentRequestModel model)
    {
        try
        {
            var redirectUrl = _paymentsService.CreateBlikPayment(model);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View("Index", model);
        }
    }
}