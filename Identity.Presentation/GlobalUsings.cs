// ====================================================================
// VERIXORA – Identity.Presentation / GlobalUsings.cs
// ====================================================================
// Summary:
//   Global using directives for the Identity Presentation project.
//   Every controller and request/response DTO automatically imports
//   these namespaces, reducing boilerplate and keeping the focus on
//   the API surface.
//
//   Why:
//     - Controllers should not be cluttered with repetitive using
//       statements.
//     - Makes the dependency on ASP.NET Core, MediatR, and the
//       Identity application layer explicit.
//     - Ensures consistency across all API endpoints.
// ====================================================================

// --- Core .NET types ---
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// --- ASP.NET Core MVC ---
global using Microsoft.AspNetCore.Mvc;         // [ApiController], [Route], ControllerBase, IActionResult
global using Microsoft.AspNetCore.Http;        // StatusCodes, HttpContext, IHeaderDictionary
global using Route = Microsoft.AspNetCore.Mvc.RouteAttribute; // Resolve the ambiguity between MVC and Blazor Route attributes.

// --- MediatR (CQRS dispatching) ---
global using MediatR;                           // IMediator for sending commands/queries

// --- SharedKernel results ---
global using SharedKernel.Domain.Results;      // Result, Result<T>

// --- Identity application (commands, queries, DTOs) ---
global using Identity.Application.Commands.RegisterUser;
global using Identity.Application.Commands.Login;
global using Identity.Application.Commands.VerifyEmail;

// --- Identity infrastructure (DI registration) ---
global using Identity.Infrastructure;
