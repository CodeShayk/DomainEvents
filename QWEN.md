# DomainEvents Library - Project Context

## Project Overview

**DomainEvents** is a .NET library that facilitates implementing **transactional domain events** within domain-driven design (DDD) bounded contexts. The library provides a pub/sub mechanism for raising and handling domain events to manage side effects across multiple aggregates while maintaining consistency.

### Purpose

- Enable explicit implementation of side effects triggered by domain changes
- Support in-process, same-domain event handling
- Ensure transactional consistency (all event-related operations succeed or all fail)
- Provide a clean separation between event publishers and handlers

### Core Components

| Component | Description |
|-----------|-------------|
| `IDomainEvent` | Marker interface for defining domain events |
| `IPublisher` | Interface for raising/publishing domain events |
| `IHandler<T>` | Interface for implementing event handlers |
| `IResolver` | Interface for resolving handlers for a given event type |
| `Publisher` | Concrete implementation of `IPublisher` |
| `Resolver` | Concrete implementation of `IResolver` |

### Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Publisher  │────▶│   Resolver   │────▶│  IHandler<T>[]  │
│  (IPublisher)│     │  (IResolver) │     │  (Event Handlers)│
└─────────────┘     └──────────────┘     └─────────────────┘
```

## Building and Running

### Prerequisites

- .NET SDK 9.0 or later (for building/testing)
- The library targets multiple frameworks: `net462`, `netstandard2.0`, `netstandard2.1`, `net9.0`, `net10.0`

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the library (Release configuration)
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release --verbosity normal

# Build with specific version
dotnet build --configuration Release -p:PackageVersion=4.1.0
```

### Project Structure

```
DomainEvents/
├── src/
│   └── DomainEvents/          # Main library source
│       ├── IDomainEvent.cs    # Event marker interface
│       ├── IHandle.cs         # Handler interface
│       ├── IPublisher.cs      # Publisher interface
│       ├── IResolver.cs       # Resolver interface
│       └── Impl/
│           ├── Publisher.cs   # Publisher implementation
│           └── Resolver.cs    # Resolver implementation
├── test/
│   └── DomainEvents.Tests/    # NUnit test project
│       ├── Events/            # Test event definitions
│       ├── Handlers/          # Test handler implementations
│       └── Run/               # Test cases
└── .github/
    └── workflows/
        └── CI-Build.yml       # GitHub Actions CI/CD
```

## Development Conventions

### Coding Style

- **Nullable reference types**: Disabled (`Nullable>disable</Nullable>`)
- **Implicit usings**: Disabled (`ImplicitUsings>disable</ImplicitUsings>`)
- **Target frameworks**: Multi-targeted for broad compatibility
- **Naming**: PascalCase for interfaces (`IPublisher`, `IHandler<T>`), classes follow standard .NET conventions

### Testing Practices

- **Framework**: NUnit 4.x
- **Test adapter**: NUnit3TestAdapter
- **Coverage**: coverlet.collector for code coverage
- **Test structure**: Separate folders for Events, Handlers, and Run (test cases)

### Versioning

- Uses **Nerdbank.GitVersioning** for semantic versioning
- Version configuration in `version.json`
- Public releases from `master` branch only
- Current version: **4.1.0**

### CI/CD Workflow

The GitHub Actions workflow (`.github/workflows/CI-Build.yml`) handles:

1. **Linting**: Super-linter on PR events
2. **Build (Beta)**: For non-release branches with auto-versioning
3. **Build (Release)**: For `release/*` branches
4. **Testing**: Runs on every build
5. **Packaging**: Publishes to GitHub Packages
6. **Release**: Publishes to NuGet.org for release branches

### Package Information

- **Package ID**: `Dormito.DomainEvents`
- **Assembly Name**: `Dormito`
- **Root Namespace**: `DomainEvents`
- **License**: MIT License
- **Repository**: https://github.com/CodeShayk/DomainEvents

## Usage Pattern

### 1. Define an Event

```csharp
public class CustomerCreated : IDomainEvent
{
    public string Name { get; set; }
}
```

### 2. Create a Handler

```csharp
public class CustomerCreatedHandler : IHandler<CustomerCreated>
{
    public Task HandleAsync(CustomerCreated @event)
    {
        Console.WriteLine($"Customer created: {@event.Name}");
        return Task.CompletedTask;
    }
}
```

### 3. Register with DI Container

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<IResolver>(sp => 
        new Resolver(sp.GetServices<IHandler>()));
    services.AddTransient<IPublisher, Publisher>();
    services.AddTransient<IHandler, CustomerCreatedHandler>();
}
```

### 4. Publish Events

```csharp
public class OrderService
{
    private readonly IPublisher _publisher;

    public OrderService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task CreateOrderAsync(Order order)
    {
        // ... create order logic
        var @event = new OrderCreated { OrderId = order.Id };
        await _publisher.RaiseAsync(@event);
    }
}
```

## Key Files Reference

| File | Purpose |
|------|---------|
| `README.md` | Library documentation and usage examples |
| `DomainEvents.sln` | Visual Studio solution file |
| `src/DomainEvents/DomainEvents.csproj` | Library project file with package metadata |
| `test/DomainEvents.Tests/DomainEvents.Tests.csproj` | Test project configuration |
| `.github/workflows/CI-Build.yml` | CI/CD pipeline definition |
| `nuget.config` | NuGet package source configuration |
| `version.json` | GitVersioning configuration |
| `License.md` | MIT License |
