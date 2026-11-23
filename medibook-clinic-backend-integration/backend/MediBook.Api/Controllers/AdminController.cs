
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MediBook.Api.Controllers
{
  [ApiController]
  [Route("api/admin")]
  public class AdminController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
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

        using var connection =
            new OracleConnection(connectionString);

        await connection.OpenAsync();

        async Task<int> GetCount(string sql)
        {
          using var command = connection.CreateCommand();
          command.CommandText = sql;

          var result = await command.ExecuteScalarAsync();
          return Convert.ToInt32(result);
        }

        var totalDoctors = await GetCount(
            "SELECT COUNT(*) FROM DOCTORS");

        var totalPatients = await GetCount(
            "SELECT COUNT(*) FROM PATIENTS");

        var totalAppointments = await GetCount(
            "SELECT COUNT(*) FROM APPOINTMENTS");

        var pendingAppointments = await GetCount(
            @"SELECT COUNT(*)
                      FROM APPOINTMENTS
                      WHERE UPPER(STATUS) = 'PENDING'");

        return Ok(new
        {
          totalDoctors,
          totalPatients,
          totalAppointments,
          pendingAppointments
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(
            ex, "Error loading admin dashboard");

        return StatusCode(500, new
        {
          message = "Unable to load admin dashboard."
        });
      }
    }
  }
}


