namespace Paynob.Patterns.DI
{
    public abstract class ScriptableObjectInstaller : UnityEngine.ScriptableObject, ILocalInstaller
    {
        public abstract void Install(Container container);
    }
}