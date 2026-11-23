
namespace MediBook.Api.Models;

public class Schedule
{
  public int Id { get; set; }
  public int DoctorId { get; set; }
  public string DayOfWeek { get; set; } = string.Empty;
  public string StartTime { get; set; } = string.Empty;
  public string EndTime { get; set; } = string.Empty;
  public int SlotDuration { get; set; } = 30;
  public int IsActive { get; set; } = 1;
}

