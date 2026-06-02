using UnityEngine;

namespace Paynob.Patterns.DI
{
    public abstract class MonoLocalInstaller : MonoBehaviour, ILocalInstaller
    {
        public abstract void Install(Container container);
    }
}