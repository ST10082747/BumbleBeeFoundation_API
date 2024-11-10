using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation_API.Models;
using System.Text;

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



        // donations
        // GET: api/donations
        [HttpGet("donations")]
        public async Task<ActionResult<IEnumerable<Donation>>> GetDonations()
        {
            var donations = new List<Donation>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                donations.Add(new Donation
                                {
                                    DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                    CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                    CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                    DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                    DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                    DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                    DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                    DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                    PaymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus")) ? null : reader.GetString(reader.GetOrdinal("PaymentStatus")),
                                    DocumentFileName = reader.IsDBNull(reader.GetOrdinal("DocumentPath")) ? null : "Attached Document"
                                });
                            }
                        }
                    }
                }
                return Ok(donations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donations");
                return StatusCode(500, "Internal server error while retrieving donations");
            }
        }

        // GET: api/donations/{id}
        [HttpGet("donations/{id}")]
        public async Task<ActionResult<Donation>> GetDonation(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID 
                    WHERE d.DonationID = @DonationID";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var donation = new Donation
                                {
                                    DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                    CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                    CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                    DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                    DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                    DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                    DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                    DonorIDNumber = reader.GetString(reader.GetOrdinal("DonorIDNumber")),
                                    DonorTaxNumber = reader.GetString(reader.GetOrdinal("DonorTaxNumber")),
                                    DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                    DonorPhone = reader.GetString(reader.GetOrdinal("DonorPhone"))
                                };
                                return Ok(donation);
                            }
                        }
                    }
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donation details for ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while retrieving donation details");
            }
        }

        // PUT: api/donations/{id}/approve
        [HttpPut("donations/{id}/approve")]
        public async Task<ActionResult<Donation>> ApproveDonation(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Update payment status
                    var updateSql = "UPDATE Donations SET PaymentStatus = 'Processed' WHERE DonationID = @DonationID";
                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound();
                        }
                    }

                    // Return the updated donation
                    return await GetDonation(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving donation for ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while approving donation");
            }
        }


        // GET: api/donations/{id}/document
        [HttpGet("donations/{id}/document")]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                SELECT DocumentPath, DonorName, DonationDate 
                FROM Donations 
                WHERE DonationID = @DonationID", connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync() && !reader.IsDBNull(reader.GetOrdinal("DocumentPath")))
                            {
                                var documentBytes = (byte[])reader["DocumentPath"];
                                var donorName = reader.GetString(reader.GetOrdinal("DonorName"));
                                var donationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate"));

                                // Try to detect file type from bytes
                                string contentType = "application/octet-stream";
                                string extension = ".bin";

                                // Check file signatures
                                if (documentBytes.Length >= 4)
                                {
                                    // PDF signature
                                    if (documentBytes[0] == 0x25 && documentBytes[1] == 0x50 &&
                                        documentBytes[2] == 0x44 && documentBytes[3] == 0x46)
                                    {
                                        contentType = "application/pdf";
                                        extension = ".pdf";
                                    }
                                    // PNG signature
                                    else if (documentBytes[0] == 0x89 && documentBytes[1] == 0x50 &&
                                            documentBytes[2] == 0x4E && documentBytes[3] == 0x47)
                                    {
                                        contentType = "image/png";
                                        extension = ".png";
                                    }
                                    // JPEG signature
                                    else if (documentBytes[0] == 0xFF && documentBytes[1] == 0xD8)
                                    {
                                        contentType = "image/jpeg";
                                        extension = ".jpg";
                                    }
                                    // ZIP signature (which could be docx, xlsx, etc.)
                                    else if (documentBytes[0] == 0x50 && documentBytes[1] == 0x4B &&
                                            documentBytes[2] == 0x03 && documentBytes[3] == 0x04)
                                    {
                                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                                        extension = ".docx";
                                    }
                                    // Check if it might be a text file
                                    else if (IsLikelyTextFile(documentBytes))
                                    {
                                        contentType = "text/plain";
                                        extension = ".txt";
                                    }
                                }

                                var fileName = $"Donation_{donorName}_{donationDate:yyyyMMdd}{extension}";
                                return File(documentBytes, contentType, fileName);
                            }
                        }
                    }
                }
                return NotFound("Document not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document for donation ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while downloading document");
            }
        }

        private bool IsLikelyTextFile(byte[] bytes)
        {
            // Check if file is empty
            if (bytes.Length == 0)
                return false;

            // Check for BOM markers
            if (bytes.Length >= 3 &&
                ((bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ||    // UTF-8
                 (bytes[0] == 0xFE && bytes[1] == 0xFF) ||                        // UTF-16 BE
                 (bytes[0] == 0xFF && bytes[1] == 0xFE)))                         // UTF-16 LE
            {
                return true;
            }

            // Check if the content contains only valid text characters
            try
            {
                // Try to decode as UTF-8
                string content = Encoding.UTF8.GetString(bytes);

                // Check if the content contains only printable characters, whitespace, or common control characters
                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];
                    if (!(char.IsLetterOrDigit(c) ||
                          char.IsPunctuation(c) ||
                          char.IsWhiteSpace(c) ||
                          char.IsSymbol(c) ||
                          c == '\r' ||
                          c == '\n' ||
                          c == '\t'))
                    {
                        return false;
                    }
                }

                // Additional check: ensure there aren't too many consecutive null bytes
                // which might indicate a binary file
                int consecutiveNulls = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == 0x00)
                    {
                        consecutiveNulls++;
                        if (consecutiveNulls > 3) // Arbitrary threshold
                            return false;
                    }
                    else
                    {
                        consecutiveNulls = 0;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        // Funding management

        // GET: api/Admin/FundingRequestManagement
        [HttpGet("FundingRequestManagement")]
        public async Task<IActionResult> GetFundingRequestManagement()
        {
            var fundingRequests = new List<FundingRequest>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
                    SELECT fr.*, c.CompanyName 
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fundingRequests.Add(new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                                AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage"))
                            });
                        }
                    }
                }
            }

            return Ok(fundingRequests);
        }

        // GET: api/Admin/FundingRequestDetails/{id}
        [HttpGet("FundingRequestDetails/{id}")]
        public async Task<IActionResult> GetFundingRequestDetails(int id)
        {
            FundingRequest fundingRequest = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT fr.*, c.CompanyName 
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID 
                    WHERE fr.RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            fundingRequest = new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                                AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage"))
                            };
                        }
                    }
                }
            }

            return fundingRequest != null ? Ok(fundingRequest) : NotFound();
        }

        // POST: api/Admin/ApproveFundingRequest
        [HttpPost("ApproveFundingRequest")]
        public async Task<IActionResult> ApproveFundingRequest(int id, [FromBody] string adminMessage)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE FundingRequests SET Status = 'Approved', AdminMessage = @AdminMessage WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    command.Parameters.AddWithValue("@AdminMessage", (object)adminMessage ?? DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }

        // POST: api/Admin/RejectFundingRequest
        [HttpPost("RejectFundingRequest")]
        public async Task<IActionResult> RejectFundingRequest(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE FundingRequests SET Status = 'Rejected' WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }
    }
}
