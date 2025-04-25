namespace Paynob.Patterns.Samples.Services
{
    using UnityEngine;
    using Paynob.Patterns.Services;

/// You can use this class to register services in the ServiceLocator at runtime.
/// Also you use a configuration file to register services in the ServiceLocator at runtime.
/// 
/// You may place the configuration file (services.txt) in the Resources folder of your project.

    public class ServicesInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            Debug.Log("ServicesInitializer: OnBeforeSceneLoadRuntimeMethod");
            #if !UNITY_EDITOR
            ServiceLocator.RegisterService<IScoreService, PlayerPrefsScoreService>(false);
            #else
            ServiceLocator.RegisterService<IScoreService, FirebaseScoreService>(false);
            #endif

            // Register other services here if needed
            // ServiceLocator.RegisterService<IOtherService, OtherService>(true);
        }
    }
}