using UnityEngine;
using Paynob.Patterns.DI;
using Paynob.Patterns.Services;

namespace Paynob.Patterns.Samples.DI
{
    // -------------------------------------------------------------------------
    // 1. Interface Definitions
    // -------------------------------------------------------------------------
    public interface ILogService
    {
        void Log(string message);
    }

    public interface IScoreCalculator
    {
        int CalculateScore(int basePoints, float multiplier);
    }

    // -------------------------------------------------------------------------
    // 2. Service Implementations (Marked with [Service])
    // -------------------------------------------------------------------------

    // Singleton service example: A single shared instance is created for the entire application.
    [Service(lifetime: ServiceLifetime.Singleton)]
    public class ConsoleLogService : ILogService
    {
        public ConsoleLogService()
        {
            Debug.Log("ConsoleLogService Created (Singleton)");
        }

        public void Log(string message)
        {
            Debug.Log($"[LogService]: {message}");
        }
    }

    // Transient service example: A new instance is created each time it is requested or injected.
    [Service(lifetime: ServiceLifetime.Transient)]
    public class SimpleScoreCalculator : IScoreCalculator
    {
        public SimpleScoreCalculator()
        {
            Debug.Log("SimpleScoreCalculator Created (Transient)");
        }

        public int CalculateScore(int basePoints, float multiplier)
        {
            return Mathf.FloorToInt(basePoints * multiplier);
        }
    }

    // -------------------------------------------------------------------------
    // 3. Service Consumer (Injection Demonstration)
    // -------------------------------------------------------------------------

    // This class demonstrates the 3 types of injection supported by Container.
    [Service(lifetime: ServiceLifetime.Transient)]
    public class GameMatchManager
    {
        private readonly ILogService _logService;
        
        // Property Injection (Requires setter and [Inject] attribute)
        [Inject]
        public IScoreCalculator ScoreCalculator { get; private set; }

        // Field Injection (Private or Public, marked with [Inject])
        [Inject]
        private ILogService _secondaryLoggerReference; 

        // Constructor Injection (Recommended)
        // The container automatically resolves and injects parameters.
        public GameMatchManager(ILogService logService)
        {
            _logService = logService;
        }

        public void EndMatch(int points)
        {
            _logService.Log("Ending match...");
            
            // Using the property-injected service
            int finalScore = ScoreCalculator.CalculateScore(points, 1.5f);
            
            _logService.Log($"Match ended. Final Score: {finalScore}");
            
            // _secondaryLoggerReference points to the same Singleton instance as _logService
        }
    }

    // -------------------------------------------------------------------------
    // 4. Example usage with MonoBehaviour (Without Automatic Injection)
    // -------------------------------------------------------------------------
    
    public class PlayerHUD : MonoBehaviour
    {
        private IScoreCalculator _scoreCalculator;

        void Start()
        {
            // Since Unity instantiates MonoBehaviours, we use ServiceLocator to obtain dependencies.
            // This works because DependencyInitializer connects the container to ServiceLocator.
            _scoreCalculator = ServiceLocator.GetService<IScoreCalculator>();
            
            if (_scoreCalculator != null)
            {
                int previewScore = _scoreCalculator.CalculateScore(100, 1.0f);
                Debug.Log($"HUD Initialized. Preview Score: {previewScore}");
            }
            else
            {
                Debug.LogWarning("IScoreCalculator not found in ServiceLocator");
            }
        }
    }

    // -------------------------------------------------------------------------
    // 5. Example of how to "instantiate" (Resolve) GameMatchManager
    // -------------------------------------------------------------------------
    
    public class GameBootstrapper : MonoBehaviour
    {
        void Start()
        {
            // INCORRECT:
            // var manager = new GameMatchManager(new ConsoleLogService()); 
            // If you do this, you lose automatic property injection (ScoreCalculator would be null).

            // CORRECT:
            // Request the instance from the ServiceLocator.
            // 1. The system looks for GameMatchManager in the container.
            // 2. It sees it needs ILogService and creates/finds it.
            // 3. It sees it needs to inject ScoreCalculator (property) and resolves it.
            var matchManager = ServiceLocator.GetService<GameMatchManager>();
            
            matchManager.EndMatch(999);
        }
    }

    // -------------------------------------------------------------------------
    // 6. "Pure DI" / Composition Root approach (Avoiding global ServiceLocator)
    // -------------------------------------------------------------------------
    // This approach centralizes dependency resolution at a single point (Installer),
    // keeping consumer classes clean and easy to test.

    // A. "Clean" Component: Does not use [Inject] attributes or ServiceLocator.
    // Receives its dependencies explicitly via method. It is the most robust approach.
    public class EnemyUnit : MonoBehaviour
    {
        private IScoreCalculator _calculator;
        private ILogService _logger;

        // Manual initialization method
        public void Construct(IScoreCalculator calculator, ILogService logger)
        {
            _calculator = calculator;
            _logger = logger;
            
            _logger.Log("EnemyUnit initialized manually via Construct().");
        }
    }

    // B. "Injectable" Component: Uses [Inject] to reduce repetitive code, 
    // but does not search for dependencies itself. It waits passively.
    public class SmartTurret : MonoBehaviour
    {
        [Inject] private ILogService _logger;

        // Method Injection: The container will call this automatically when calling .Inject()
        [Inject]
        public void Init(IScoreCalculator calculator)
        {
            _logger?.Log("SmartTurret initialized via Container.Inject().");
        }
    }

    // C. Scene Installer: The ONLY script in the scene that knows the ServiceLocator.
    // It handles wiring the dependencies at the start.
    public class GameSceneInstaller : MonoBehaviour
    {
        public EnemyUnit enemy;
        public SmartTurret turret;

        void Start()
        {
            // 1. Get the container (automatically registered in DependencyInitializer)
            var container = ServiceLocator.GetService<Container>();

            // Option A: Manual Configuration (Pure DI) - Total control, no magic.
            if (enemy) enemy.Construct(container.Resolve<IScoreCalculator>(), container.Resolve<ILogService>());

            // Option B: Automatic Injection (Reflection) - Faster to write.
            if (turret) container.Inject(turret);
        }
    }
}