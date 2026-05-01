# Contributing to DotSerial

Thank you for your interest in contributing! Please follow these guidelines.

## Getting Started

1. Fork the repository and create a branch from `main`
2. Branch naming: `feature/xxx`, `fix/xxx`, `chore/xxx`
3. Make your changes
4. Ensure tests pass and coverage does not drop
5. Open a pull request

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat: add new feature`
- `fix: correct a bug`
- `docs: update documentation`
- `chore: maintenance tasks`
- `test: add or update tests`
- `refactor: code refactoring`

## Code Style

- Follow `.editorconfig` settings
- All public APIs must have XML doc comments
- Nullable reference types must be handled explicitly
- Every `.cs` file must start with the copyright header

## Running Tests Locally

```bash
# Unit tests
dotnet test tests/DotSerial.Tests.Unit

# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Building the NuGet Package Locally

```bash
dotnet pack src/DotSerial -c Release -o ./artifacts
```

## Pull Request Requirements

- All CI checks must pass
- Code coverage must not drop below existing levels
- New features must include unit tests
- Breaking changes require a major version bump
