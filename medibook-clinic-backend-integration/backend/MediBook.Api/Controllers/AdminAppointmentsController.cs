
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin/appointments")]
  public class AdminAppointmentsController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAppointmentsController> _logger;

    public AdminAppointmentsController(
        IConfiguration configuration,
        ILogger<AdminAppointmentsController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        a.APPOINTMENT_ID,
                        a.PATIENT_ID,
                        p.FULL_NAME AS PATIENT_NAME,
                        a.SLOT_ID,
                        d.FULL_NAME AS DOCTOR_NAME,
                        s.SLOT_DATE,
                        s.START_TIME,
                        s.END_TIME,
                        a.REASON_FOR_VISIT,
                        a.CONSULTATION_FEE,
                        a.STATUS,
                        a.REFERENCE_NUMBER,
                        a.CREATED_AT
                    FROM APPOINTMENTS a
                    JOIN PATIENTS p
                        ON a.PATIENT_ID = p.PATIENT_ID
                    JOIN APPOINTMENT_SLOTS s
                        ON a.SLOT_ID = s.SLOT_ID
                    JOIN DOCTOR_SCHEDULES ds
                        ON s.SCHEDULE_ID = ds.SCHEDULE_ID
                    JOIN DOCTORS d
                        ON ds.DOCTOR_ID = d.DOCTOR_ID
                    ORDER BY a.CREATED_AT DESC";

        var appointments = new List<object>();

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          appointments.Add(new
          {
            appointmentId =
                  Convert.ToInt32(reader.GetValue(0)),

            patientId =
                  Convert.ToInt32(reader.GetValue(1)),

            patientName = reader.GetString(2),

            slotId =
                  Convert.ToInt32(reader.GetValue(3)),

            doctorName = reader.GetString(4),

            appointmentDate =
                  Convert.ToDateTime(reader.GetValue(5)),

            startTime = reader.GetString(6),

            endTime = reader.GetString(7),

            reasonForVisit = reader.IsDBNull(8)
                  ? null : reader.GetString(8),

            consultationFee =
                  Convert.ToDecimal(reader.GetValue(9)),

            status = reader.GetString(10),

            referenceNumber = reader.GetString(11),

            createdAt =
                  Convert.ToDateTime(reader.GetValue(12))
          });
        }

        return Ok(appointments);
      }
      catch (Exception ex)
      {
        _logger.LogError(
            ex, "Error loading admin appointments");

        return StatusCode(500, new
        {
          message = "Unable to load appointments."
        });
      }
    }


    public class UpdateAppointmentStatusRequest
    {
      public string Status { get; set; } = string.Empty;
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(
        int id,
        [FromBody] UpdateAppointmentStatusRequest request)
    {
      if (id <= 0 || request == null)
        return BadRequest(new { message = "Invalid request." });

      string newStatus = request.Status?.Trim().ToUpperInvariant() ?? "";

      if (newStatus is not ("CONFIRMED" or "CANCELLED" or "COMPLETED"))
        return BadRequest(new
        {
          message = "Invalid appointment status."
        });

      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        // Allow only valid transitions, atomically.
        using var command = connection.CreateCommand();
        command.BindByName = true;

        command.CommandText = @"
            UPDATE APPOINTMENTS
            SET STATUS = :newStatus
            WHERE APPOINTMENT_ID = :id
              AND (
                    (UPPER(STATUS) = 'PENDING'
                     AND :newStatus IN ('CONFIRMED', 'CANCELLED'))
                 OR (UPPER(STATUS) = 'CONFIRMED'
                     AND :newStatus IN ('COMPLETED', 'CANCELLED'))
              )";

        command.Parameters.Add("newStatus", OracleDbType.Varchar2)
            .Value = newStatus;

        command.Parameters.Add("id", OracleDbType.Int32)
            .Value = id;

        int rows = await command.ExecuteNonQueryAsync();

        if (rows == 0)
        {
          using var checkCommand = connection.CreateCommand();
          checkCommand.BindByName = true;
          checkCommand.CommandText = @"
                SELECT STATUS
                FROM APPOINTMENTS
                WHERE APPOINTMENT_ID = :id";

          checkCommand.Parameters.Add("id", OracleDbType.Int32)
              .Value = id;

          var currentStatus = await checkCommand.ExecuteScalarAsync();

          if (currentStatus == null || currentStatus == DBNull.Value)
            return NotFound(new { message = "Appointment not found." });

          return Conflict(new
          {
            message = "This status transition is not allowed."
          });
        }

        return Ok(new
        {
          appointmentId = id,
          status = newStatus,
          message = "Appointment status updated successfully."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating appointment status");

        return StatusCode(500, new
        {
          message = "Unable to update appointment status."
        });
      }
    }

  }
}

