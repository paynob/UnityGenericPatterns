using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Paynob.Patterns.DI
{
    public class DefaultSceneInstaller : ISceneInstaller
    {
        [Inject] private ILogger _logger;
        [Inject] private Container _container;

        private readonly Dictionary<Scene, Container> _sceneContainers = new();

        [PostConstruct]
        public void Initialize()
        {
            _logger?.Warn("Installing dependencies in the current scene");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (SceneManager.sceneCount > 1)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded )
                    {
                        OnSceneLoaded(scene, LoadSceneMode.Additive);
                    }
                }
            }
            else
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded )
                {
                    OnSceneLoaded(activeScene, LoadSceneMode.Single);
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (_sceneContainers.TryGetValue(scene, out var container))
            {
                container.Dispose();
                _sceneContainers.Remove(scene);
                _logger?.Log($"Scene container removed for: {scene.name}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_sceneContainers.ContainsKey(scene)) return;
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
    
            _logger?.Log($"Installing dependencies in the scene: {scene.name}");

            var containerName = $"{scene.name}{scene.buildIndex}Container";
            var sceneContainer = _container.CreateChildContainer(containerName);
            _sceneContainers[scene] = sceneContainer;
            sceneContainer.RegisterInstance(sceneContainer, name: containerName);

            InstallLocalDependencies(scene, sceneContainer);
            InjectDependenciesToSceneObjects(scene, sceneContainer);

            SceneManager.SetActiveScene(activeScene);
            _logger?.Log($"End of dependency injection in {scene.name}");
        }

        private static void InstallLocalDependencies(Scene scene, Container sceneContainer)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var installers = root.GetComponentsInChildren<ILocalInstaller>(true);
                foreach (var installer in installers)
                {
                    installer.Install(sceneContainer);
                }
            }
        }

        private static void InjectDependenciesToSceneObjects(Scene scene, Container sceneContainer)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var injectables = root.GetComponentsInChildren<IInjectable>(true);
                foreach (var injectable in injectables)
                {
                    Debug.Log($"Injecting dependencies to {root.name}: {injectable.GetType().FullName}");
                    sceneContainer.Inject(injectable);
                }
            }
        }
    }
}