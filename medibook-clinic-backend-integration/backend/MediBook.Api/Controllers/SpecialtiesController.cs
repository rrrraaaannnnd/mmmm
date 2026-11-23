
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using MediBook.Api.Models;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin/specialties")]
  public class SpecialtiesController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpecialtiesController> _logger;

    public SpecialtiesController(
        IConfiguration configuration,
        ILogger<SpecialtiesController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSpecialties()
    {
      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
          throw new InvalidOperationException(
              "Oracle connection string is missing.");
        }

        var specialties = new List<Specialty>();

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
                    SELECT
                        SPECIALTY_ID,
                        SPECIALTY_NAME,
                        DESCRIPTION
                    FROM SPECIALTIES
                    ORDER BY SPECIALTY_ID";

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
          specialties.Add(new Specialty
          {
            Id = Convert.ToInt32(reader.GetDecimal(0)),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2)
                  ? string.Empty
                  : reader.GetString(2),
            IsActive = true
          });
        }

        return Ok(specialties);
      }
      catch (Exception ex)
      {
        _logger.LogError(
            ex, "Error loading specialties");

        return StatusCode(500, new
        {
          message = "Unable to load specialties."
        });
      }
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSpecialtyById(int id)
    {
      if (id <= 0)
        return BadRequest(new { message = "Invalid specialty ID." });

      try
      {
        var connectionString = _configuration
            .GetConnectionString("OracleConnection");

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT SPECIALTY_ID, SPECIALTY_NAME, DESCRIPTION
            FROM SPECIALTIES
            WHERE SPECIALTY_ID = :id";

        command.BindByName = true;
        command.Parameters.Add("id", OracleDbType.Int32).Value = id;

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
          return NotFound(new { message = "Specialty not found." });

        var specialty = new Specialty
        {
          Id = Convert.ToInt32(reader.GetDecimal(0)),
          Name = reader.GetString(1),
          Description = reader.IsDBNull(2)
                ? string.Empty
                : reader.GetString(2),
          IsActive = true
        };

        return Ok(specialty);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading specialty");

        return StatusCode(500, new
        {
          message = "Unable to load specialty."
        });
      }
    }


    [HttpPost]
    public async Task<IActionResult> CreateSpecialty(
        [FromBody] Specialty request)
    {
      if (string.IsNullOrWhiteSpace(request.Name))
        return BadRequest(new
        {
          message = "Specialty name is required."
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

        command.CommandText = @"
            INSERT INTO SPECIALTIES
                (SPECIALTY_NAME, DESCRIPTION)
            VALUES
                (:name, :description)
            RETURNING SPECIALTY_ID INTO :newId";

        command.BindByName = true;

        command.Parameters.Add(
            "name", OracleDbType.Varchar2, 100).Value =
            request.Name.Trim();

        command.Parameters.Add(
            "description", OracleDbType.Varchar2, 500).Value =
            string.IsNullOrWhiteSpace(request.Description)
                ? DBNull.Value
                : request.Description.Trim();

        var idParameter = new OracleParameter(
            "newId", OracleDbType.Decimal)
        {
          Direction =
                System.Data.ParameterDirection.Output
        };

        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync();

        int newId = Convert.ToInt32(
            ((Oracle.ManagedDataAccess.Types.OracleDecimal)
                idParameter.Value).Value);

        return CreatedAtAction(
            nameof(GetSpecialtyById),
            new { id = newId },
            new Specialty
            {
              Id = newId,
              Name = request.Name.Trim(),
              Description = request.Description ?? "",
              IsActive = true
            });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        return Conflict(new
        {
          message = "Specialty already exists."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating specialty");

        return StatusCode(500, new
        {
          message = "Unable to create specialty."
        });
      }
    }



    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSpecialty(
        int id,
        [FromBody] Specialty request)
    {
      if (id <= 0 || string.IsNullOrWhiteSpace(request.Name))
        return BadRequest(new
        {
          message = "Valid specialty ID and name are required."
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
            UPDATE SPECIALTIES
            SET SPECIALTY_NAME = :name,
                DESCRIPTION = :description
            WHERE SPECIALTY_ID = :id";

        command.Parameters.Add("name",
            OracleDbType.Varchar2, 100).Value =
            request.Name.Trim();

        command.Parameters.Add("description",
            OracleDbType.Varchar2, 500).Value =
            string.IsNullOrWhiteSpace(request.Description)
                ? DBNull.Value
                : request.Description.Trim();

        command.Parameters.Add("id",
            OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Specialty not found."
          });

        return Ok(new Specialty
        {
          Id = id,
          Name = request.Name.Trim(),
          Description = request.Description?.Trim() ?? "",
          IsActive = true
        });
      }
      catch (OracleException ex) when (ex.Number == 1)
      {
        return Conflict(new
        {
          message = "Specialty name already exists."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating specialty");

        return StatusCode(500, new
        {
          message = "Unable to update specialty."
        });
      }
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSpecialty(int id)
    {
      if (id <= 0)
        return BadRequest(new
        {
          message = "Invalid specialty ID."
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
            DELETE FROM SPECIALTIES
            WHERE SPECIALTY_ID = :id";

        command.Parameters.Add(
            "id", OracleDbType.Int32).Value = id;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
          return NotFound(new
          {
            message = "Specialty not found."
          });

        return NoContent();
      }
      catch (OracleException ex) when (ex.Number == 2292)
      {
        return Conflict(new
        {
          message =
                "Cannot delete specialty because doctors are linked to it."
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting specialty");

        return StatusCode(500, new
        {
          message = "Unable to delete specialty."
        });
      }
    }

  }
}
