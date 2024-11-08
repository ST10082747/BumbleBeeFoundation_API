using BumbleBeeFoundation_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace BumbleBeeFoundation_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonorController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<DonorController> _logger;

        public DonorController(IConfiguration configuration, ILogger<DonorController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        [HttpGet("FundingRequests")]
        public async Task<IActionResult> GetFundingRequests()
        {
            var fundingRequests = new List<FundingRequestViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT fr.RequestID, fr.CompanyID, fr.ProjectDescription, fr.RequestedAmount,
                      fr.ProjectImpact, fr.Status, fr.SubmittedAt, fr.AdminMessage, c.CompanyName
               FROM FundingRequests fr
               LEFT JOIN Companies c ON fr.CompanyID = c.CompanyID
               WHERE fr.Status IN ('Approved', 'Closed')";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        fundingRequests.Add(new FundingRequestViewModel
                        {
                            RequestID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            CompanyID = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                            ProjectDescription = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            RequestedAmount = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                            ProjectImpact = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Status = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            SubmittedAt = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                            AdminMessage = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            CompanyName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                        });
                    }
                }
            }
            return Ok(fundingRequests);
        }

        [HttpPost("Donate")]
        public async Task<IActionResult> CreateDonation([FromBody] DonationViewModel model)
        {
            int donationId;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"INSERT INTO Donations (DonationDate, DonationType, DonationAmount, DonorName,
                          DonorIDNumber, DonorTaxNumber, DonorEmail, DonorPhone, DocumentPath, PaymentStatus)
                 VALUES (@DonationDate, @DonationType, @DonationAmount, @DonorName, 
                         @DonorIDNumber, @DonorTaxNumber, @DonorEmail, @DonorPhone, 
                         @DocumentPath, 'Pending');
                 SELECT CAST(SCOPE_IDENTITY() as int);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DonationDate", DateTime.Now);
                    command.Parameters.AddWithValue("@DonationType", model.DonationType);
                    command.Parameters.AddWithValue("@DonationAmount", model.DonationAmount);
                    command.Parameters.AddWithValue("@DonorName", model.DonorName);
                    command.Parameters.AddWithValue("@DonorIDNumber", model.DonorIDNumber);
                    command.Parameters.AddWithValue("@DonorTaxNumber", model.DonorTaxNumber);
                    command.Parameters.AddWithValue("@DonorEmail", model.DonorEmail);
                    command.Parameters.AddWithValue("@DonorPhone", model.DonorPhone);
                    command.Parameters.AddWithValue("@DocumentPath", (object)model.DocumentPath ?? DBNull.Value);

                    donationId = await command.ExecuteScalarAsync() as int? ?? 0;
                }
            }

            return CreatedAtAction(nameof(GetDonation), new { id = donationId }, model);
        }

        [HttpGet("Donation/{id}")]
        public async Task<IActionResult> GetDonation(int id)
        {
            DonationViewModel donation = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Donations WHERE DonationID = @DonationID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DonationID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            donation = new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                DonorIDNumber = reader.GetString(reader.GetOrdinal("DonorIDNumber")),
                                DonorTaxNumber = reader.GetString(reader.GetOrdinal("DonorTaxNumber")),
                                DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                DonorPhone = reader.GetString(reader.GetOrdinal("DonorPhone")),
                                DocumentPath = reader.IsDBNull(reader.GetOrdinal("DocumentPath"))
                                    ? null
                                    : (byte[])reader["DocumentPath"]
                            };
                        }
                    }
                }
            }

            return donation == null ? NotFound() : Ok(donation);
        }

        [HttpGet("Donations/User/{userEmail}")]
        public async Task<IActionResult> GetDonationsForUser(string userEmail)
        {
            var donations = new List<DonationViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Donations WHERE DonorEmail = @DonorEmail ORDER BY DonationDate DESC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DonorEmail", userEmail);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            donations.Add(new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName"))
                            });
                        }
                    }
                }
            }

            return Ok(donations);
        }

        [HttpGet("SearchFundingRequests")]
        public async Task<IActionResult> SearchFundingRequests(string term)
        {
            var fundingRequests = new List<FundingRequestViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT fr.RequestID, fr.CompanyID, c.CompanyName, fr.ProjectDescription,
                      fr.RequestedAmount, fr.ProjectImpact, fr.Status, fr.SubmittedAt
                      FROM FundingRequests fr
                      INNER JOIN Companies c ON fr.CompanyID = c.CompanyID
                      WHERE fr.Status IN ('Pending', 'Approved', 'Rejected')
                      AND (c.CompanyName LIKE @SearchTerm OR fr.ProjectDescription LIKE @SearchTerm)
                      ORDER BY fr.SubmittedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{term}%");

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fundingRequests.Add(new FundingRequestViewModel
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
                            });
                        }
                    }
                }
            }

            return Ok(fundingRequests);
        }
    }

}
