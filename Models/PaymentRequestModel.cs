namespace GPAPI_MVC_DEMO.Models;

public class PaymentRequestModel
{
    public string FirstName { get; set; } = "Jan";
    public string LastName { get; set; } = "Kowalski";
    public string Email { get; set; } = "jan.kowalski@example.com";

    public string Address { get; set; } = "Testowa 1";
    public string City { get; set; } = "Warszawa";
    public string PostalCode { get; set; } = "00-001";

    public decimal Amount { get; set; } = 10m;

    public string Language { get; set; } = "PL";
}