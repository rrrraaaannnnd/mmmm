
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MediBook.Api.Models;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class AppointmentsController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IConfiguration configuration,
        ILogger<AppointmentsController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    // 1. BOOK APPOINTMENT
    [HttpPost]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentRequest request)
    {
      if (request.PatientId <= 0 || request.SlotId <= 0)
      {
        return BadRequest(new
        {
          message = "Invalid patient or slot ID."
        });
      }

      try
      {

        using var connection = CreateConnection();
        await connection.OpenAsync();

        // Check slot availability and get doctor's fee
        using var checkCommand = connection.CreateCommand();

        checkCommand.CommandText = @"
                    SELECT d.CONSULTATION_FEE
                    FROM APPOINTMENT_SLOTS s
                    JOIN DOCTOR_SCHEDULES ds
                        ON s.SCHEDULE_ID = ds.SCHEDULE_ID
                    JOIN DOCTORS d
                        ON ds.DOCTOR_ID = d.DOCTOR_ID
                    WHERE s.SLOT_ID = :slotId
                      AND s.IS_ACTIVE = 1
                      AND ds.IS_ACTIVE = 1
                      AND d.IS_ACTIVE = 1";

        checkCommand.BindByName = true;

        checkCommand.Parameters.Add(
            "slotId", OracleDbType.Int32).Value = request.SlotId;

        var feeResult = await checkCommand.ExecuteScalarAsync();

        if (feeResult == null || feeResult == DBNull.Value)
        {
          return NotFound(new
          {
            message = "Appointment slot not found or inactive."
          });
        }

        decimal consultationFee = Convert.ToDecimal(feeResult);

        // Check duplicate booking
        using var duplicateCommand = connection.CreateCommand();

        duplicateCommand.CommandText = @"
                    SELECT COUNT(*)
                    FROM APPOINTMENTS
                    WHERE SLOT_ID = :slotId
                      AND UPPER(STATUS) <> 'CANCELLED'";

        duplicateCommand.BindByName = true;

        duplicateCommand.Parameters.Add(
            "slotId", OracleDbType.Int32).Value = request.SlotId;

        int existingBookings = Convert.ToInt32(
            await duplicateCommand.ExecuteScalarAsync());

        if (existingBookings > 0)
        {
          return Conflict(new
          {
            message = "This appointment slot is already booked."
          });
        }

        // Generate reference number
        string referenceNumber =
            "APT-" + Guid.NewGuid().ToString("N")[..20];

        // Insert appointment
        using var insertCommand = connection.CreateCommand();

        insertCommand.CommandText = @"
                    INSERT INTO APPOINTMENTS
                    (
                        PATIENT_ID,
                        SLOT_ID,
                        REASON_FOR_VISIT,
                        CONSULTATION_FEE,
                        STATUS,
                        REFERENCE_NUMBER
                    )
                    VALUES
                    (
                        :patientId,
                        :slotId,
                        :reason,
                        :fee,
                        'Pending',
                        :referenceNumber
                    )";

        insertCommand.BindByName = true;

        insertCommand.Parameters.Add(
            "patientId", OracleDbType.Int32).Value =
            request.PatientId;

        insertCommand.Parameters.Add(
            "slotId", OracleDbType.Int32).Value =
            request.SlotId;

        insertCommand.Parameters.Add(
            "reason", OracleDbType.Varchar2, 300).Value =
            string.IsNullOrWhiteSpace(request.ReasonForVisit)
                ? DBNull.Value
                : request.ReasonForVisit.Trim();

        insertCommand.Parameters.Add(
            "fee", OracleDbType.Decimal).Value = consultationFee;

        insertCommand.Parameters.Add(
            "referenceNumber", OracleDbType.Varchar2, 30).Value =
            referenceNumber;

        await insertCommand.ExecuteNonQueryAsync();

        return Ok(new
        {
          message = "Appointment booked successfully.",
          referenceNumber,
          status = "Pending",
          consultationFee
        });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        // Unique index prevents simultaneous duplicate bookings
        return Conflict(new
        {
          message = "This appointment slot is already booked."
        });
      }
      catch (OracleException ex) when (ex.Number == 2291)
      {
        return BadRequest(new
        {
          message = "Patient or appointment slot does not exist."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error booking appointment");

        return StatusCode(500, new
        {
          message = "Unable to book appointment."
        });
      }
    }

    // 2. GET PATIENT APPOINTMENTS
    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetPatientAppointments(int patientId)
    {
      if (patientId <= 0)
      {
        return BadRequest(new
        {
          message = "Invalid patient ID."
        });
      }

      try
      {
        var appointments = new List<AppointmentDto>();

        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        a.APPOINTMENT_ID,
                        a.REFERENCE_NUMBER,
                        a.REASON_FOR_VISIT,
                        a.CONSULTATION_FEE,
                        a.STATUS,
                        s.SLOT_DATE,
                        s.START_TIME,
                        s.END_TIME,
                        d.FULL_NAME,
                        sp.SPECIALTY_NAME
                    FROM APPOINTMENTS a
                    JOIN APPOINTMENT_SLOTS s
                        ON a.SLOT_ID = s.SLOT_ID
                    JOIN DOCTOR_SCHEDULES ds
                        ON s.SCHEDULE_ID = ds.SCHEDULE_ID
                    JOIN DOCTORS d
                        ON ds.DOCTOR_ID = d.DOCTOR_ID
                    JOIN SPECIALTIES sp
                        ON d.SPECIALTY_ID = sp.SPECIALTY_ID
                    WHERE a.PATIENT_ID = :patientId
                    ORDER BY s.SLOT_DATE DESC, s.START_TIME DESC";

        command.BindByName = true;

        command.Parameters.Add(
            "patientId", OracleDbType.Int32).Value = patientId;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          appointments.Add(new AppointmentDto(
              Convert.ToInt32(reader.GetDecimal(0)),
              reader.GetString(1),
              reader.IsDBNull(2) ? null : reader.GetString(2),
              reader.GetDecimal(3),
              reader.GetString(4),
              reader.GetDateTime(5),
              reader.GetString(6),
              reader.GetString(7),
              reader.GetString(8),
              reader.GetString(9)
          ));
        }

        return Ok(appointments);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading patient appointments");

        return StatusCode(500, new
        {
          message = "Unable to load appointments."
        });
      }
    }

    // 3. CANCEL APPOINTMENT
    [HttpPut("{appointmentId:int}/cancel")]
    public async Task<IActionResult> CancelAppointment(
        int appointmentId,
        [FromQuery] int patientId)
    {
      if (appointmentId <= 0 || patientId <= 0)
      {
        return BadRequest(new
        {
          message = "Invalid appointment or patient ID."
        });
      }

      try
      {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    UPDATE APPOINTMENTS
                    SET STATUS = 'Cancelled'
                    WHERE APPOINTMENT_ID = :appointmentId
                      AND PATIENT_ID = :patientId
                      AND UPPER(STATUS) IN ('PENDING', 'CONFIRMED')";

        command.BindByName = true;

        command.Parameters.Add(
            "appointmentId", OracleDbType.Int32).Value =
            appointmentId;

        command.Parameters.Add(
            "patientId", OracleDbType.Int32).Value =
            patientId;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
          return NotFound(new
          {
            message = "Appointment not found or cannot be cancelled."
          });
        }

        return Ok(new
        {
          message = "Appointment cancelled successfully."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error cancelling appointment");

        return StatusCode(500, new
        {
          message = "Unable to cancel appointment."
        });
      }
    }

    // ORACLE CONNECTION
    private OracleConnection CreateConnection()
    {
      var connectionString = _configuration
          .GetConnectionString("OracleConnection");

      if (string.IsNullOrWhiteSpace(connectionString))
      {
        throw new InvalidOperationException(
            "Oracle connection string is missing.");
      }

      return new OracleConnection(connectionString);
    }

  }
}
