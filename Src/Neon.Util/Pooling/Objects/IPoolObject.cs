namespace Neon.Util.Pooling.Objects
{
    public interface IPoolObject
    {
        void OnTookFromPool();
        void OnReturnToPool();
    }
}
