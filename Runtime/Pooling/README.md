# Object Pooling Pattern

## Pool&lt;T&gt; class
**Methods**
```csharp
// If T implements ISpawnable, you can pass initBeforeEnable and parameters to Spawn method
// If you pass it, but T does notimplements ISpawnable, the method ignores it,
public T Spawn( Vector3 position , Quaternion rotation , Transform parent = null, bool initBeforeEnable = true , params object [ ] parameters);

// Prewarm the Pool with disabled objects
public void Prewarm (int amount );

// Fake destroy the element o
public void Destroy( T o );

// Fake destroy all elements of this pull
public void DestroyAll();
```



## Usage

```csharp
[SerializeField] 
private Enemy _enemyPrefab;

private int currentLevel = 0;

private Pool<Enemy> _enemiesPool;

void Start(){
    _enemiesPool = PoolManager.Instance.GetPoolOf( _enemyPrefab );
    StartCoroutine( StartNewLevel() );
}

private IEnumerator StartNewLevel(){
    currentLevel ++;
    
    for ( var i = 0; i< currentLevel; i++ ){
        var position = Random.insideUnitCircle.normalized * 10f;
        var enemy = //Instantiate( _enemyPrefab , position , Quaternion.identity );
			_enemiesPool.Spawn( position , Quaternion.identity , null , true, _enemyPrefabSpeed , _enemyPrefab.transform.localScale );
        yield return new WaitForSeconds( 0.2f );
    }
}

public void EndLevel(){
    _enemiesPool.DestroyAll();
}
```
