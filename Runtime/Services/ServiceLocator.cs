
namespace Paynob.Patterns.Services
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public static class ServiceLocator
	{
		private static readonly Dictionary<Type , object> services = new( );
		
		public static bool RegisterService<T>( T service , bool overrides = true ) where T : class {
			UnityEngine.Debug.Log($"Registering service of type ({typeof( T ).Name}){service.GetType().Name} with overrides: {overrides}");
			if( !overrides && services.ContainsKey( typeof( T ) ) ) {
				return false;
			}
			services [ typeof( T ) ] = service;
			return true;
		}
		public static bool RegisterService<T, S>( bool overrides = true ) where T : class where S : T, new() {
			if (!typeof(T).IsAssignableFrom(typeof(S)))
			{
				throw new InvalidOperationException($"Type {typeof(S).Name} is not assignable to {typeof(T).Name}");
			}
			return RegisterService<T>( new S( ) , overrides );
		}

		public static bool RegisterService(string typeName, string superTypeName, bool overrides = true)
		{

			var type = Type.GetType(typeName);
			var superType = Type.GetType(superTypeName);

			if( !overrides && services.ContainsKey( superType ) ) {
				return false;
			}
			if (type == null || superType == null)
			{
				throw new InvalidOperationException($"Type {typeName} or {superTypeName} not found.");
			}

			if (!superType.IsAssignableFrom(type))
			{
				throw new InvalidOperationException($"Type {type.Name} is not assignable to {superType.Name}");
			}

			return RegisterService( type,superType, overrides);
		}

		public static bool RegisterService( Type type , Type baseType , bool overrides = true ) {
			if( !overrides && services.ContainsKey( baseType ) ) {
				return false;
			}
			if ( baseType == null || !baseType.IsAssignableFrom( type ) ) {
				throw new InvalidOperationException( $"Type {type.Name} is not assignable to {baseType.Name}" );
			}
			services [ baseType ] = Activator.CreateInstance( type );
			UnityEngine.Debug.Log($"Registering service of type ({baseType.Name}){type.Name} with overrides: {overrides}");
			return true;
		}

		public static void UnregisterService( Type type ) {
			if( services.TryGetValue( type , out var service ) ) {
				if( service is IDisposable disposable ) {
					disposable.Dispose( );
				}
				
				services.Remove( type );
			}
		}
		public static void UnregisterService<T>() where T : class => UnregisterService( typeof( T ) );

		public static T GetService<T>() where T : class {
			var type = typeof( T );

			if( !services.TryGetValue( type , out var service ) ) {
				UnityEngine.Debug.LogWarning($"Service of type {type.Name} is not registered. Returning a null service.");
        		service = CreateNullService<T>( );
				services [ type ] = service;
			}

			return (T)service;
		}

		public static Dictionary<Type, string> 	GetRegisteredServices( ) {
			var registeredServices = new Dictionary<Type, string>( );
			foreach( var service in services ) {
				registeredServices.Add( service.Key , service.Value.GetType( ).Name );
			}
			return registeredServices;
		}

		public static void LogRegisteredServices()
		{
			foreach (var service in services)
			{
				UnityEngine.Debug.Log($"Service: {service.Key.Name}, Implementation: {service.Value.GetType().Name}");
			}
		}
		public static bool IsServiceRegistered<T>() where T : class {
			return services.ContainsKey( typeof( T ) );
		}
		public static bool IsServiceRegistered( Type type ) {
			return services.ContainsKey( type );
		}
		public static void Clear( ) {
			foreach( var service in services ) {
				if( service.Value is IDisposable disposable ) {
					disposable.Dispose( );
				}
			}
			services.Clear( );
		}

		public static async Task<T> GetServiceAsync<T>() where T : class
		{
			var service = GetService<T>();

			if (service is IAsyncInitializable asyncInitializable)
			{
				await asyncInitializable.InitializeAsync();
			}

			return service;
		}

		public interface IAsyncInitializable
		{
			Task InitializeAsync();
		}

		private static T CreateNullService<T>() where T : class
		{
			return Activator.CreateInstance(typeof(NullService<>).MakeGenericType(typeof(T))) as T;
		}

		private class NullService<T> : INullService where T : class
		{
		}

		public interface INullService {}

		public static bool IsNullService<T>( T service ) where T : class {
			return service is INullService;
		}
	}
}