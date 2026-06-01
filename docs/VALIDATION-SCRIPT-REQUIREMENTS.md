# VERIXORA Validation Script Requirements

The validation script exists to enforce VERIXORA architecture rules as the codebase grows.

## Required Checks

The script must verify:

- All expected projects exist
- Mandatory module folders exist
- Domain projects reference only `SharedKernel.Domain`
- Application projects reference their own Domain project and `SharedKernel.Application`
- Infrastructure projects reference their own Application project and `BuildingBlocks.Infrastructure`
- Presentation projects reference their own Application and Infrastructure projects
- Contracts projects have zero project references
- `SharedKernel.Domain` has zero project references
- `SharedKernel.Domain` has zero NuGet package references
- ApiHost references all Presentation projects
- ApiHost references `BuildingBlocks.Infrastructure`

## Expected Module Shape

Each module should eventually contain:

- `{Module}.Domain`
- `{Module}.Application`
- `{Module}.Infrastructure`
- `{Module}.Presentation`
- `{Module}.Contracts`

Current modules:

- Identity
- Authorization
- Devices
- Provisioning
- SmartLocks
- Monitoring
- Notifications
- Sessions
- AuditLogs
- Reports
- Automation
- Security
- FaceVerification

## Output Requirements

The script output must include a detailed pass/fail table.

Each failed check should include:

- Check name
- Project or path
- Expected result
- Actual result
- Clear remediation hint

The script should exit with a non-zero status when any required check fails.
