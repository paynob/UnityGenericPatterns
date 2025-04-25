namespace Paynob.Patterns.Pooling
{
	using System.Collections.Generic;

	using UnityEngine;

	public class PoolManager
	{
		private static PoolManager _instance;

		private Transform transform;
		private PoolManager( Transform parentOfAllPools ) {
			transform = parentOfAllPools;
		}

		public static PoolManager Instance {
			get {
				if( null == _instance ) {
					var go = new GameObject( "---=== PoolManager ===---" );
					_instance = new PoolManager( go.transform );
				}
				return _instance;
			}
		}

		private readonly Dictionary<MonoBehaviour , IPool> pools = new Dictionary<MonoBehaviour , IPool>( );


		public Pool<T> GetPoolOf<T>( T prefab, int initialSize = 10 ) where T : MonoBehaviour {
			if( !pools.ContainsKey( prefab ) ) {
				var poolOfT = new GameObject( $"-==={typeof( T )} {prefab.name} Pool===-" );
				poolOfT.transform.SetParent( this.transform );
				poolOfT.transform.localPosition = Vector3.zero;
				var newPool = new Pool<T>( poolOfT.transform , prefab );
				pools.Add( prefab , newPool );
				if ( initialSize > 0 ){
					newPool.Prewarm( initialSize );
				}
			}
			return ((Pool<T>)pools [ prefab ]);
		}

		public void DestroyAll() {
			foreach( var pool in pools ) {
				pool.Value.DestroyAll( );
			}
		}
	}
}
