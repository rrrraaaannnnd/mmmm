
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class AppointmentSlotsController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppointmentSlotsController> _logger;

    public AppointmentSlotsController(
        IConfiguration configuration,
        ILogger<AppointmentSlotsController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int doctorId,
        [FromQuery] DateTime date)
    {
      if (doctorId <= 0 || date == default)
      {
        return BadRequest(new
        {
          message = "Valid doctorId and date are required."
        });
      }

      if (date.Date < DateTime.Today)
      {
        return BadRequest(new
        {
          message = "Cannot search for past dates."
        });
      }

      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
          throw new InvalidOperationException(
              "Oracle connection string is missing.");
        }

        var slots = new List<object>();

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.BindByName = true;

        command.CommandText = @"
                    SELECT
                        s.SLOT_ID,
                        s.SLOT_DATE,
                        s.START_TIME,
                        s.END_TIME
                    FROM APPOINTMENT_SLOTS s
                    JOIN DOCTOR_SCHEDULES ds
                        ON s.SCHEDULE_ID = ds.SCHEDULE_ID
                    JOIN DOCTORS d
                        ON ds.DOCTOR_ID = d.DOCTOR_ID
                    WHERE ds.DOCTOR_ID = :doctorId
                      AND TRUNC(s.SLOT_DATE) = :slotDate
                      AND s.IS_ACTIVE = 1
                      AND ds.IS_ACTIVE = 1
                      AND d.IS_ACTIVE = 1
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM APPOINTMENTS a
                          WHERE a.SLOT_ID = s.SLOT_ID
                            AND UPPER(a.STATUS) <> 'CANCELLED'
                      )
                    ORDER BY s.START_TIME";

        command.Parameters.Add(
            "doctorId", OracleDbType.Int32).Value = doctorId;

        command.Parameters.Add(
            "slotDate", OracleDbType.Date).Value = date.Date;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          slots.Add(new
          {
            slotId = Convert.ToInt32(reader.GetDecimal(0)),
            date = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            startTime = reader.GetString(2),
            endTime = reader.GetString(3)
          });
        }

        return Ok(slots);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving available slots");

        return StatusCode(500, new
        {
          message = "Unable to load available slots."
        });
      }
    }
  }
}
