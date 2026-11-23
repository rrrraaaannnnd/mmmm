
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MediBook.Api.Models;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin/schedules")]
  public class AdminSchedulesController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSchedulesController> _logger;

    public AdminSchedulesController(
        IConfiguration configuration,
        ILogger<AdminSchedulesController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules()
    {
      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        var schedules = new List<Schedule>();

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        SCHEDULE_ID,
                        DOCTOR_ID,
                        DAY_OF_WEEK,
                        START_TIME,
                        END_TIME,
                        SLOT_DURATION_MINUTES,
                        IS_ACTIVE
                    FROM DOCTOR_SCHEDULES
                    ORDER BY SCHEDULE_ID";

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          schedules.Add(new Schedule
          {
            Id = Convert.ToInt32(reader.GetDecimal(0)),
            DoctorId = Convert.ToInt32(reader.GetDecimal(1)),
            DayOfWeek = reader.GetString(2),
            StartTime = reader.GetString(3),
            EndTime = reader.GetString(4),
            SlotDuration = Convert.ToInt32(
                  reader.GetDecimal(5)),
            IsActive = Convert.ToInt32(
                  reader.GetDecimal(6))
          });
        }

        return Ok(schedules);
      }
      catch (Exception ex)
      {
        _logger.LogError(
            ex, "Error loading admin schedules");

        return StatusCode(500, new
        {
          message = "Unable to load schedules."
        });
      }
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetScheduleById(int id)
    {
      if (id <= 0)
        return BadRequest(new
        {
          message = "Invalid schedule ID."
        });

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
        command.BindByName = true;

        command.CommandText = @"
            SELECT
                SCHEDULE_ID,
                DOCTOR_ID,
                DAY_OF_WEEK,
                START_TIME,
                END_TIME,
                SLOT_DURATION_MINUTES,
                IS_ACTIVE
            FROM DOCTOR_SCHEDULES
            WHERE SCHEDULE_ID = :id";

        command.Parameters.Add(
            "id", OracleDbType.Int32).Value = id;

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
          return NotFound(new
          {
            message = "Schedule not found."
          });

        var schedule = new Schedule
        {
          Id = Convert.ToInt32(reader.GetDecimal(0)),
          DoctorId = Convert.ToInt32(reader.GetDecimal(1)),
          DayOfWeek = reader.GetString(2),
          StartTime = reader.GetString(3),
          EndTime = reader.GetString(4),
          SlotDuration = Convert.ToInt32(
                reader.GetDecimal(5)),
          IsActive = Convert.ToInt32(
                reader.GetDecimal(6))
        };

        return Ok(schedule);
      }
      catch (Exception ex)
      {
        _logger.LogError(
            ex, "Error loading admin schedule");

        return StatusCode(500, new
        {
          message = "Unable to load schedule."
        });
      }
    }

    [HttpPost]
    public async Task<IActionResult> CreateSchedule(
        [FromBody] Schedule request)
    {
      var validDays = new[]
      {
        "SUNDAY", "MONDAY", "TUESDAY",
        "WEDNESDAY", "THURSDAY",
        "FRIDAY", "SATURDAY"
    };

      string day = request.DayOfWeek?.Trim().ToUpperInvariant()
          ?? "";

      bool validStart = TimeSpan.TryParseExact(
          request.StartTime, @"hh\:mm",
          System.Globalization.CultureInfo.InvariantCulture,
          out var start);

      bool validEnd = TimeSpan.TryParseExact(
          request.EndTime, @"hh\:mm",
          System.Globalization.CultureInfo.InvariantCulture,
          out var end);

      if (request.DoctorId <= 0 ||
          !validDays.Contains(day) ||
          !validStart ||
          !validEnd ||
          start >= end ||
          request.SlotDuration <= 0 ||
          request.IsActive is not (0 or 1))
      {
        return BadRequest(new
        {
          message = "Invalid schedule details."
        });
      }

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
        command.BindByName = true;

        command.CommandText = @"
            INSERT INTO DOCTOR_SCHEDULES
                (DOCTOR_ID, DAY_OF_WEEK,
                 START_TIME, END_TIME,
                 SLOT_DURATION_MINUTES, IS_ACTIVE)
            VALUES
                (:doctorId, :dayOfWeek,
                 :startTime, :endTime,
                 :slotDuration, :isActive)
            RETURNING SCHEDULE_ID INTO :newId";

        command.Parameters.Add("doctorId",
            OracleDbType.Int32).Value = request.DoctorId;

        command.Parameters.Add("dayOfWeek",
            OracleDbType.Varchar2).Value = day;

        command.Parameters.Add("startTime",
            OracleDbType.Varchar2).Value =
            request.StartTime;

        command.Parameters.Add("endTime",
            OracleDbType.Varchar2).Value =
            request.EndTime;

        command.Parameters.Add("slotDuration",
            OracleDbType.Int32).Value =
            request.SlotDuration;

        command.Parameters.Add("isActive",
            OracleDbType.Int32).Value =
            request.IsActive;

        var newIdParameter = new OracleParameter(
            "newId", OracleDbType.Decimal)
        {
          Direction =
                System.Data.ParameterDirection.Output
        };

        command.Parameters.Add(newIdParameter);

        await command.ExecuteNonQueryAsync();

        int newId = Convert.ToInt32(
            ((Oracle.ManagedDataAccess.Types.OracleDecimal)
                newIdParameter.Value).Value);

        request.Id = newId;
        request.DayOfWeek = day;

        return CreatedAtAction(
            nameof(GetScheduleById),
            new { id = newId },
            request);
      }
      catch (OracleException ex) when (ex.Number == 2291)
      {
        return BadRequest(new
        {
          message = "Selected doctor does not exist."
        });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        return Conflict(new
        {
          message = "Schedule conflicts with an existing record."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating schedule");

        return StatusCode(500, new
        {
          message = "Unable to create schedule."
        });
      }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSchedule(
        int id, [FromBody] Schedule request)
    {
      string day = request.DayOfWeek?.Trim().ToUpperInvariant() ?? "";

      var validDays = new[]
      {
        "SUNDAY", "MONDAY", "TUESDAY",
        "WEDNESDAY", "THURSDAY",
        "FRIDAY", "SATURDAY"
    };

      bool validStart = TimeSpan.TryParseExact(
          request.StartTime, @"hh\:mm",
          System.Globalization.CultureInfo.InvariantCulture,
          out var start);

      bool validEnd = TimeSpan.TryParseExact(
          request.EndTime, @"hh\:mm",
          System.Globalization.CultureInfo.InvariantCulture,
          out var end);

      if (id <= 0 ||
          request.DoctorId <= 0 ||
          !validDays.Contains(day) ||
          !validStart || !validEnd ||
          start >= end ||
          request.SlotDuration <= 0 ||
          request.IsActive is not (0 or 1))
      {
        return BadRequest(new
        {
          message = "Invalid schedule details."
        });
      }

      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.BindByName = true;

        command.CommandText = @"
            UPDATE DOCTOR_SCHEDULES
            SET DOCTOR_ID = :doctorId,
                DAY_OF_WEEK = :dayOfWeek,
                START_TIME = :startTime,
                END_TIME = :endTime,
                SLOT_DURATION_MINUTES = :slotDuration,
                IS_ACTIVE = :isActive
            WHERE SCHEDULE_ID = :id";

        command.Parameters.Add("doctorId",
            OracleDbType.Int32).Value = request.DoctorId;

        command.Parameters.Add("dayOfWeek",
            OracleDbType.Varchar2).Value = day;

        command.Parameters.Add("startTime",
            OracleDbType.Varchar2).Value = request.StartTime;

        command.Parameters.Add("endTime",
            OracleDbType.Varchar2).Value = request.EndTime;

        command.Parameters.Add("slotDuration",
            OracleDbType.Int32).Value = request.SlotDuration;

        command.Parameters.Add("isActive",
            OracleDbType.Int32).Value = request.IsActive;

        command.Parameters.Add("id",
            OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Schedule not found."
          });

        request.Id = id;
        request.DayOfWeek = day;

        return Ok(request);
      }
      catch (OracleException ex) when (ex.Number == 2291)
      {
        return BadRequest(new
        {
          message = "Selected doctor does not exist."
        });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        return Conflict(new
        {
          message = "Schedule conflicts with an existing record."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating schedule");

        return StatusCode(500, new
        {
          message = "Unable to update schedule."
        });
      }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
      if (id <= 0)
        return BadRequest(new
        {
          message = "Invalid schedule ID."
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

        using var command = connection.CreateCommand();
        command.BindByName = true;

        command.CommandText = @"
            DELETE FROM DOCTOR_SCHEDULES
            WHERE SCHEDULE_ID = :id";

        command.Parameters.Add(
            "id", OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Schedule not found."
          });

        return NoContent();
      }
      catch (OracleException ex) when (ex.Number == 2292)
      {
        return Conflict(new
        {
          message = "Cannot delete schedule because related slots exist."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting schedule");

        return StatusCode(500, new
        {
          message = "Unable to delete schedule."
        });
      }
    }
