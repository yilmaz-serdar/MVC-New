using System.ComponentModel.DataAnnotations;

namespace DependencyAnalyzer.Mvc.Models;

public sealed class AnalysisRequest : IValidatableObject
{
    [Required(ErrorMessage = "Lütfen servis adını girin.")]
    [StringLength(100, ErrorMessage = "Servis adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Servis Adı")]
    public string ServiceName { get; set; } = string.Empty;

    [Display(Name = "Analiz Süresi")]
    public int Days { get; set; }

    [Required(ErrorMessage = "Lütfen sonuçların gönderileceği e-posta adresini girin.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(254)]
    [Display(Name = "Sonuç E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Email) && (Email.Any(character => char.IsWhiteSpace(character) || char.IsControl(character))
            || !System.Net.Mail.MailAddress.TryCreate(Email, out var address)
            || !string.Equals(address.Address, Email, StringComparison.OrdinalIgnoreCase)))
            yield return new ValidationResult("Geçerli bir e-posta adresi girin.", new[] { nameof(Email) });
        if (Days != 1 && Days != 3 && Days != 7)
            yield return new ValidationResult("Son 1, 3 veya 7 gün seçilmelidir.", new[] { nameof(Days) });
    }
}
