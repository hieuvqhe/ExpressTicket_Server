using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerGetPartnersWithoutContractsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "GetPartnersWithoutContracts")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var pageParam = operation.Parameters.FirstOrDefault(p => p.Name == "page");
            if (pageParam != null)
            {
                pageParam.Description = "Page number (default: 1)";
                pageParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Default"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(1)
                    },
                    ["Page 2"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(2)
                    }
                };
            }

            var limitParam = operation.Parameters.FirstOrDefault(p => p.Name == "limit");
            if (limitParam != null)
            {
                limitParam.Description = "Number of items per page (default: 10, max: 100)";
                limitParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Default"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(10)
                    },
                    ["More Items"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(20)
                    }
                };
            }

            var searchParam = operation.Parameters.FirstOrDefault(p => p.Name == "search");
            if (searchParam != null)
            {
                searchParam.Description = "Search term for partner name";
                searchParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Search by Name"] = new OpenApiExample
                    {
                        Value = new OpenApiString("ABC")
                    },
                    ["Search Partial"] = new OpenApiExample
                    {
                        Value = new OpenApiString("cine")
                    }
                };
            }

            // Response 200 OK - Success
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success with Partners", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách partner chưa có hợp đồng thành công",
                          "result": {
                            "partners": [
                              {
                                "partnerId": 1,
                                "partnerName": "CÔNG TY TNHH RẠP PHIM ABC"
                              },
                              {
                                "partnerId": 3,
                                "partnerName": "RẠP CHIẾU PHIM XYZ"
                              },
                              {
                                "partnerId": 5,
                                "partnerName": "CINEMA HÀ NỘI"
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 15,
                              "totalPages": 2,
                              "hasPrevious": false,
                              "hasNext": true
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Success Empty", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách partner chưa có hợp đồng thành công",
                          "result": {
                            "partners": [],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 0,
                              "totalPages": 0,
                              "hasPrevious": false,
                              "hasNext": false
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 200 OK - With Search Results
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Add("Search Results", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách partner chưa có hợp đồng thành công",
                          "result": {
                            "partners": [
                              {
                                "partnerId": 1,
                                "partnerName": "CÔNG TY TNHH RẠP PHIM ABC"
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 1,
                              "totalPages": 1,
                              "hasPrevious": false,
                              "hasNext": false
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách partner chưa có hợp đồng."
                        }
                        """
                        )
                    });
                }
            }

            operation.Summary = "Get list of partners without any contracts";
            operation.Description = "Retrieve a paginated list of approved partners who don't have any active contracts (excluding draft contracts). This is useful for managers to identify potential partners for new contract creation.";

            // Thêm tags và metadata
            operation.Tags = new List<OpenApiTag>
            {
                new OpenApiTag { Name = "Manager" }
            };

            // Thêm response descriptions
            operation.Responses["200"].Description = "Successfully retrieved partners without contracts";
            operation.Responses["500"].Description = "Internal server error";
        }
    }
}