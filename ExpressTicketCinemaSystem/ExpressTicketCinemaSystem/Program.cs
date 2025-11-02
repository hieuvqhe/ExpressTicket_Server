using ExpressTicketCinemaSystem.Src.Cinema.Api.Example;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Auth;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Text.Json;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager;
using System.Text;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURATION
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// CONTROLLERS & SWAGGER
builder.Services.AddControllers()
     .AddJsonOptions(opt =>
     {
         opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
         opt.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
     })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true; // Tắt auto 400
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    options.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date", Nullable = true });
    // Cấu hình JWT Bearer để có nút "Authorize" trong Swagger
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Nhập token theo định dạng: Bearer {your JWT token}",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    options.OperationFilter<AddGenericMovieExampleFilter>();
    options.OperationFilter<AuthRegisterExampleFilter>();
    options.OperationFilter<AuthLoginExampleFilter>();
    options.OperationFilter<AuthForgotPasswordExampleFilter>();
    options.OperationFilter<AuthVerifyResetCodeExampleFilter>();
    options.OperationFilter<AuthResetPasswordExampleFilter>();
    options.OperationFilter<AuthVerifyEmailExampleFilter>();
    options.OperationFilter<AuthResendVerificationExampleFilter>();
    options.OperationFilter<AuthLoginGoogleExampleFilter>();
    options.OperationFilter<AuthRefreshTokenExampleFilter>();
    options.OperationFilter<AuthLogoutExampleFilter>();
    options.OperationFilter<UserGetMeExampleFilter>();
    options.OperationFilter<UserUpdateMeExampleFilter>();
    options.OperationFilter<UserChangePasswordExampleFilter>();
    options.OperationFilter<UserRequestEmailChangeExampleFilter>();
    options.OperationFilter<UserVerifyCurrentEmailCodeExampleFilter>();
    options.OperationFilter<UserSubmitNewEmailExampleFilter>();
    options.OperationFilter<UserVerifyNewEmailCodeExampleFilter>();
    options.OperationFilter<UserCompleteEmailChangeExampleFilter>();
    options.OperationFilter<PartnerRegisterExampleFilter>();
    options.OperationFilter<PartnerGetProfileExampleFilter>();
    options.OperationFilter<ManagerCreateContractExampleFilter>();
    options.OperationFilter<ManagerGetAllContractsExampleFilter>();
    options.OperationFilter<ManagerGetContractByIdExampleFilter>();
    options.OperationFilter<ManagerFinalizeContractExampleFilter>();
    options.OperationFilter<PartnerUploadSignatureExampleFilter>();
    options.OperationFilter<PartnerGetContractsExampleFilter>();
    options.OperationFilter<PartnerGetContractByIdExampleFilter>();
    options.OperationFilter<ManagerGetPendingPartnersExampleFilter>();
    options.OperationFilter<ManagerSendContractPdfExampleFilter>();
    options.OperationFilter<ManagerApprovePartnerExampleFilter>();
    options.OperationFilter<ManagerRejectPartnerExampleFilter>();
    options.OperationFilter<PartnerPatchProfileExampleFilter>();
    options.OperationFilter<ManagerGetPartnersWithoutContractsExampleFilter>();
    options.OperationFilter<CreateMovieExampleFilter>();
    options.OperationFilter<UpdateMovieExampleFilter>();
    options.OperationFilter<DeleteMovieExampleFilter>();
    options.OperationFilter<GetActorsExampleFilter>();
    options.OperationFilter<GetActorByIdExampleFilter>();
    options.OperationFilter<CreateActorExampleFilter>();
    options.OperationFilter<UpdateActorExampleFilter>();
    options.OperationFilter<DeleteActorExampleFilter>();
    options.OperationFilter<DeleteScreenExampleFilter>();
    options.OperationFilter<CreateScreenExampleFilter>();
    options.OperationFilter<UpdateScreenExampleFilter>();
    options.OperationFilter<GetScreenByIdExampleFilter>();
    options.OperationFilter<GetAllScreensExampleFilter>();
    options.OperationFilter<SeatTypeExamplesFilter>();
    options.OperationFilter<GetAllCinemasExampleFilter>();
    options.OperationFilter<GetCinemaByIdExampleFilter>();
    options.OperationFilter<CreateCinemaExampleFilter>();
    options.OperationFilter<UpdateCinemaExampleFilter>();
    options.OperationFilter<DeleteCinemaExampleFilter>();


    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            jwtSecurityScheme,
            Array.Empty<string>()
        }
    });
    options.OperationFilter<AdminUserExamplesFilter>();

});

// DATABASE 
builder.Services.AddDbContext<CinemaDbCoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// CONFIGURATION FOR AZURE BLOB STORAGE
builder.Services.Configure<AzureBlobStorageSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));

// DEPENDENCY INJECTION 
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ExpressTicketCinemaSystem.Src.Cinema.Application.Services.IMovieService, ExpressTicketCinemaSystem.Src.Cinema.Application.Services.MovieService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<ISeatTypeService, SeatTypeService>();
builder.Services.AddScoped<ISeatLayoutService, SeatLayoutService>();
builder.Services.AddScoped<IContractValidationService, ContractValidationService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<PartnerMovieManagementService>();
builder.Services.AddScoped<ManagerMovieSubmissionService>();



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        ),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            string errorMessage = "Yêu cầu cần được xác thực. Vui lòng cung cấp token hợp lệ.";

            var responseBody = new ValidationErrorResponse
            {
                Message = errorMessage, 
                Errors = new Dictionary<string, ValidationError>
                {
                    ["auth"] = new ValidationError
                    {
                        Msg = errorMessage, 
                        Path = "header",
                        Location = "Authorization"
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(responseBody);
            await context.Response.WriteAsync(jsonResponse);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            string errorMessage = "Bạn không có quyền thực hiện hành động này.";

            var responseBody = new ValidationErrorResponse
            {
                Message = errorMessage,
                Errors = new Dictionary<string, ValidationError>
                {
                    ["auth"] = new ValidationError
                    {
                        Msg = errorMessage,
                        Path = "role",
                        Location = "token"
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(responseBody);
            await context.Response.WriteAsync(jsonResponse);
        }
    };
})
.AddGoogle(options =>
{
    var googleSection = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleSection["ClientId"];
    options.ClientSecret = googleSection["ClientSecret"];
});

// BUILD APP 
var app = builder.Build();

// MIDDLEWARE 
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();