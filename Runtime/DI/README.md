# Dependency Injection Container

## Overview

The `Container` is a lightweight, reflection-based dependency injection system designed for Unity projects. It provides automatic service registration, constructor and property injection, and lifecycle management (Singleton, Transient).

## Key Features

- **Automatic Service Discovery**: Scan assemblies for classes marked with `[Service]` attribute
- **Multiple Registration Methods**: Register by interface, by type, or by instance
- **Flexible Injection**: Supports field, property, and method injection via `[Inject]` attribute
- **Lifecycle Management**: Singleton and Transient lifetimes
- **Post-Construction Initialization**: Execute methods after injection with `[PostConstruct]` attribute
- **Circular Dependency Detection**: Prevents infinite loops during resolution
- **Caching**: Reflection metadata and constructor info are cached for performance
- **Optional Logging**: Automatically integrates with registered `ILogger` implementations
- **Flexible Instance Registration**: Register pre-existing instances (e.g., MonoBehaviour from scene)

---

## Usage

### Basic Setup

```csharp
var container = new Container();

// Register services from assemblies
container.RegisterServices(new string[] { "MyNamespace.Services", "MyNamespace.Domain" });

// Register specific interface-implementation pair
container.Register<IRepository, UserRepository>(ServiceLifetime.Singleton);

// Register a pre-existing instance
container.RegisterInstance<IConfig>(configInstance);

// Resolve services
var userService = container.Resolve<IUserService>();
```

### Creating a Service

Services are discovered and registered automatically by marking classes with `[Service]`:

```csharp
[Service(ServiceLifetime.Singleton)]
public class UserService : IUserService
{
    [Inject]
    private ILogger _logger;

    [Inject]
    private IRepository _repository;

    [PostConstruct]
    private void Initialize()
    {
        _logger.Log("UserService initialized");
    }

    public void DoSomething() { }
}
```

### Creating a Logger

Implement `ILogger` and register it as a service:

```csharp
[Service(ServiceLifetime.Singleton)]
public class UnityDebugLogger : ILogger
{
    public void Log(string message) => Debug.Log(message);
    public void Warn(string message) => Debug.LogWarning(message);
    public void Error(string message) => Debug.LogError(message);
}
```

---

## Attributes

### `[Service]`
Marks a class for automatic registration during assembly scanning.

```csharp
[Service(ServiceLifetime.Singleton)]
public class MyService : IMyService { }
```

### `[Inject]`
Marks a field, property, or method for dependency injection.

```csharp
[Inject]
private ILogger _logger;

[Inject]
public IRepository Repository { get; set; }

[Inject]
public void Configure(IConfig config) { }
```

### `[PostConstruct]`
Marks a method to be invoked after all dependencies are injected.

```csharp
[PostConstruct]
private void Initialize()
{
    // Called after injection
}
```

---

## Common Issues and Solutions

### Issue 1: Registration Order with Instances

**Problem**: Registering instances with `RegisterInstance<T>` before their dependencies are registered will throw an exception, because dependency injection happens **immediately** when the instance is registered.

```csharp
// ❌ WRONG - ILogger and other deps not registered yet
container.RegisterInstance<HudController>(hudController);
container.Register<ILogger, UnityDebugLogger>(ServiceLifetime.Singleton);
```

**Solution**: Register all dependencies first, then register instances:

```csharp
// ✅ CORRECT - Register dependencies before instances
container.Register<ILogger, UnityDebugLogger>(ServiceLifetime.Singleton);
container.RegisterServices(...);
container.RegisterInstance<HudController>(hudController);
```

### Issue 2: Circular Dependencies

**Problem**: Two or more services depend on each other.

```csharp
[Service]
public class ServiceA
{
    [Inject] private IServiceB _serviceB;  // Depends on B
}

[Service]
public class ServiceB
{
    [Inject] private IServiceA _serviceA;  // Depends on A - CIRCULAR!
}
```

**Solution**: Redesign your architecture. Consider:
- Inverting the dependency direction
- Using a mediator or event system
- Splitting responsibilities

### Issue 3: Service Not Registered

**Problem**: Trying to resolve a service that was never registered or not included in scanned assemblies.

```csharp
// ❌ MyService doesn't have [Service] attribute
public class MyService { }

var service = container.Resolve<MyService>();  // Exception!
```

**Solution**:
- Add `[Service]` attribute to the class
- Or manually register it: `container.Register<IMyService, MyService>(ServiceLifetime.Singleton);`
- Ensure the assembly is included in `RegisterServices()`

### Issue 4: MonoBehaviour Dependencies

**Problem**: Trying to resolve MonoBehaviour instances created in the scene.

```csharp
// ❌ These won't be automatically discovered
public class UIController : MonoBehaviour { }
```

**Solution**: Register them as instances after all dependencies are registered:

```csharp
private void Awake()
{
    var container = new Container();
    container.Register<ILogger, UnityDebugLogger>(ServiceLifetime.Singleton);
    container.RegisterServices(...);
    
    // Now register MonoBehaviours from scene (after dependencies exist)
    var uiController = GetComponent<UIController>();
    container.RegisterInstance<UIController>(uiController);
}
```

### Issue 5: No Logger When Using Empty Constructor

