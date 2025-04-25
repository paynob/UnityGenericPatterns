namespace Paynob.Patterns.Pooling
{
	public interface ISpawnable
	{
		void Init( params object [ ] parameters );
		void OnDespawn();
	}
}