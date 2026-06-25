# Copilot Instructions

## Directrices del proyecto
- Repository Development Rules for ServidorTransaccionalRepo:

**General Rules:**
- Variable/property/function/method names must be in English
- Design using SOLID principles
- Functions/methods should be async preferentially
- All functions must have try-catch blocks
- All functions and properties should have XML documentation (Summary) in English
- Service/interface classes must have XML documentation (Summary) in English

**API/Endpoint Rules:**
- All endpoints must validate JWT tokens (validity and expiration)
- All endpoints must validate ModelState
- All endpoints must implement basic SQL injection prevention
- All DTOs must have DataAnnotations for validation
- Direct database access forbidden; must use services for all database operations

**MAUI Application Rules:**
- Use CommunityToolkit for page design and ViewModels
- Page logic must be in ViewModels
- Create services for API consumption
- ViewModels consuming API services must implement IConnectivity validation in constructor
- Implement SQLite database with DbContextFactory
- Any component requiring permissions (camera, gallery, location, etc.) must: add to AndroidManifest and check permissions before use

**Unit Testing Rules:**
- Use NUnit framework with Mock for testing
- Create tests in existing test project or create new one if needed
- Cover all scenarios: positive and negative test cases for every new function or modification