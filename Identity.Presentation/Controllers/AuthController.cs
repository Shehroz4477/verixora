// ====================================================================
// VERIXORA – Identity.Presentation / Controllers / AuthController.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Exposes the authentication and user management API endpoints.
//   This controller receives HTTP requests, translates them into
//   CQRS commands, dispatches them via MediatR to the Application
//   layer, and returns appropriate HTTP responses.
//
//   WHY A THIN CONTROLLER:
//     - All business logic lives in the Application layer (command
//       handlers).  The controller only handles HTTP concerns:
//       routing, serialisation, and status codes.
//     - This makes the API easy to test (mock MediatR) and keeps
//       the code focused.
//
//   ENDPOINTS:
//     POST /api/v1/auth/register      – create a new user account
//     POST /api/v1/auth/login         – authenticate and get tokens
//     POST /api/v1/auth/verify-email  – verify an email address
//
//   API VERSIONING:
//     All routes are prefixed with /api/v1/ per the Master Spec.
//     Future breaking changes will introduce /api/v2/ alongside v1.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **ControllerBase**:
//    - `ControllerBase` provides the base functionality for API
//      controllers without View support (for Razor Pages / MVC).
//    - It includes helper methods like `Ok()`, `Created()`,
//      `BadRequest()`, `StatusCode()`.
//
// 2. **[ApiController]** attribute:
//    - Enables automatic model validation (checks `[Required]`,
//      `[EmailAddress]`, etc. before the action runs).
//    - Automatically returns 400 Bad Request with ProblemDetails
//      if validation fails.
//    - Infers binding sources ([FromBody], [FromRoute]) so you
//      don't need to specify them explicitly.
//
// 3. **[Route]** attribute:
//    - Defines the URL pattern for all actions in this controller.
//    - `api/v{version:apiVersion}/[controller]` uses the ASP.NET
//      API versioning middleware to insert the version number.
//    - `[controller]` is a placeholder replaced by the controller
//      name ("Auth").
//
// 4. **[HttpPost]** attribute:
//    - Marks an action method as responding to HTTP POST requests.
//    - The optional route template (e.g., "register") is appended
//      to the controller's base route.
//
// 5. **IMediator** (from MediatR):
//    - The dispatcher that sends commands/queries to their
//      respective handlers.  The controller doesn't know which
//      handler will process the request.
//
// 6. **ActionResult<T>** / **IActionResult**:
//    - `ActionResult<T>` returns a typed response with automatic
//      serialisation to JSON on success.
//    - `IActionResult` allows returning different status codes
//      (Ok, BadRequest, NotFound) from the same method.
//
// 7. **async / await**:
//    - All MediatR dispatches are asynchronous.  The controller
//      awaits them to keep the request pipeline non‑blocking.
//
// 8. **CancellationToken**:
//    - Propagated from the HTTP request (`HttpContext.RequestAborted`)
//      to the command handler, so that long‑running operations are
//      cancelled if the client disconnects.
// ====================================================================

using Identity.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Serilog;
using SharedKernel.Domain.Base;
using static System.Net.WebRequestMethods;

namespace Identity.Presentation.Controllers;

/// <summary>
/// Handles authentication and user management requests.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initialises the controller with the MediatR dispatcher.
    /// </summary>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration details.</param>
    /// <param name="ct">Cancellation token from the HTTP request.</param>
    /// <returns>
    /// 201 Created with the new user's ID and email on success.
    /// 400 Bad Request if the email is already taken or validation fails.
    /// </returns>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken ct)
    {
        // Build the command from the request DTO.
        var command = new RegisterUserCommand(request.Email, request.Password);

        // Dispatch via MediatR to RegisterUserHandler.
        // The generic overload ensures the return type is Result<RegisterUserResponse>.
        var result = await _mediator.Send<Result<RegisterUserResponse>>(command, ct);

        // If the handler returned a failure, map it to a 400 Bad Request.
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        // On success, return 201 Created with the response body.
        return CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>
    /// Authenticates a user and returns access + refresh tokens.
    /// </summary>
    /// <param name="request">The login credentials and device info.</param>
    /// <param name="ct">Cancellation token from the HTTP request.</param>
    /// <returns>
    /// 200 OK with tokens on success.
    /// 400 Bad Request if credentials are invalid or email is unverified.
    /// </returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.DeviceFingerprint ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send<Result<LoginResponse>>(command, ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Verifies a user's email address.
    /// </summary>
    /// <param name="request">The user ID to verify.</param>
    /// <param name="ct">Cancellation token from the HTTP request.</param>
    /// <returns>
    /// 200 OK on success (or if already verified).
    /// 400 Bad Request if the user is not found.
    /// </returns>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken ct)
    {
        var command = new VerifyEmailCommand(request.UserId);

        // VerifyEmail returns a plain Result (no data).
        var result = await _mediator.Send<Result>(command, ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Email verified successfully." });
    }
}

// ----------------------------------------------------------------
// Request DTOs (internal to the Presentation layer)
// ----------------------------------------------------------------

/// <summary>
/// Request body for the register endpoint.
/// </summary>
public sealed record RegisterUserRequest(string Email, string Password);

/// <summary>
/// Request body for the login endpoint.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password,
    string? DeviceFingerprint = null);

/// <summary>
/// Request body for the verify‑email endpoint.
/// </summary>
public sealed record VerifyEmailRequest(Ulid UserId);




//Dry‑run — how the controller processes requests:
//POST /api/v1/auth/register
//Body: { "email": "alice@example.com", "password": "s3cr3t!!" }

//1.ASP.NET Core binds the JSON body to RegisterUserRequest.
//2. The controller creates RegisterUserCommand(email, password).
//3. MediatR dispatches the command → RegisterUserHandler executes.
//4. Handler returns Result<RegisterUserResponse>.Success(response).
//5. Controller returns:
//   HTTP 201 Created
//   Body: { "userId": "01HXYZA...", "email": "alice@example.com" }


//POST / api / v1 / auth / login
//Body: { "email": "alice@example.com", "password": "s3cr3t!!" }

//1.Controller creates LoginCommand with email, password, device info.
//2. MediatR dispatches → LoginHandler executes.
//3. Handler returns Result<LoginResponse>.Success(tokens).
//4. Controller returns:
//   HTTP 200 OK
//   Body: { "accessToken": "eyJ...", "refreshToken": "...", ... }


//POST / api / v1 / auth / verify - email
//Body: { "userId": "01HXYZA..." }

//1.Controller creates VerifyEmailCommand(userId).
//2.MediatR dispatches → VerifyEmailHandler executes.
//3. Handler returns Result.Success().
//4. Controller returns:
//   HTTP 200 OK
//   Body: { "message": "Email verified successfully." }
