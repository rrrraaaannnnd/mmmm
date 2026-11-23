
using System.ComponentModel.DataAnnotations;

namespace MediBook.Api.Models
{
  public class BookAppointmentRequest
  {
    [Required]
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int SlotId { get; set; }

    [MaxLength(300)]
    public string? ReasonForVisit { get; set; }
  }
}
