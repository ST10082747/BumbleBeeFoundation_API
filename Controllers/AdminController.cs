using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation_API.Models;

namespace BumbleBeeFoundation_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly string _connectionString;

        public AdminController(IConfiguration configuration, ILogger<AdminController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var dashboardViewModel = new DashboardViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    command.CommandText = "SELECT COUNT(*) FROM Users";
                    dashboardViewModel.TotalUsers = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM Companies";
                    dashboardViewModel.TotalCompanies = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM Donations";
                    dashboardViewModel.TotalDonations = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM FundingRequests";
                    dashboardViewModel.TotalFundingRequests = (int)await command.ExecuteScalarAsync();
                }
            }

            return Ok(dashboardViewModel);
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = new List<User>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Users", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            });
                        }
                    }
                }
            }

            return Ok(users);
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            User user = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Users (FirstName, LastName, Email, Password, Role) VALUES (@FirstName, @LastName, @Email, @Password, @Role)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.UserID }, user);
        }

        // PUT: api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UserForEdit userForEdit)
        {
            if (id != userForEdit.UserID)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Users SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Role = @Role WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userForEdit.UserID);
                    command.Parameters.AddWithValue("@FirstName", userForEdit.FirstName);
                    command.Parameters.AddWithValue("@LastName", userForEdit.LastName);
                    command.Parameters.AddWithValue("@Email", userForEdit.Email);
                    command.Parameters.AddWithValue("@Role", userForEdit.Role);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }



        // company management
        // GET: api/admin/companies
        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = new List<Company>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Companies", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            companies.Add(new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("RejectionReason"))
                            });
                        }
                    }
                }
            }

            return Ok(companies); // Returns companies as JSON
        }

        // GET: api/admin/companies/{id}
        [HttpGet("companies/{id}")]
        public async Task<IActionResult> GetCompanyDetails(int id)
        {
            Company company = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Companies WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            company = new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("RejectionReason"))
                            };
                        }
                    }
                }
            }

            if (company == null)
            {
                return NotFound();
            }

            return Ok(company); // Returns company details as JSON
        }

        // POST: api/admin/companies/approve/{id}
        [HttpPost("companies/approve/{id}")]
        public async Task<IActionResult> ApproveCompany(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Companies SET Status = 'Approved' WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Company approved successfully." });
        }

        // POST: api/admin/companies/reject/{id}
        [HttpPost("companies/reject/{id}")]
        public async Task<IActionResult> RejectCompany(int id, [FromBody] string rejectionReason)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Companies SET Status = 'Rejected', RejectionReason = @RejectionReason WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    command.Parameters.AddWithValue("@RejectionReason", rejectionReason ?? string.Empty);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Company rejected with reason: " + rejectionReason });
        }
    



    }
}
