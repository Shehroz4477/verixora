// ====================================================================
// VERIXORA – Identity.Domain / GlobalUsings.cs
// ====================================================================
// Summary:
//   Global using directives for the Identity Domain project.
//   Every source file in this project automatically imports these
//   namespaces, reducing boilerplate in entities, value objects,
//   and domain events.
//
//   Why:
//     - Keeps domain classes focused on business logic.
//     - Ensures consistency across all files.
//     - Makes the dependency surface explicit.
// ====================================================================

// --- Core .NET types ---
global using System;
global using System.Collections.Generic;
global using System.Linq;

// --- SharedKernel domain primitives ---
global using SharedKernel.Domain.Base;
global using SharedKernel.Domain.Events;
global using SharedKernel.Domain.Guard;
global using SharedKernel.Domain.Results;
