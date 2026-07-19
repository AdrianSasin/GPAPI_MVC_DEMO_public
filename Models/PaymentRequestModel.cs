using System.ComponentModel.DataAnnotations;

namespace GPAPI_MVC_DEMO.Models;

public class PaymentRequestModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    public string FirstName { get; set; } = "Jan";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    public string LastName { get; set; } = "Kowalski";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = "jan.kowalski@example.com";

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(100)]
    public string Address { get; set; } = "Testowa 1";

    [Required(ErrorMessage = "City is required.")]
    [StringLength(50)]
    public string City { get; set; } = "Warszawa";

    [Required(ErrorMessage = "Postal code is required.")]
    [StringLength(10)]
    public string PostalCode { get; set; } = "00-001";

    [Range(
    typeof(decimal),
    "0.01",
    "1000000",
    ErrorMessage = "Amount must be greater than 0.",
    ParseLimitsInInvariantCulture = true,
    ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; } = 10m;

    [Required]
    [StringLength(2)]
    public string Language { get; set; } = "PL";
}