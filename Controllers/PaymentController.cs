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

    // HPP / PAYMENT LINK

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(PaymentRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

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
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

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

    // BANK PAYMENT PO API

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BankPayment(PaymentRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var redirectUrl = _paymentsService.CreateBankPayment(model);

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View("Index", model);
        }
    }

    // eRATY PO API

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ERaty(PaymentRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var redirectUrl = _paymentsService.CreateERatyPayment(model);

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View("Index", model);
        }
    }

    // PAYMENT RETURN URL

    [HttpGet]
    public IActionResult Return()
    {
        return Content("Powrót z płatności.");
    }

    // PAYMENT CANCEL URL

    [HttpGet]
    public IActionResult Cancel()
    {
        return Content("Płatność została anulowana.");
    }

    // PAYMENT STATUS CALLBACK / WEBHOOK

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Status()
    {
        return Ok();
    }
}