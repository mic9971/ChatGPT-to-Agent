# Dependency Injection Conventions

## Composition root owns wiring

Core/application code defines contracts; Host/CLI composition registers concrete implementations. Do not resolve dependencies manually inside business logic.

## Registration by capability

As registrations grow, expose cohesive registration methods such as:

```text
AddWorkspace(...)
AddExecutionEvidence(...)
AddTunnel(...)
AddAuthorization(...)
```

rather than allowing one giant registration file to become an unstructured list.

Keep provider-specific registration close to the provider/capability. The final composition root may call those registration methods.

## No service locator

Do not inject `IServiceProvider` to fetch arbitrary dependencies during normal business execution. Do not call `BuildServiceProvider()` while registering services.

## Lifetime ownership

Choose lifetime from behavior/state ownership, not habit. Mutable singletons must be explicitly thread-safe. DI-created disposables are disposed by the container.

## Options

Strongly typed options live near the capability/provider that consumes them and are validated at startup when invalid configuration would make the runtime unsafe or unusable.

Do not create one giant `AppSettings` model spanning unrelated capabilities.