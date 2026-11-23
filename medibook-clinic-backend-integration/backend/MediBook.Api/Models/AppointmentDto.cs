
namespace MediBook.Api.Models
{
  public record AppointmentDto(
      int AppointmentId,
      string ReferenceNumber,
      string? ReasonForVisit,
      decimal ConsultationFee,
      string Status,
      DateTime AppointmentDate,
      string StartTime,
      string EndTime,
      string DoctorName,
      string Specialty
  );
}

