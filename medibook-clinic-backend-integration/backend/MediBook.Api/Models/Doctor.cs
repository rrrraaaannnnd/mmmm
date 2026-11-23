
namespace MediBook.Api.Models;

public class Doctor
{
  public int Id { get; set; }
  public int SpecialtyId { get; set; }
  public string FullName { get; set; } = string.Empty;
  public string? PhotoUrl { get; set; }
  public string? Qualifications { get; set; }
  public int? ExperienceYears { get; set; }
  public decimal? ConsultationFee { get; set; }
  public string? Bio { get; set; }
  public int IsActive { get; set; } = 1;
}

