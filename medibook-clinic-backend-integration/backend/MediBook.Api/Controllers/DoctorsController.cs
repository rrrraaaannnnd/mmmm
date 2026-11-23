
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace MediBook.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(
            IConfiguration configuration,
            ILogger<DoctorsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET /api/Doctors
        // GET /api/Doctors?search=Cardiology
        [HttpGet]
        public async Task<IActionResult> GetDoctors(
            [FromQuery] string? search)
        {
            try
            {
                var doctors = new List<DoctorDto>();

                using var connection = CreateConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();

                command.CommandText = @"
                    SELECT
                        d.DOCTOR_ID,
                        d.FULL_NAME,
                        s.SPECIALTY_NAME,
                        d.CONSULTATION_FEE
                    FROM DOCTORS d
                    JOIN SPECIALTIES s
                        ON d.SPECIALTY_ID = s.SPECIALTY_ID
                    WHERE
                        :searchTerm IS NULL
                        OR UPPER(d.FULL_NAME) LIKE
                            '%' || UPPER(:searchTerm) || '%'
                        OR UPPER(s.SPECIALTY_NAME) LIKE
                            '%' || UPPER(:searchTerm) || '%'
                    ORDER BY d.DOCTOR_ID";

                command.BindByName = true;

                command.Parameters.Add(
                    "searchTerm",
                    OracleDbType.Varchar2
                ).Value = string.IsNullOrWhiteSpace(search)
                    ? DBNull.Value
                    : search.Trim();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    doctors.Add(new DoctorDto(
                        Convert.ToInt32(reader.GetDecimal(0)),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.IsDBNull(3)
                            ? null
                            : reader.GetDecimal(3)
                    ));
                }

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctors");
                return StatusCode(500, new
                {
                    message = "Unable to load doctors"
                });
            }
        }

        // GET /api/Doctors/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();

                command.CommandText = @"
                    SELECT
                        d.DOCTOR_ID,
                        d.FULL_NAME,
                        s.SPECIALTY_NAME,
                        d.CONSULTATION_FEE
                    FROM DOCTORS d
                    JOIN SPECIALTIES s
                        ON d.SPECIALTY_ID = s.SPECIALTY_ID
                    WHERE d.DOCTOR_ID = :doctorId";

                command.BindByName = true;
                command.Parameters.Add(
                    "doctorId",
                    OracleDbType.Int32
                ).Value = id;

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return NotFound(new
                    {
                        message = "Doctor not found"
                    });
                }

                var doctor = new DoctorDto(
                    Convert.ToInt32(reader.GetDecimal(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3)
                        ? null
                        : reader.GetDecimal(3)
                );

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor");
                return StatusCode(500, new
                {
                    message = "Unable to load doctor"
                });
            }
        }

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

        public record DoctorDto(
            int Id,
            string Name,
            string Specialty,
            decimal? ConsultationFee
        );
    }
}

