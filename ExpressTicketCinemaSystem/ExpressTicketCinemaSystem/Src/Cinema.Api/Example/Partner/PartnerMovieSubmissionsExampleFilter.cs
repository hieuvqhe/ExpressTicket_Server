using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    /// <summary>
    /// Inject request/response examples cho toàn bộ API PartnerMovieManagementController
    /// </summary>
    public class PartnerMovieSubmissionsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (!string.Equals(controller, "PartnerMovieManagement", StringComparison.Ordinal))
                return;

            // Dispatch theo action name
            switch (action)
            {
                // ======== AVAILABLE ACTORS ========
                case "GetAvailableActors":
                    Example_GetAvailableActors(operation);
                    break;
                case "GetAvailableActorById":
                    Example_GetAvailableActorById(operation);
                    break;

                // ======== SUBMISSION ACTORS ========
                case "AddActorToSubmission":
                    Example_AddActorToSubmission(operation);
                    break;
                case "GetSubmissionActors":
                    Example_GetSubmissionActors(operation);
                    break;
                case "UpdateSubmissionActor":
                    Example_UpdateSubmissionActor(operation);
                    break;
                case "RemoveActorFromSubmission":
                    Example_RemoveActorFromSubmission(operation);
                    break;

                // ======== SUBMISSIONS CRUD ========
                case "CreateMovieSubmission":
                    Example_CreateMovieSubmission(operation);
                    break;
                case "GetMovieSubmissions":
                    Example_GetMovieSubmissions(operation);
                    break;
                case "GetMovieSubmissionById":
                    Example_GetMovieSubmissionById(operation);
                    break;
                case "UpdateMovieSubmission":
                    Example_UpdateMovieSubmission(operation);
                    break;
                case "SubmitMovieSubmission":
                    Example_SubmitMovieSubmission(operation);
                    break;
                case "DeleteMovieSubmission":
                    Example_DeleteMovieSubmission(operation);
                    break;
            }
        }

        // ----------------- HELPERS -----------------
        private static OpenApiString J(string json) => new OpenApiString(json);

        private static void SetRequestExample(OpenApiOperation op, string name, string json, string? description = null)
        {
            if (op.RequestBody == null) return;
            op.RequestBody.Description = description ?? op.RequestBody.Description;
            var content = op.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
            if (content == null) return;
            content.Examples ??= new Dictionary<string, OpenApiExample>();
            content.Examples.Clear();
            content.Examples.Add(name, new OpenApiExample { Value = J(json) });
        }

        private static void AddResponseExample(OpenApiOperation op, string statusCode, string name, string json)
        {
            if (!op.Responses.ContainsKey(statusCode)) return;
            var resp = op.Responses[statusCode];
            var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
            if (content == null) return;
            content.Examples ??= new Dictionary<string, OpenApiExample>();
            // không Clear() để có thể add nhiều mẫu 1 mã phản hồi (nếu cần)
            content.Examples[name] = new OpenApiExample { Value = J(json) };
        }

        // =========================================================
        // AVAILABLE ACTORS
        // =========================================================
        private static void Example_GetAvailableActors(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Lấy danh sách diễn viên có sẵn thành công",
              "result": {
                "actors": [
                  { "id": 101, "name": "John Doe", "profileImage": "https://cdn/app/john.webp" },
                  { "id": 102, "name": "Jane Smith", "profileImage": "https://cdn/app/jane.png" }
                ],
                "pagination": { "currentPage": 1, "pageSize": 10, "totalCount": 42, "totalPages": 5 }
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            {
              "message": "Lỗi xác thực dữ liệu",
              "errors": {
                "contract": {
                  "msg": "Partner chưa có hợp đồng active",
                  "path": "contract",
                  "location": "authorization"
                }
              }
            }
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            {
              "message": "Xác thực thất bại",
              "errors": {
                "auth": {
                  "msg": "Token không hợp lệ hoặc không chứa ID người dùng.",
                  "path": "form",
                  "location": "body"
                }
              }
            }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên." }
            """);
        }

        private static void Example_GetAvailableActorById(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            { "message": "Lấy thông tin diễn viên thành công",
              "result": { "id": 101, "name": "John Doe", "profileImage": "https://cdn/app/john.webp" } }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"contract":{"msg":"Partner chưa có hợp đồng active","path":"contract","location":"authorization"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy diễn viên với ID này." }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin diễn viên." }
            """);
        }

        // =========================================================
        // SUBMISSION ACTORS
        // =========================================================
        private static void Example_AddActorToSubmission(OpenApiOperation op)
        {
            // Request: 2 biến thể
            SetRequestExample(op, "Select Existing Actor",
            """
            {
              "actorId": 101,
              "actorName": null,
              "actorAvatarUrl": null,
              "role": "Lead"
            }
            """, "Chọn diễn viên hệ thống hoặc tạo diễn viên nháp");

            // 201
            AddResponseExample(op, "201", "Created",
            """
            {
              "message": "Thêm diễn viên vào submission thành công",
              "result": {
                "movieSubmissionActorId": 555,
                "actorId": 101,
                "actorName": "John Doe",
                "actorAvatarUrl": "https://cdn/app/john.webp",
                "role": "Lead",
                "isExistingActor": true
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            {
              "message": "Lỗi xác thực dữ liệu",
              "errors": {
                "status": { "msg": "Chỉ được thao tác khi submission ở trạng thái Draft.", "path": "status", "location": "body" },
                "role": { "msg": "Vai diễn là bắt buộc", "path": "role", "location": "body" },
                "actorAvatarUrl": { "msg": "URL avatar không hợp lệ", "path": "actorAvatarUrl", "location": "body" }
              }
            }
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy diễn viên trong hệ thống" }
            """);

            // 409
            AddResponseExample(op, "409", "Conflict",
            """
            { "message":"Xung đột dữ liệu",
              "errors":{"actorId":{"msg":"Diễn viên đã có trong submission này","path":"actorId","location":"body"}}}
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi thêm diễn viên." }
            """);
        }

        private static void Example_GetSubmissionActors(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Lấy danh sách diễn viên thành công",
              "result": {
                "actors": [
                  { "movieSubmissionActorId": 555, "actorId": 101, "actorName": "John Doe", "actorAvatarUrl": "https://cdn/app/john.webp", "role": "Lead", "isExistingActor": true },
                  { "movieSubmissionActorId": 556, "actorId": null, "actorName": "New Talent", "actorAvatarUrl": "https://cdn/app/new.jpg", "role": "Supporting", "isExistingActor": false }
                ],
                "totalCount": 2
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"status":{"msg":"Chỉ được thao tác khi submission ở trạng thái Draft.","path":"status","location":"body"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy bản nháp phim" }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên." }
            """);
        }

        private static void Example_UpdateSubmissionActor(OpenApiOperation op)
        {
            // Request
            SetRequestExample(op, "Update Role / Draft Info",
            """
            {
              "actorName": null,
              "actorAvatarUrl": null,
              "role": "Main Protagonist"
            }
            """, "Đổi vai diễn; với actor nháp có thể sửa tên/avatar");

            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Cập nhật diễn viên thành công",
              "result": {
                "movieSubmissionActorId": 555,
                "actorId": 101,
                "actorName": "John Doe",
                "actorAvatarUrl": "https://cdn/app/john.webp",
                "role": "Main Protagonist",
                "isExistingActor": true
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            {
              "message": "Lỗi xác thực dữ liệu",
              "errors": {
                "actor": { "msg": "Không thể đổi thông tin diễn viên hệ thống, chỉ được đổi vai diễn", "path": "actor", "location": "body" },
                "role": { "msg": "Vai diễn là bắt buộc", "path": "role", "location": "body" },
                "actorAvatarUrl": { "msg": "URL avatar không hợp lệ", "path": "actorAvatarUrl", "location": "body" },
                "status": { "msg": "Chỉ được thao tác khi submission ở trạng thái Draft.", "path": "status", "location": "body" }
              }
            }
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy diễn viên trong submission" }
            """);

            // 409
            AddResponseExample(op, "409", "Conflict",
            """
            { "message":"Xung đột dữ liệu",
              "errors":{"actorName":{"msg":"Diễn viên đã có trong submission này","path":"actorName","location":"body"}}}
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi cập nhật diễn viên." }
            """);
        }

        private static void Example_RemoveActorFromSubmission(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            { "message": "Xóa diễn viên khỏi submission thành công", "result": {} }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"status":{"msg":"Chỉ được thao tác khi submission ở trạng thái Draft.","path":"status","location":"body"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy diễn viên trong submission" }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi xóa diễn viên." }
            """);
        }

        // =========================================================
        // SUBMISSIONS CRUD
        // =========================================================
        private static void Example_CreateMovieSubmission(OpenApiOperation op)
        {
            // Request
            SetRequestExample(op, "Create Draft",
            """
            {
              "title": "The River of Light 4",
              "genre": "Drama",
              "durationMinutes": 120,
              "director": "Jane Doe",
              "language": "vi",
              "country": "VN",
              "posterUrl": "https://assets/images/poster.webp",
              "bannerUrl": "https://assets/images/banner.png",
              "production": "Studio A",
              "description": "Synopsis ...",
              "premiereDate": "2026-02-10",
              "endDate": "2026-03-10",
              "trailerUrl": "https://youtu.be/xyz123",
              "copyrightDocumentUrl": "https://assets/docs/copyright.pdf",
              "distributionLicenseUrl": "https://assets/docs/license.pdf",
              "additionalNotes": "Ghi chú",
              "actorIds": [101, 102],
              "newActors": [
                { "name": "New Talent", "avatarUrl": "https://cdn/app/new.jpg", "role": "Supporting" }
              ],
              "actorRoles": { "101": "Lead", "102": "Supporting" }
            }
            """, "Tạo bản nháp phim cùng danh sách diễn viên");

            // 201
            AddResponseExample(op, "201", "Created",
            """
            {
              "message": "Tạo bản nháp phim thành công",
              "result": {
                "movieSubmissionId": 2001,
                "title": "The River of Light 4",
                "status": "Draft",
                "createdAt": "2025-11-01T05:57:59.897Z",
                "updatedAt": "2025-11-01T05:57:59.897Z",
                "actors": [
                  { "movieSubmissionActorId": 700, "actorId": 101, "actorName": "John Doe", "actorAvatarUrl": "https://cdn/app/john.webp", "role": "Lead", "isExistingActor": true },
                  { "movieSubmissionActorId": 701, "actorId": null, "actorName": "New Talent", "actorAvatarUrl": "https://cdn/app/new.jpg", "role": "Supporting", "isExistingActor": false }
                ]
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            {
              "message": "Lỗi xác thực dữ liệu",
              "errors": {
                "title": { "msg": "Tiêu đề là bắt buộc", "path": "title", "location": "body" },
                "posterUrl": { "msg": "URL poster phải là ảnh hợp lệ (jpg, jpeg, png, webp)", "path": "posterUrl", "location": "body" },
                "premiereDate": { "msg": "Ngày công chiếu phải ở tương lai", "path": "premiereDate", "location": "body" },
                "endDate": { "msg": "Ngày kết thúc phải sau ngày công chiếu", "path": "endDate", "location": "body" }
              }
            }
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 409
            AddResponseExample(op, "409", "Conflict",
            """
            { "message":"Xung đột dữ liệu",
              "errors":{"title":{"msg":"Đã tồn tại bản nháp với tiêu đề 'The River of Light 4'. Vui lòng sử dụng bản nháp hiện có hoặc đổi tiêu đề.","path":"title","location":"body"}}}
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi tạo bản nháp phim." }
            """);
        }

        private static void Example_GetMovieSubmissions(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Lấy danh sách bản nháp phim thành công",
              "result": {
                "submissions": [
                  { "movieSubmissionId": 2001, "title": "The River of Light 4", "status": "Draft", "createdAt": "2025-11-01T05:57:59.897Z", "updatedAt": "2025-11-01T05:57:59.897Z" },
                  { "movieSubmissionId": 2000, "title": "The River of Light 3", "status": "Pending", "submittedAt": "2025-11-01T06:13:12.918Z" }
                ],
                "pagination": { "currentPage": 1, "pageSize": 10, "totalCount": 12, "totalPages": 2 }
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"contract":{"msg":"Partner chưa có hợp đồng active","path":"contract","location":"authorization"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi lấy danh sách bản nháp phim." }
            """);
        }

        private static void Example_GetMovieSubmissionById(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Lấy thông tin bản nháp phim thành công",
              "result": {
                "movieSubmissionId": 2001,
                "title": "The River of Light 4",
                "genre": "Drama",
                "durationMinutes": 120,
                "director": "Jane Doe",
                "language": "vi",
                "country": "VN",
                "posterUrl": "https://assets/images/poster.webp",
                "bannerUrl": "https://assets/images/banner.png",
                "production": "Studio A",
                "description": "Synopsis ...",
                "premiereDate": "2026-02-10",
                "endDate": "2026-03-10",
                "trailerUrl": "https://youtu.be/xyz123",
                "copyrightDocumentUrl": "https://assets/docs/copyright.pdf",
                "distributionLicenseUrl": "https://assets/docs/license.pdf",
                "additionalNotes": "Ghi chú",
                "status": "Draft",
                "submittedAt": null,
                "reviewedAt": null,
                "rejectionReason": null,
                "movieId": null,
                "createdAt": "2025-11-01T05:57:59.897Z",
                "updatedAt": "2025-11-01T05:57:59.897Z",
                "actors": []
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"contract":{"msg":"Partner chưa có hợp đồng active","path":"contract","location":"authorization"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message": "Không tìm thấy bản nháp phim" }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi lấy thông tin bản nháp phim." }
            """);
        }

        private static void Example_UpdateMovieSubmission(OpenApiOperation op)
        {
            // Request
            SetRequestExample(op, "Update Draft",
            """
            {
              "title": "The River of Light 4 (Updated)",
              "genre": "Drama",
              "durationMinutes": 118,
              "posterUrl": "https://assets/images/poster2.webp",
              "premiereDate": "2026-02-20",
              "endDate": "2026-03-20",
              "trailerUrl": "https://youtu.be/xyz456"
            }
            """, "Cập nhật bản nháp phim");

            // 200
            AddResponseExample(op, "200", "Success",
            """
            { "message":"Cập nhật bản nháp phim thành công",
              "result":{ "movieSubmissionId":2001, "title":"The River of Light 4 (Updated)", "status":"Draft", "updatedAt":"2025-11-01T06:30:00Z"} }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{
                "posterUrl":{"msg":"URL poster phải là ảnh hợp lệ (jpg, jpeg, png, webp)","path":"posterUrl","location":"body"},
                "premiereDate":{"msg":"Ngày công chiếu phải ở tương lai","path":"premiereDate","location":"body"},
                "endDate":{"msg":"Ngày kết thúc phải sau ngày công chiếu","path":"endDate","location":"body"},
                "status":{"msg":"Chỉ được thao tác khi submission ở trạng thái Draft.","path":"status","location":"body"}
              }}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message":"Không tìm thấy bản nháp phim" }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi cập nhật bản nháp phim." }
            """);
        }

        private static void Example_SubmitMovieSubmission(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            {
              "message": "Nộp phim thành công. Vui lòng chờ quản trị viên xét duyệt.",
              "result": {
                "movieSubmissionId": 2001,
                "status": "Pending",
                "submittedAt": "2025-11-01T06:45:00Z",
                "reviewerId": 1,
                "resubmitCount": 1,
                "resubmittedAt": "2025-11-01T06:45:00Z"
              }
            }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            {
              "message": "Lỗi xác thực dữ liệu",
              "errors": {
                "status": { "msg": "Phim này đã tồn tại trong hệ thống. Không thể nộp lại.", "path": "status", "location": "body" },
                "copyrightDocumentUrl": { "msg": "Tài liệu bản quyền là bắt buộc để nộp phim", "path": "copyrightDocumentUrl", "location": "body" },
                "distributionLicenseUrl": { "msg": "Giấy phép phân phối là bắt buộc để nộp phim", "path": "distributionLicenseUrl", "location": "body" },
                "premiereDate": { "msg": "Ngày công chiếu phải ở tương lai", "path": "premiereDate", "location": "body" },
                "endDate": { "msg": "Ngày kết thúc phải sau ngày công chiếu", "path": "endDate", "location": "body" }
              }
            }
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message":"Không tìm thấy movie submission" }
            """);

            // 409
            AddResponseExample(op, "409", "Conflict",
            """
            { "message":"Xung đột dữ liệu",
              "errors":{"title":{"msg":"Tiêu đề này đã tồn tại trong hệ thống.","path":"title","location":"body"}}}
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi nộp phim." }
            """);
        }

        private static void Example_DeleteMovieSubmission(OpenApiOperation op)
        {
            // 200
            AddResponseExample(op, "200", "Success",
            """
            { "message": "Xóa bản nháp phim thành công", "result": {} }
            """);

            // 400
            AddResponseExample(op, "400", "Validation Error",
            """
            { "message":"Lỗi xác thực dữ liệu",
              "errors":{"status":{"msg":"Chỉ được thao tác khi submission ở trạng thái Draft.","path":"status","location":"body"}}}
            """);

            // 401
            AddResponseExample(op, "401", "Unauthorized",
            """
            { "message":"Xác thực thất bại",
              "errors":{"auth":{"msg":"Token không hợp lệ hoặc không chứa ID người dùng.","path":"form","location":"body"}}}
            """);

            // 404
            AddResponseExample(op, "404", "Not Found",
            """
            { "message":"Không tìm thấy movie submission" }
            """);

            // 500
            AddResponseExample(op, "500", "Server Error",
            """
            { "message":"Đã xảy ra lỗi hệ thống khi xóa bản nháp phim." }
            """);
        }
    }
}
