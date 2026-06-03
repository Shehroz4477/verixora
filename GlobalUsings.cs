// ============================================================
// VERIXORA – SharedKernel.Domain/GlobalUsings.cs
// ============================================================
// Summary:
//   C# 10 introduced "global usings" – any using directive
//   marked with the `global` keyword is automatically applied
//   to every source file in the project.
//
//   This file centralises the namespaces that the Domain kernel
//   needs.  Other .cs files in this project can use types like
//   List<T>, Regex, or Task without adding their own using
//   statements.
//
//   Why:
//     - Reduces boilerplate in every entity/value object file.
//     - Makes it obvious which BCL namespaces the kernel
//       depends on (no hidden implicit usings beyond BCL).
//     - Keeps the kernel self-contained – no external libraries
//       appear here.
// ============================================================

// --- Core .NET types (object, string, int, bool, etc.) ---
global using System;

// --- Generic collections used in domain logic ---
global using System.Collections.Generic;   // List<T>, IEnumerable<T>, IReadOnlyCollection<T>
global using System.Collections.ObjectModel; // ReadOnlyCollection<T>

// --- LINQ for domain queries (e.g., filtering domain events) ---
global using System.Linq;

// --- Reflection – used by Enumeration base class to discover fields ---
global using System.Reflection;

// --- Threading (for potential domain services that need cancellation) ---
global using System.Threading;
global using System.Threading.Tasks;
