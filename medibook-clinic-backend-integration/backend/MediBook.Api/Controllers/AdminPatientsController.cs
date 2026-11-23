
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin/patients")]
  public class AdminPatientsController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminPatientsController> _logger;

    public AdminPatientsController(
        IConfiguration configuration,
        ILogger<AdminPatientsController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    // GET: api/admin/patients
    [HttpGet]
    public async Task<IActionResult> GetPatients()
    {
      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
          throw new InvalidOperationException(
              "Oracle connection string is missing.");

        var patients = new List<object>();

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        PATIENT_ID,
                        FULL_NAME,
                        EMAIL,
                        PHONE_NUMBER,
                        CREATED_AT
                    FROM PATIENTS
                    ORDER BY PATIENT_ID";

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          patients.Add(new
          {
            id = Convert.ToInt32(reader.GetValue(0)),
            fullName = reader.GetString(1),
            email = reader.GetString(2),
            phoneNumber = reader.GetString(3),
            createdAt = reader.GetDateTime(4)
          });
        }

        return Ok(patients);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading patients");

        return StatusCode(500, new
        {
          message = "Unable to load patients."
        });
      }
    }

    public class UpdatePatientRequest
    {
      public string FullName { get; set; } = "";
      public string Email { get; set; } = "";
      public string PhoneNumber { get; set; } = "";
    }

    // PUT: api/admin/patients/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePatient(
        int id,
        [FromBody] UpdatePatientRequest request)
    {
      if (id <= 0 ||
          string.IsNullOrWhiteSpace(request.FullName) ||
          string.IsNullOrWhiteSpace(request.Email) ||
          string.IsNullOrWhiteSpace(request.PhoneNumber))
      {
        return BadRequest(new
        {
          message = "All patient fields are required."
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
                    UPDATE PATIENTS
                    SET FULL_NAME = :fullName,
                        EMAIL = :email,
                        PHONE_NUMBER = :phone
                    WHERE PATIENT_ID = :id";

        command.Parameters.Add(
            "fullName", OracleDbType.Varchar2
        ).Value = request.FullName.Trim();

        command.Parameters.Add(
            "email", OracleDbType.Varchar2
        ).Value = request.Email.Trim();

        command.Parameters.Add(
            "phone", OracleDbType.Varchar2
        ).Value = request.PhoneNumber.Trim();

        command.Parameters.Add(
            "id", OracleDbType.Int32
        ).Value = id;

        int rowsAffected =
            await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
          return NotFound(new
          {
            message = "Patient not found."
          });
        }

        return Ok(new
        {
          id,
          fullName = request.FullName.Trim(),
          email = request.Email.Trim(),
          phoneNumber = request.PhoneNumber.Trim(),
          message = "Patient updated successfully."
        });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        return Conflict(new
        {
          message = "Email or phone number already exists."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating patient");

        return StatusCode(500, new
        {
          message = "Unable to update patient."
        });
      }
    }
  }
}
