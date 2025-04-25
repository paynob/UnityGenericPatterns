namespace Paynob.Patterns.Pooling
{
	using System.Collections.Generic;

	using UnityEngine;

	public class Pool<T> : IPool, IEnumerable<T> where T : MonoBehaviour
	{

		Transform _transform;
		T _prefab;

		private readonly HashSet<T> _enabledObjects = new HashSet<T>( );
		Stack<T> _disabledObjects = new Stack<T>( );

		public IEnumerator<T> GetEnumerator() => _enabledObjects.GetEnumerator( );

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_enabledObjects).GetEnumerator( );
		internal Pool( Transform t , T prefab ) {
			_transform = t;
			_prefab = prefab;
		}

		private T GetFromPoolOrCreate( Vector3 position , Quaternion rotation ) {
			T result;
			if( _disabledObjects.Count > 0 ) {
				result = _disabledObjects.Pop( );
				result.transform.SetPositionAndRotation( (Vector2)position , rotation );
			} else {
				result = GameObject.Instantiate( _prefab , (Vector2)position , rotation );
			}

			_enabledObjects.Add( result );

			return result;
		}

		private T _Spawn( Vector3 position , Quaternion rotation , Transform parent = null ) {
			T toSpawn = GetFromPoolOrCreate( position , rotation );

			if( parent == null ) {
				parent = _transform;
			}
			toSpawn.transform.SetParent( parent );

			return toSpawn;
		}

		private void _FakeDestroy( T o ) {
			o.gameObject.SetActive( false );
			o.transform.SetParent( _transform );

			_enabledObjects.Remove( o );
			_disabledObjects.Push( o );
		}

		public T Spawn( Vector3 position , Quaternion rotation , Transform parent = null, bool initBeforeEnable = true , params object [ ] parameters ) {
			T toSpawn = _Spawn( position , rotation , parent );
			if( initBeforeEnable && toSpawn is ISpawnable sp )
				sp.Init( parameters );

			toSpawn.gameObject.SetActive( true );

			if( !initBeforeEnable && toSpawn is ISpawnable sp2 )
				sp2.Init( parameters );

			return toSpawn;
		}

		public void Prewarm(int count) {
			if (_disabledObjects.Count >= count) return; // Ya hay suficientes

			int needed = count - _disabledObjects.Count;
			for (int i = 0; i < needed; i++) {
				T instance = GameObject.Instantiate(_prefab, _transform); 
				instance.gameObject.SetActive(false); // Empezar desactivado
				_disabledObjects.Push(instance);// Nota: No se añade a _enabledObjects
			}
		}

		public void Destroy( T o ) {
			_FakeDestroy( o );
			if( o is ISpawnable s )
				s.OnDespawn( );
		}

		public void DestroyAll() {
			foreach( var o in _enabledObjects ) {
				o.gameObject.SetActive(false);
				o.transform.SetParent(_transform);
				_disabledObjects.Push(o);
				
				if (o is ISpawnable s) 
					s.OnDespawn();
			}
			_enabledObjects.Clear( );
		}
	}
}
