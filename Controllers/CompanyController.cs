using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace BumbleBeeFoundation_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(IConfiguration configuration, ILogger<CompanyController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // GET: api/company/{companyId}
        [HttpGet("{companyId}")]
        public async Task<ActionResult<CompanyViewModel>> GetCompanyInfo(int companyId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT * FROM Companies WHERE CompanyID = @CompanyID AND UserID = @UserID";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyID", companyId);
            command.Parameters.AddWithValue("@UserID", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CompanyViewModel
                {
                    CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                    CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                    ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                    ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                    DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    RejectionReason = reader["RejectionReason"] as string
                };
            }

            return NotFound();
        }

        // POST: api/company/RequestFunding
        [HttpPost("RequestFunding")]
        public async Task<ActionResult<int>> RequestFunding([FromBody] FundingRequestViewModel model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO FundingRequests (CompanyID, ProjectDescription, RequestedAmount, ProjectImpact, Status, SubmittedAt)
                             VALUES (@CompanyID, @ProjectDescription, @RequestedAmount, @ProjectImpact, @Status, GETDATE());
                             SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyID", model.CompanyID);
            command.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RequestedAmount", model.RequestedAmount);
            command.Parameters.AddWithValue("@ProjectImpact", model.ProjectImpact ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", "Pending");

            int requestId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return Ok(requestId);
        }

        // GET: api/company/FundingRequestConfirmation/{id}
        [HttpGet("FundingRequestConfirmation/{id}")]
        public async Task<ActionResult<FundingRequestViewModel>> FundingRequestConfirmation(int id)
        {
            var request = new FundingRequestViewModel
            {
                Attachments = new List<AttachmentViewModel>()
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT fr.*, fra.AttachmentID, fra.FileName 
                             FROM FundingRequests fr
                             LEFT JOIN FundingRequestAttachments fra ON fr.RequestID = fra.RequestID
                             WHERE fr.RequestID = @RequestID";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@RequestID", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                request.RequestID = reader.GetInt32(reader.GetOrdinal("RequestID"));
                request.CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID"));
                request.ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription"));
                request.RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount"));
                request.ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact"));
                request.Status = reader.GetString(reader.GetOrdinal("Status"));
                request.SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"));

                do
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("AttachmentID")))
                    {
                        request.Attachments.Add(new AttachmentViewModel
                        {
                            AttachmentID = reader.GetInt32(reader.GetOrdinal("AttachmentID")),
                            FileName = reader.GetString(reader.GetOrdinal("FileName"))
                        });
                    }
                } while (await reader.ReadAsync());

                return Ok(request);
            }

            return NotFound();
        }

        // POST: api/company/UploadDocument
        [HttpPost("upload-document")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument([FromForm] int requestId, IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var companyId = HttpContext.Session.GetInt32("CompanyID");
            if (companyId == null)
            {
                return Unauthorized("CompanyID not found.");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var memoryStream = new MemoryStream())
                {
                    await document.CopyToAsync(memoryStream);
                    byte[] fileContent = memoryStream.ToArray();

                    string query = "INSERT INTO Documents (DocumentName, DocumentType, UploadDate, Status, CompanyID, FileContent, RequestID) " +
                                   "VALUES (@DocumentName, @DocumentType, @UploadDate, @Status, @CompanyID, @FileContent, @RequestID)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentName", document.FileName);
                        command.Parameters.AddWithValue("@DocumentType", document.ContentType);
                        command.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                        command.Parameters.AddWithValue("@Status", "Pending");
                        command.Parameters.AddWithValue("@CompanyID", companyId);
                        command.Parameters.AddWithValue("@FileContent", fileContent);
                        command.Parameters.AddWithValue("@RequestID", requestId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            return Ok("Document uploaded successfully.");
        }


        // GET: api/company/FundingRequestHistory/{companyId}
        [HttpGet("FundingRequestHistory/{companyId}")]
        public async Task<ActionResult<IEnumerable<FundingRequestViewModel>>> FundingRequestHistory(int companyId)
        {
            var requests = new List<FundingRequestViewModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT RequestID, CompanyID, ProjectDescription, RequestedAmount, Status, SubmittedAt, AdminMessage 
                             FROM FundingRequests WHERE CompanyID = @CompanyID ORDER BY SubmittedAt DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyID", companyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requests.Add(new FundingRequestViewModel
                {
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                    ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                    RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                    AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage"))
                });
            }

            return Ok(requests);
        }
    }
}
