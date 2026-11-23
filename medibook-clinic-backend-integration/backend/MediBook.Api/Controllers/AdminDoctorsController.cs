
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MediBook.Api.Models;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin/doctors")]
  public class AdminDoctorsController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminDoctorsController> _logger;

    public AdminDoctorsController(
        IConfiguration configuration,
        ILogger<AdminDoctorsController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDoctors()
    {
      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        var doctors = new List<Doctor>();

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        DOCTOR_ID,
                        SPECIALTY_ID,
                        FULL_NAME,
                        PHOTO_URL,
                        QUALIFICATIONS,
                        EXPERIENCE_YEARS,
                        CONSULTATION_FEE,
                        BIO,
                        IS_ACTIVE
                    FROM DOCTORS
                    ORDER BY DOCTOR_ID";

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          doctors.Add(new Doctor
          {
            Id = Convert.ToInt32(reader.GetDecimal(0)),
            SpecialtyId = Convert.ToInt32(reader.GetDecimal(1)),
            FullName = reader.GetString(2),
            PhotoUrl = reader.IsDBNull(3)
                  ? null : reader.GetString(3),
            Qualifications = reader.IsDBNull(4)
                  ? null : reader.GetString(4),
            ExperienceYears = reader.IsDBNull(5)
                  ? null : Convert.ToInt32(reader.GetDecimal(5)),
            ConsultationFee = reader.IsDBNull(6)
                  ? null : reader.GetDecimal(6),
            Bio = reader.IsDBNull(7)
                  ? null : reader.GetString(7),
            IsActive = Convert.ToInt32(reader.GetDecimal(8))
          });
        }

        return Ok(doctors);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading admin doctors");

        return StatusCode(500, new
        {
          message = "Unable to load admin doctors."
        });
      }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDoctorById(int id)
    {
      if (id <= 0)
        return BadRequest(new { message = "Invalid doctor ID." });

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
                DOCTOR_ID,
                SPECIALTY_ID,
                FULL_NAME,
                PHOTO_URL,
                QUALIFICATIONS,
                EXPERIENCE_YEARS,
                CONSULTATION_FEE,
                BIO,
                IS_ACTIVE
            FROM DOCTORS
            WHERE DOCTOR_ID = :id";

        command.Parameters.Add("id", OracleDbType.Int32).Value = id;

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
          return NotFound(new { message = "Doctor not found." });

        var doctor = new Doctor
        {
          Id = Convert.ToInt32(reader.GetDecimal(0)),
          SpecialtyId = Convert.ToInt32(reader.GetDecimal(1)),
          FullName = reader.GetString(2),
          PhotoUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
          Qualifications = reader.IsDBNull(4) ? null : reader.GetString(4),
          ExperienceYears = reader.IsDBNull(5)
                ? null : Convert.ToInt32(reader.GetDecimal(5)),
          ConsultationFee = reader.IsDBNull(6)
                ? null : reader.GetDecimal(6),
          Bio = reader.IsDBNull(7) ? null : reader.GetString(7),
          IsActive = Convert.ToInt32(reader.GetDecimal(8))
        };

        return Ok(doctor);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading admin doctor");

        return StatusCode(500, new
        {
          message = "Unable to load doctor."
        });
      }
    }


    [HttpPost]
    public async Task<IActionResult> CreateDoctor([FromBody] Doctor request)
    {
      if (string.IsNullOrWhiteSpace(request.FullName) ||
          request.SpecialtyId <= 0 ||
          request.IsActive is not (0 or 1) ||
          request.ExperienceYears < 0 ||
          request.ConsultationFee < 0)
      {
        return BadRequest(new { message = "Invalid doctor details." });
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
            INSERT INTO DOCTORS
                (SPECIALTY_ID, FULL_NAME, PHOTO_URL,
                 QUALIFICATIONS, EXPERIENCE_YEARS,
                 CONSULTATION_FEE, BIO, IS_ACTIVE)
            VALUES
                (:specialtyId, :fullName, :photoUrl,
                 :qualifications, :experienceYears,
                 :consultationFee, :bio, :isActive)
            RETURNING DOCTOR_ID INTO :newId";

        command.Parameters.Add("specialtyId", OracleDbType.Int32)
            .Value = request.SpecialtyId;

        command.Parameters.Add("fullName", OracleDbType.Varchar2)
            .Value = request.FullName.Trim();

        command.Parameters.Add("photoUrl", OracleDbType.Varchar2)
            .Value = (object?)request.PhotoUrl ?? DBNull.Value;

        command.Parameters.Add("qualifications", OracleDbType.Varchar2)
            .Value = (object?)request.Qualifications ?? DBNull.Value;

        command.Parameters.Add("experienceYears", OracleDbType.Int32)
            .Value = (object?)request.ExperienceYears ?? DBNull.Value;

        command.Parameters.Add("consultationFee", OracleDbType.Decimal)
            .Value = (object?)request.ConsultationFee ?? DBNull.Value;

        command.Parameters.Add("bio", OracleDbType.Varchar2)
            .Value = (object?)request.Bio ?? DBNull.Value;

        command.Parameters.Add("isActive", OracleDbType.Int32)
            .Value = request.IsActive;

        var newIdParameter = new OracleParameter(
            "newId", OracleDbType.Decimal)
        {
          Direction = System.Data.ParameterDirection.Output
        };

        command.Parameters.Add(newIdParameter);

        await command.ExecuteNonQueryAsync();

        int newId = Convert.ToInt32(
            ((Oracle.ManagedDataAccess.Types.OracleDecimal)
                newIdParameter.Value).Value);

        request.Id = newId;

        return CreatedAtAction(
            nameof(GetDoctorById),
            new { id = newId },
            request);
      }
      catch (OracleException ex) when (ex.Number == 2291)
      {
        return BadRequest(new
        {
          message = "Selected specialty does not exist."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating doctor");

        return StatusCode(500, new
        {
          message = "Unable to create doctor."
        });
      }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDoctor(
        int id, [FromBody] Doctor request)
    {
      if (id <= 0 ||
          string.IsNullOrWhiteSpace(request.FullName) ||
          request.SpecialtyId <= 0 ||
          request.IsActive is not (0 or 1) ||
          request.ExperienceYears < 0 ||
          request.ConsultationFee < 0)
      {
        return BadRequest(new
        {
          message = "Invalid doctor details."
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
            UPDATE DOCTORS
            SET SPECIALTY_ID = :specialtyId,
                FULL_NAME = :fullName,
                PHOTO_URL = :photoUrl,
                QUALIFICATIONS = :qualifications,
                EXPERIENCE_YEARS = :experienceYears,
                CONSULTATION_FEE = :consultationFee,
                BIO = :bio,
                IS_ACTIVE = :isActive
            WHERE DOCTOR_ID = :id";

        command.Parameters.Add("specialtyId",
            OracleDbType.Int32).Value = request.SpecialtyId;

        command.Parameters.Add("fullName",
            OracleDbType.Varchar2).Value =
            request.FullName.Trim();

        command.Parameters.Add("photoUrl",
            OracleDbType.Varchar2).Value =
            (object?)request.PhotoUrl ?? DBNull.Value;

        command.Parameters.Add("qualifications",
            OracleDbType.Varchar2).Value =
            (object?)request.Qualifications ?? DBNull.Value;

        command.Parameters.Add("experienceYears",
            OracleDbType.Int32).Value =
            (object?)request.ExperienceYears ?? DBNull.Value;

        command.Parameters.Add("consultationFee",
            OracleDbType.Decimal).Value =
            (object?)request.ConsultationFee ?? DBNull.Value;

        command.Parameters.Add("bio",
            OracleDbType.Varchar2).Value =
            (object?)request.Bio ?? DBNull.Value;

        command.Parameters.Add("isActive",
            OracleDbType.Int32).Value = request.IsActive;

        command.Parameters.Add("id",
            OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Doctor not found."
          });

        request.Id = id;
        return Ok(request);
      }
      catch (OracleException ex) when (ex.Number == 2291)
      {
        return BadRequest(new
        {
          message = "Selected specialty does not exist."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating doctor");

        return StatusCode(500, new
        {
          message = "Unable to update doctor."
        });
      }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
      if (id <= 0)
        return BadRequest(new
        {
          message = "Invalid doctor ID."
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
            DELETE FROM DOCTORS
            WHERE DOCTOR_ID = :id";

        command.Parameters.Add(
            "id", OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Doctor not found."
          });

        return NoContent();
      }
      catch (OracleException ex) when (ex.Number == 2292)
      {
        return Conflict(new
        {
          message = "Cannot delete doctor because related records exist."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting doctor");

        return StatusCode(500, new
        {
          message = "Unable to delete doctor."
        });
      }
    }

  }
}

