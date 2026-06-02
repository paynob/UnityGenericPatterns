using UnityEngine;
using Paynob.Patterns.DI;
using Paynob.Patterns.Services;

namespace Paynob.Patterns.Samples.DI
{
    /// <summary>
    /// Responsible for initializing the dependency injection container.
    /// This class scans assemblies for classes marked with the <see cref="ServiceAttribute"/>
    /// and registers them in the <see cref="Container"/>.
    /// </summary>
    public static class DependencyInitializer
    {
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // static void OnBeforeSceneLoadRuntimeMethod()
        // {
        //     var container = new Container();
        //     container.RegisterServices();
            
        //     // Optional
        //     ServiceLocator.SetResolver((type) => 
        //     {
        //         try { return container.Resolve(type); }
        //         catch { return null; }
        //     });

        //     // Optional
        //     ServiceLocator.RegisterService(container);
        // }
    }
}