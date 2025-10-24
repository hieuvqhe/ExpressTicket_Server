using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class GetActorsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "GetActors")
            {
                return;
            }

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "message": "Lấy danh sách diễn viên thành công",
                      "result": {
                        "actors": [
                          {
                            "id": 1,
                            "name": "Keanu Reeves",
                            "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg"
                          },
                          {
                            "id": 2,
                            "name": "Carrie-Anne Moss",
                            "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/5gJGLnwO14V4aQKjdlL0VbJN9yO.jpg"
                          }
                        ],
                        "pagination": {
                          "currentPage": 1,
                          "pageSize": 10,
                          "totalCount": 2,
                          "totalPages": 1
                        }
                      }
                    }
                    """
                        )
                    });
                }
            }
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
            {
              "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên."
            }
            """
                        )
                    });
                }
            }
            operation.Summary = "Get all actors";
            operation.Description = "Get paginated list of actors with search and sorting options.";
        }
    }
}