// ====================================================================
// VERIXORA – Identity.Infrastructure / GlobalUsings.cs
// ====================================================================
// Summary:
//   Global using directives for the Identity Infrastructure project.
//   Every source file automatically imports these namespaces,
//   reducing boilerplate in repository implementations, EF Core
//   configurations, and service implementations.
//
//   Why:
//     - Keeps infrastructure files focused on their implementation.
//     - Ensures consistency across all files.
//     - Makes the dependency surface explicit.
// ====================================================================

// --- Core .NET types ---
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// --- SharedKernel domain primitives ---
global using SharedKernel.Domain.Base;
global using SharedKernel.Domain.Events;
global using SharedKernel.Domain.Results;

// --- SharedKernel application abstractions ---
global using SharedKernel.Application.Abstractions;

// --- BuildingBlocks infrastructure ---
global using BuildingBlocks.Infrastructure.Persistence;
global using BuildingBlocks.Infrastructure.Encryption;

// --- Identity domain ---
global using Identity.Domain.Entities;
global using Identity.Domain.Enums;
global using Identity.Domain.Events;

// --- Identity application interfaces ---
global using Identity.Application.Interfaces;

// --- Entity Framework Core ---
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// --- Logging ---
global using Microsoft.Extensions.Logging;
