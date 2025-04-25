# Service Locator

The **Service Locator** pattern provides a centralized registry for managing and accessing services in your application. This implementation is designed to be flexible, robust, and easy to use, supporting features like dynamic service registration, asynchronous initialization, and the null service pattern.

---

## Features

- **Dynamic Service Registration:**
  - Register services by type, instance, or dynamically using type names.
  - Support for overriding existing services.

- **Asynchronous Initialization:**
  - Services can implement `IAsyncInitializable` to support asynchronous initialization.

- **Null Service Pattern:**
  - Automatically returns a `NullService` for unregistered services to avoid null reference exceptions.

- **Service Management:**
  - Unregister services, clear all services, and check if a service is registered.
  - List and log all registered services for debugging.

---

## Usage

### Registering Services
You can register services in multiple ways:

1. **Register an instance:**
   ```csharp
   var myService = new MyService();
   ServiceLocator.RegisterService<IMyService>(myService);
   ```
2. **Registering a type with default constructor:**
   ```csharp
   ServiceLocator.RegisterService<IMyService, MyService>();
   ```


## Null Service Pattern:
If a service is not registered, the ServiceLocator will return a NullService instead of null. This avoids null reference exceptions and allows you to handle missing services gracefully.

1. **Example:**
    ```csharp
    var myService = ServiceLocator.GetService<IMyService>();
    if (ServiceLocator.IsNullService(myService))
    {
        Debug.LogWarning("Using a null service for IMyService.");
    }
    ```

# ServicesInitializer

The ServicesInitializer class allows you to register services dynamically from a configuration file. The configuration file must have a .txt extension and use an INI-like format.

Example Configuration File (services.txt):
~~~
[ScoreService]
BaseAssembly=Paynob.Patterns.Samples
BaseName=Paynob.Patterns.Samples.Services.IScoreService
ImplAssembly=Paynob.Patterns.Samples
ImplName=Paynob.Patterns.Samples.Services.PlayerPrefsScoreService
;Overrides=true (true by default)

[LoggingService]
BaseAssembly=Paynob.Patterns.Logging
BaseName=Paynob.Patterns.Logging.ILoggingService
ImplAssembly=Paynob.Patterns.Logging
ImplName=Paynob.Patterns.Logging.ConsoleLoggingService
Overrides=false
~~~

Registering Services from Configuration:
The ServicesInitializer reads the configuration file and registers the services automatically:
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void OnBeforeSceneLoadRuntimeMethod()
{
    ServicesInitializer.Initialize();
}

Debugging:
Use the following methods to debug the state of the ServiceLocator:
- ServiceLocator.LogRegisteredServices(): Logs all registered services.
- ServiceLocator.IsServiceRegistered<T>(): Checks if a specific service is registered.
- ServiceLocator.IsNullService<T>(): Checks if a service is a null service.

License:
This project is licensed under the MIT License. See the LICENSE file for details.
