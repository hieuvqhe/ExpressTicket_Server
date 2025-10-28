using Amazon;
using Amazon.S3;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Auth;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURATION
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

//  CORS 
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
builder.Services.AddControllers();

var awsOptions = builder.Configuration.GetSection("AWS");
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    return new AmazonS3Client(
        awsOptions["AccessKey"],
        awsOptions["SecretKey"],
        RegionEndpoint.GetBySystemName(awsOptions["Region"])
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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
    options.OperationFilter<CreateScreenExampleFilter>();
    options.OperationFilter<UpdateScreenExampleFilter>();
    options.OperationFilter<GetScreenByIdExampleFilter>();
    options.OperationFilter<GetScreensExampleFilter>();
    options.OperationFilter<SeatTypeExamplesFilter>();
    options.OperationFilter<AdminUserExamplesFilter>();

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
});

// DATABASE 
builder.Services.AddDbContext<CinemaDbCoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

//  DEPENDENCY INJECTION 
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
builder.Services.AddScoped<MovieManagementService>();
builder.Services.AddScoped<ScreenService>();
builder.Services.AddScoped<ISeatTypeService, SeatTypeService>();
builder.Services.AddScoped<ISeatLayoutService, SeatLayoutService>();
builder.Services.AddScoped<S3Service>();


//  JWT AUTHENTICATION 
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
})
.AddGoogle(options =>
{
    var googleSection = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleSection["ClientId"];
    options.ClientSecret = googleSection["ClientSecret"];
});

//  BUILD APP 
var app = builder.Build();

// - MIDDLEWARE 
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
