using UnityEngine;
using Paynob.Patterns.DI;
using Paynob.Patterns.Services;

namespace Paynob.Patterns.Samples.DI
{
    // -------------------------------------------------------------------------
    // 1. Definición de Interfaces
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
    // 2. Implementación de Servicios (Marcados con [Service])
    // -------------------------------------------------------------------------

    // Ejemplo de un servicio Singleton: Se crea una sola instancia compartida para toda la aplicación.
    [Service(ServiceLifetime.Singleton)]
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

    // Ejemplo de un servicio Transient: Se crea una nueva instancia cada vez que se solicita o inyecta.
    [Service(ServiceLifetime.Transient)]
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
    // 3. Consumidor de Servicios (Demostración de Inyección)
    // -------------------------------------------------------------------------

    // Esta clase demuestra los 3 tipos de inyección soportados por DependencyContainer.
    [Service(ServiceLifetime.Transient)]
    public class GameMatchManager
    {
        private readonly ILogService _logService;
        
        // Inyección de Propiedad (Requiere setter y atributo [Inject])
        [Inject]
        public IScoreCalculator ScoreCalculator { get; private set; }

        // Inyección de Campo (Privado o Público, marcado con [Inject])
        [Inject]
        private ILogService _secondaryLoggerReference; 

        // Inyección por Constructor (Recomendado)
        // El contenedor resuelve e inyecta automáticamente los parámetros.
        public GameMatchManager(ILogService logService)
        {
            _logService = logService;
        }

        public void EndMatch(int points)
        {
            _logService.Log("Ending match...");
            
            // Usando el servicio inyectado por propiedad
            int finalScore = ScoreCalculator.CalculateScore(points, 1.5f);
            
            _logService.Log($"Match ended. Final Score: {finalScore}");
            
            // _secondaryLoggerReference apunta a la misma instancia Singleton que _logService
        }
    }

    // -------------------------------------------------------------------------
    // 4. Ejemplo de uso con MonoBehaviour (Sin Inyección Automática)
    // -------------------------------------------------------------------------
    
    public class PlayerHUD : MonoBehaviour
    {
        private IScoreCalculator _scoreCalculator;

        void Start()
        {
            // Como Unity instancia los MonoBehaviours, usamos ServiceLocator para obtener las dependencias.
            // Esto funciona gracias a que DependencyInitializer conecta el contenedor con ServiceLocator.
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
    // 5. Ejemplo de cómo "instanciar" (Resolver) GameMatchManager
    // -------------------------------------------------------------------------
    
    public class GameBootstrapper : MonoBehaviour
    {
        void Start()
        {
            // INCORRECTO:
            // var manager = new GameMatchManager(new ConsoleLogService()); 
            // Si haces esto, pierdes la inyección automática de propiedades (ScoreCalculator sería null).

            // CORRECTO:
            // Pides la instancia al ServiceLocator.
            // 1. El sistema busca GameMatchManager en el contenedor.
            // 2. Ve que necesita ILogService y lo crea/busca.
            // 3. Ve que necesita inyectar ScoreCalculator (propiedad) y lo resuelve.
            var matchManager = ServiceLocator.GetService<GameMatchManager>();
            
            matchManager.EndMatch(999);
        }
    }

    // -------------------------------------------------------------------------
    // 6. Enfoque "Pure DI" / Composition Root (Evitando ServiceLocator global)
    // -------------------------------------------------------------------------
    // Este enfoque centraliza la resolución de dependencias en un solo punto (Installer),
    // manteniendo las clases consumidoras limpias y fáciles de testear.

    // A. Componente "Limpio": No usa atributos [Inject] ni ServiceLocator.
    // Recibe sus dependencias explícitamente vía método. Es el enfoque más robusto.
    public class EnemyUnit : MonoBehaviour
    {
        private IScoreCalculator _calculator;
        private ILogService _logger;

        // Método de inicialización manual
        public void Construct(IScoreCalculator calculator, ILogService logger)
        {
            _calculator = calculator;
            _logger = logger;
            
            _logger.Log("EnemyUnit initialized manually via Construct().");
        }
    }

    // B. Componente "Inyectable": Usa [Inject] para reducir código repetitivo, 
    // pero no busca las dependencias por sí mismo. Espera pasivamente.
    public class SmartTurret : MonoBehaviour
    {
        [Inject] private ILogService _logger;

        // Inyección por Método: El contenedor llamará a esto automáticamente al hacer .Inject()
        [Inject]
        public void Init(IScoreCalculator calculator)
        {
            _logger?.Log("SmartTurret initialized via Container.Inject().");
        }
    }

    // C. Scene Installer: El ÚNICO script en la escena que conoce el ServiceLocator.
    // Se encarga de cablear las dependencias al inicio.
    public class GameSceneInstaller : MonoBehaviour
    {
        public EnemyUnit enemy;
        public SmartTurret turret;

        void Start()
        {
            // 1. Obtenemos el contenedor (registrado automáticamente en DependencyInitializer)
            var container = ServiceLocator.GetService<DependencyContainer>();

            // Opción A: Configuración Manual (Pure DI) - Control total, sin magia.
            if (enemy) enemy.Construct(container.Resolve<IScoreCalculator>(), container.Resolve<ILogService>());

            // Opción B: Inyección Automática (Reflexión) - Más rápido de escribir.
            if (turret) container.Inject(turret);
        }
    }
}