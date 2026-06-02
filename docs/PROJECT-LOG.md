# VERIXORA Project Log

This file is the long-term working memory for VERIXORA. Add one entry per meaningful work session.

## 2026-06-01

### Completed

- Consolidated project notes, PDFs, pasted text, and RTF material into engineering documentation.
- Created the master specification, requirements assessment, architecture decisions, backend use-case/test catalog, roadmap, and validation-script requirements.
- Removed academic proposal and viva-preparation material from engineering docs to keep the repository focused on implementation.
- Updated the root README with the real product identity and architecture direction.
- Inspected current SharedKernel domain primitives.
- Added long-term management files: `PROJECT-LOG.md` and `NEXT-ACTIONS.md`.
- Confirmed the solution builds successfully after NuGet restore.
- Standardized all project target frameworks on `.NET 8`.
- Removed .NET 9-style OpenAPI template calls from ApiHost so the project can target ASP.NET Core 8. Swagger/OpenAPI will be added later with the chosen .NET 8 package stack.
- Verified `dotnet build VERIXORA.sln` succeeds on `.NET 8`.

### Key Decisions

- Build backend core first.
- Use modular monolith, Clean Architecture, Vertical Slice Architecture, and CQRS.
- Backend is the only authority for access decisions.
- Devices are passive executors.
- Device simulation is a first-class development path.
- Defer frontend, production face recognition, SMS/push, PDF export, and advanced cloud deployment until the secure backend flow works.

### Current Status

The project is in the architecture and domain foundation phase.

### Next Step

Start Phase 1: SharedKernel Foundation and architecture validation.
