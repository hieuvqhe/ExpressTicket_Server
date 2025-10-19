using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions
{
    /// <summary>
    /// Exception cho lỗi 400 Bad Request (Validation).
    /// </summary>
    public class ValidationException : Exception
    {
        public Dictionary<string, ValidationError> Errors { get; }

        public ValidationException(Dictionary<string, ValidationError> errors)
            : base("Validation failed")
        {
            Errors = errors;
        }

        public ValidationException(string field, string message, string path = "")
            : base("Validation failed")
        {
            Errors = new Dictionary<string, ValidationError>
            {
                [field] = new ValidationError
                {
                    Msg = message,
                    Path = string.IsNullOrEmpty(path) ? field : path,
                    Location = "body"
                }
            };
        }
    }

    /// <summary>
    /// Exception cho lỗi 409 Conflict (Xung đột dữ liệu).
    /// </summary>
    public class ConflictException : Exception
    {
        public Dictionary<string, ValidationError> Errors { get; }

        public ConflictException(string field, string message, string path = "")
            : base("Data conflict")
        {
            Errors = new Dictionary<string, ValidationError>
            {
                [field] = new ValidationError
                {
                    Msg = message,
                    Path = string.IsNullOrEmpty(path) ? field : path,
                    Location = "body"
                }
            };
        }
    }

    /// <summary>
    /// Exception cho lỗi 401 Unauthorized (Xác thực thất bại).
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public Dictionary<string, ValidationError> Errors { get; }

        // Constructor mới nhận Dictionary
        public UnauthorizedException(Dictionary<string, ValidationError> errors)
            : base("Unauthorized")
        {
            Errors = errors;
        }

        // Constructor cũ vẫn giữ để tương thích
        public UnauthorizedException(string message)
            : base("Unauthorized")
        {
            Errors = new Dictionary<string, ValidationError>
            {
                ["auth"] = new ValidationError
                {
                    Msg = message,
                    Path = "form",
                    Location = "body"
                }
            };
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}