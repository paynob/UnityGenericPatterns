using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Paynob.Patterns.Services
{
	using UnityEngine;
    
	/// <summary>
    /// Responsible for initializing services in the <see cref="ServiceLocator"/>.
    /// This class reads configuration files from the <c>Resources</c> folder
    /// and registers the services defined in those files.
    /// 
    /// The configuration files must have a <c>.txt</c> extension but use an INI-like format internally.
    /// Each section in the file represents a service to be registered, specifying its base type
    /// and implementation type.
    /// </summary>
    /// <example>
    /// Example configuration file (<c>services.txt</c>):
    /// <code>
    /// [ScoreService]
    /// BaseAssembly=Paynob.Patterns.Samples
    /// BaseName=Paynob.Patterns.Samples.Services.IScoreService
    /// ImplAssembly=Paynob.Patterns.Samples
    /// ImplName=Paynob.Patterns.Samples.Services.PlayerPrefsScoreService
    /// ;Overrides=true (true by default)
    /// 
    /// [LoggingService]
    /// BaseAssembly=Paynob.Patterns.Logging
    /// BaseName=Paynob.Patterns.Logging.ILoggingService
    /// ImplAssembly=Paynob.Patterns.Logging
    /// ImplName=Paynob.Patterns.Logging.ConsoleLoggingService
    /// Overrides=false
    /// </code>
    /// </example>
	public class ServicesInitializer
	{

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            TextAsset[] configFiles = Resources.LoadAll<TextAsset>("services");

            if (configFiles == null || configFiles.Length == 0)
            {
                Debug.LogWarning("No configuration files found in Resources/services folder.");
                return;
            }

            foreach (var configFile in configFiles)
            {
                Debug.Log($"Reading config file: {configFile.name}");
                
                var sections = Paynob.Patterns.Utils.IniUtils.ReadIniText(configFile.text);
                
                if (sections == null || sections.Count == 0)
                {
                    Debug.LogError($"No sections found in config file: {configFile.name}");
                    continue;
                }

                foreach( var section in sections ){
                    Debug.Log($"Section: {section.Key}");
                    var properties = section.Value;
                    var implAssembly = properties.ContainsKey("ImplAssembly") ? ", " + properties["ImplAssembly"] : "";
                    
                    var typeString = $"{properties["ImplName"]}{implAssembly}";
                    var type = Type.GetType(typeString);

                    if (type == null)
                    {
                        Debug.LogError($"Type {typeString} not found.");
                        continue;
                    }
                    var baseAssembly = properties.ContainsKey("BaseAssembly") ? ", " + properties["BaseAssembly"] : "";

                    var superTypeString = $"{properties["BaseName"]}{baseAssembly}";
                    var superType = Type.GetType(superTypeString);

                    if (superType == null)
                    {
                        Debug.LogError($"Base type {superTypeString} not found.");
                        continue;
                    }
                    if (!superType.IsAssignableFrom(type))
                    {
                        Debug.LogError($"Type {typeString} is not assignable to base type {superTypeString}.");
                        continue;
                    }
                    if (type == null)
                    {
                        Debug.LogError($"Type {typeString} not found.");
                        continue;
                    }

                    var overrides = properties.GetValueOrDefault("Overrides", "true").ToLower() == "true";
                    
                    ServiceLocator.RegisterService(type, superType, overrides);
                }
            }
        }
	}
}
