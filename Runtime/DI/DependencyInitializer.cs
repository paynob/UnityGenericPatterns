using UnityEngine;
using System;
using Paynob.Patterns.Services;

namespace Paynob.Patterns.DI
{
    /// <summary>
    /// Responsible for initializing the dependency injection container.
    /// This class scans assemblies for classes marked with the <see cref="ServiceAttribute"/>
    /// and registers them in the <see cref="DependencyContainer"/>.
    /// </summary>
    public static class DependencyInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            var dependencyContainer = new DependencyContainer();
            dependencyContainer.RegisterServices();
            
            // Conectamos el Container al ServiceLocator
            ServiceLocator.SetResolver((type) => 
            {
                try { return dependencyContainer.Resolve(type); }
                catch { return null; }
            });

            // Opcional: Registrar el contenedor mismo por si alguien lo necesita explícitamente
            ServiceLocator.RegisterService(dependencyContainer);
        }
    }
}