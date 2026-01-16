# Contributing to AuthCore

Thank you for your interest in contributing to AuthCore! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/yourusername/authcore/issues)
2. If not, create a new issue with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details (.NET version, OS, etc.)
   - Relevant logs or screenshots

### Suggesting Features

1. Check existing feature requests in [Issues](https://github.com/yourusername/authcore/issues)
2. Create a new issue tagged with "enhancement"
3. Describe the feature, use case, and benefits
4. Provide examples if possible

### Pull Requests

1. **Fork the repository**
   ```bash
   git clone https://github.com/yourusername/authcore.git
   cd authcore
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow the coding standards below
   - Write tests for new functionality
   - Update documentation as needed

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Commit your changes**
   ```bash
   git commit -m "Add: Brief description of changes"
   ```
   Use conventional commits:
   - `Add:` for new features
   - `Fix:` for bug fixes
   - `Update:` for updates to existing features
   - `Refactor:` for code refactoring
   - `Docs:` for documentation changes
   - `Test:` for test-related changes

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**
   - Provide a clear description
   - Reference any related issues
   - Ensure all checks pass

## Coding Standards

### C# Style Guidelines

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small
- Use async/await for I/O operations

### Project Structure

- **AuthCore.Core**: Domain entities, DTOs, and interfaces (no dependencies)
- **AuthCore.Infrastructure**: Data access, external services, implementations
- **AuthCore.API**: Controllers, middleware, configuration

### Testing Guidelines

- Write unit tests for business logic
- Use meaningful test names: `MethodName_Scenario_ExpectedResult`
- Aim for 80%+ code coverage
- Mock external dependencies
- Test both success and failure paths

Example:
```csharp
[Fact]
public void HashPassword_ValidPassword_ShouldReturnHash()
{
    // Arrange
    var hasher = new PasswordHasher();
    var password = "TestPassword123!";
    
    // Act
    var hash = hasher.HashPassword(password);
    
    // Assert
    Assert.NotNull(hash);
    Assert.NotEqual(password, hash);
}
```

### Database Migrations

- Name migrations descriptively: `Add_RefreshToken_Table`
- Test migrations both up and down
- Don't modify existing migrations after they've been merged

### Documentation

- Update README.md for user-facing changes
- Update API documentation in Swagger annotations
- Add code comments for complex logic
- Update SETUP.md for setup/configuration changes

## Development Workflow

1. **Setup development environment**
   ```bash
   ./scripts/setup-dev.sh  # Linux/macOS
   ./scripts/setup-dev.ps1  # Windows
   ```

2. **Run locally**
   ```bash
   docker-compose up -d postgres redis
   cd src/AuthCore.API
   dotnet run
   ```

3. **Run tests during development**
   ```bash
   dotnet watch test
   ```

4. **Check code coverage**
   ```bash
   dotnet test /p:CollectCoverage=true
   ```

## Review Process

1. All PRs require at least one approval
2. CI/CD checks must pass
3. Code coverage must not decrease
4. Documentation must be updated
5. Reviewers may request changes

## Security Issues

**DO NOT** create public issues for security vulnerabilities.

Instead, email security concerns to: security@authcore.dev

Include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

## Questions?

- Check the [Wiki](https://github.com/yourusername/authcore/wiki)
- Ask in [Discussions](https://github.com/yourusername/authcore/discussions)
- Join our [Discord](https://discord.gg/authcore) (if available)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
