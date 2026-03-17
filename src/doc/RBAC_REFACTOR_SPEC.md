\# RBAC Refactor Specification



\## Current System



\- ASP.NET Core Web API

\- Clean Architecture

\- JWT Authentication

\- RBAC implemented using:

&#x20; - PermissionAttribute

&#x20; - PermissionRequirement

&#x20; - PermissionHandler

&#x20; - PermissionPolicyProvider



\---



\## Problems



\- Inconsistent permission naming:

&#x20; - "category.view"

&#x20; - "category.viewbyid"

&#x20; - "categories.delete"



\- Hardcoded permission strings in controllers



\- Seed data is not standardized



\---



\## Target Design



\### Permission Format



module.action



Examples:

\- category.create

\- category.read

\- product.update



\---



\### Roles



\- Admin (full access)

\- User (limited access)



\---



\### Permission Rules



\- Admin must have ALL permissions

\- User only has READ permissions



\---



\### Authorization Rules



\- Admin bypasses all checks

\- Others must have permission in claims

\- No DB query inside handler



\---



\### JWT Claims



\- role

\- permission (multiple)

\- optional: is\_super\_admin



\---



\### Code Requirements



\- Use centralized permission constants

\- Clean code, production-ready

\- Follow Clean Architecture



\---



\## Expected Output



\- Refactored Permission constants

\- Updated Controllers

\- Updated Seed Data

\- Updated AuthorizationHandler

