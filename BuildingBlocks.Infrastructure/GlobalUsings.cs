// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / GlobalUsings.cs
// ====================================================================
// Summary:
//   Global using directives for the BuildingBlocks.Infrastructure
//   project.  Every source file automatically imports these
//   namespaces, which cover the cross‑cutting frameworks and
//   SharedKernel types used by infrastructure services.
//
//   Why:
//     - Eliminates repetitive using statements in every helper,
//       service, and middleware file.
//     - Makes the dependency surface explicit: EF Core, Serilog,
//       OpenTelemetry, Rate Limiting, etc.
// ====================================================================

// --- Core .NET types ---
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// --- SharedKernel (domain + application abstractions) ---
global using SharedKernel.Domain.Base;
global using SharedKernel.Domain.Events;
global using SharedKernel.Domain.Guard;
global using SharedKernel.Domain.Results;
global using SharedKernel.Application.Abstractions;
global using SharedKernel.Application.Behaviours;
global using SharedKernel.Application.Exceptions;

// --- Entity Framework Core ---
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage;

// --- Serilog (structured logging) ---
global using Serilog;
global using Serilog.Events;

// --- OpenTelemetry (distributed tracing) ---
global using OpenTelemetry;
global using OpenTelemetry.Trace;
global using OpenTelemetry.Metrics;

// --- Rate Limiting ---
global using System.Threading.RateLimiting;

// --- Feature Flags ---
global using Microsoft.FeatureManagement;

// --- Polly (resilience) ---
global using Polly;

// --- Health Checks ---
global using Microsoft.Extensions.Diagnostics.HealthChecks;

// --- Dependency injection ---
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;

// --- Secrets management ---
global using Azure.Identity;
global using Azure.Security.KeyVault.Secrets;

// --- ASP.NET Core abstractions (for middleware) ---
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
