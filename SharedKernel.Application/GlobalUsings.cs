// ====================================================================
// VERIXORA – SharedKernel.Application / GlobalUsings.cs
// ====================================================================
// Summary:
//   Global using directives for the Application‑layer SharedKernel.
//   Every .cs file in this project automatically imports these
//   namespaces, reducing boilerplate in abstractions and behaviours.
//
//   Why:
//     - Keeps command/query interface files focused on their contract.
//     - Ensures consistency – all application‑layer code uses the
//       same set of base namespaces.
//     - Makes it obvious which frameworks the Application kernel
//       depends on (MediatR, FluentValidation, Logging).
// ====================================================================

// --- Core .NET types ---
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// --- MediatR (CQRS contracts) ---
// IRequest<TResponse>  – marker for queries/commands
// IRequestHandler<TRequest, TResponse> – handler contract
// IStreamRequest<TResponse> – optional streaming support
global using MediatR;

// --- FluentValidation (pipeline behaviour) ---
// IValidator<T> – used by ValidationBehaviour to auto‑validate requests
global using FluentValidation;

// --- Logging (pipeline behaviour) ---
// ILogger<T> – used by LoggingBehaviour for structured logging
global using Microsoft.Extensions.Logging;

// --- SharedKernel.Domain results ---
// Result, Result<T> – used as return types by all application handlers
global using SharedKernel.Domain.Results;