**Problem**: Using `new Container()` without registering `ILogger` means internal logs are silent.

```csharp
var container = new Container();
// _logger is null, no logging occurs
```

**Solution**: Register an `ILogger` implementation early. The logger is automatically cached when `ILogger` is registered:

```csharp
var container = new Container();
container.Register<ILogger, UnityDebugLogger>(ServiceLifetime.Singleton);  // Logger cached here
container.RegisterServices(...);  // Now all subsequent registrations are logged
```

---

## Best Practices

### 1. Always Use Interfaces

Register and inject interfaces, not concrete implementations:

```csharp
// ✅ GOOD
container.Register<IUserService, UserService>(ServiceLifetime.Singleton);

// ❌ AVOID
container.Register<UserService, UserService>(ServiceLifetime.Singleton);
```

### 2. Use Appropriate Lifetimes

- **Singleton**: Stateless services, logger, config, repositories
- **Transient**: Services with state that should be independent per request

```csharp
[Service(ServiceLifetime.Singleton)]
public class ILogger { }

[Service(ServiceLifetime.Transient)]
public class RequestHandler { }
```

### 3. Register All Dependencies Upfront

Avoid registering dependencies during usage. Register everything in the initialization phase:

```csharp
// ✅ GOOD - All registered before usage
public void Initialize()
{
    container.RegisterServices(...);
    container.RegisterInstance<HudController>(hudController);
}

// ❌ AVOID - Registering during gameplay
public void LateRegisterSomething()
{
    container.Register<INewService, NewService>(ServiceLifetime.Singleton);
}
```

### 4. Use PostConstruct for Initialization Logic

Instead of complex constructors, use `[PostConstruct]` for initialization:

```csharp
[Service]
public class GameManager
{
    [Inject] private ILogger _logger;
    
    // ✅ GOOD - Simple constructor
    public GameManager() { }
    
    [PostConstruct]
    private void Initialize()
    {
        _logger.Log("GameManager initialized");
    }
}
```

---

## Example: Complete Setup in Unity

```csharp
public class GameInitializer : MonoBehaviour
{
    private Container _container;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Create container
        _container = new Container();

        // Register logger first (optional, enables logging during registration)
        _container.Register<ILogger, UnityDebugLogger>(ServiceLifetime.Singleton);

        // Scan assemblies for [Service] classes
        _container.RegisterServices(new string[]
        {
            "MyGame.Services",
            "MyGame.Domain",
            "MyGame.UI"
        });

        // Register scene-specific instances (after dependencies are available)
        _container.RegisterInstance<IGameConfig>(Resources.Load<GameConfig>("Config"));
        _container.RegisterInstance<HudController>(GetComponent<HudController>());
        _container.RegisterInstance<GameplayController>(GetComponent<GameplayController>());

        // Connect to ServiceLocator (optional)
        ServiceLocator.SetResolver(type => 
        {
            try { return _container.Resolve(type); }
            catch { return null; }
        });
        
        ServiceLocator.RegisterService(_container);
    }
}
```

---

## Performance Considerations

### Caching
The container caches:
- **Reflection metadata**: Field, property, and method injection info per type
- **Constructor info**: Selected constructor per type

This means the first resolution of a type is slower (reflection), but subsequent resolutions are fast.

### Best Practices for Performance
- Register services early in the initialization phase
- Avoid resolving types repeatedly in tight loops
- Use Singleton lifetime for stateless services
- Consider lazy initialization with factories for heavy services

---

## API Reference

### Methods

| Method | Purpose |
|--------|---------|
| `RegisterServices()` | Scan all loaded assemblies for `[Service]` classes and register them |
| `RegisterServices(string[])` | Scan specific assemblies by name for `[Service]` classes |
| `RegisterServices(Assembly[])` | Scan specific assemblies for `[Service]` classes |
| `Register<TService, TImplementation>()` | Manually register an interface-implementation pair |
| `Register<T>()` | Register a type as both service and implementation |
| `Register(Type, Type, ServiceLifetime)` | Register services using reflection (non-generic) |
| `RegisterInstance<T>(T instance)` | Register a pre-existing instance as a service (injects immediately) |
| `Resolve<T>()` | Resolve a service by generic type |
| `Resolve(Type)` | Resolve a service using reflection |
| `Inject(object)` | Manually inject dependencies into an object |

---

## Troubleshooting

### "Service of type X is not registered"
- Check if the class has `[Service]` attribute
- Ensure the assembly is included in `RegisterServices()`
- Check spelling and namespace

### "Circular dependency detected: X"
- Review the dependency graph of your services
- Refactor to break the cycle (see Issue 2 above)

### "No public constructor found for type X"
- Ensure the type has at least one public constructor
- Or mark a constructor with `[Inject]` to use it explicitly

### PostConstruct methods not called
- Ensure methods are marked with `[PostConstruct]`
- They must be public or private (not protected)
- Methods are called after all injection is complete
- They receive no parameters and should return void

### Logger not logging
- Ensure `ILogger` is registered before calling other registrations
- The logger is automatically cached when `ILogger` is registered
- If no `ILogger` is registered, logging is silently skipped (no errors)

---

## License

Part of the Paynob.Patterns library.
