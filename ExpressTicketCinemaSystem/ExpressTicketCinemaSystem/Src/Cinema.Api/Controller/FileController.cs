using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Produces("application/json")]
    [Authorize] // Yêu cầu đã đăng nhập (Manager/Partner/Admin đều dùng được)
    public class FileController : ControllerBase
    {
        private readonly IAzureBlobService _azureBlobService;

        public FileController(IAzureBlobService azureBlobService)
        {
            _azureBlobService = azureBlobService;
        }

        /// <summary>
        /// Tạo SAS URL chỉ đọc (Read) từ một Blob URL thuần.
        /// FE dùng API này khi cần xem lại PDF đã lưu (chỉ còn BlobUrl trong DB).
        /// </summary>
        /// <param name="request">BlobUrl và thời gian hết hạn mong muốn</param>
        /// <returns>Read SAS URL có thời gian hiệu lực ngắn</returns>
        [HttpPost("generate-read-sas")]
        [ProducesResponseType(typeof(SuccessResponse<GenerateReadSasResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateReadSas([FromBody] GenerateReadSasRequest request)
        {
            try
            {
                // ===== VALIDATION =====
                var errors = new Dictionary<string, ValidationError>();

                if (string.IsNullOrWhiteSpace(request.BlobUrl))
                {
                    errors["blobUrl"] = new ValidationError
                    {
                        Msg = "BlobUrl là bắt buộc",
                        Path = "blobUrl",
                        Location = "body"
                    };
                }
                else if (!Uri.TryCreate(request.BlobUrl, UriKind.Absolute, out _))
                {
                    errors["blobUrl"] = new ValidationError
                    {
                        Msg = "BlobUrl không hợp lệ",
                        Path = "blobUrl",
                        Location = "body"
                    };
                }

                if (errors.Count > 0)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = errors
                    });
                }

                var expiryInDays = request.ExpiryInDays > 0 ? request.ExpiryInDays : 7;

                // ===== BUSINESS LOGIC =====
                var sasUrl = await _azureBlobService.GeneratePdfReadUrlAsync(request.BlobUrl, expiryInDays);

                var response = new SuccessResponse<GenerateReadSasResponse>
                {
                    Message = "Tạo read SAS URL thành công",
                    Result = new GenerateReadSasResponse
                    {
                        ReadSasUrl = sasUrl,
                        ExpiresAt = DateTime.UtcNow.AddDays(expiryInDays)
                    }
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo read SAS URL."
                });
            }
        }
    }
}































