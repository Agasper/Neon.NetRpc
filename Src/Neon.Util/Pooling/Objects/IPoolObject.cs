namespace Neon.Util.Pooling.Objects
{
    public interface IPoolObject
    {
        /// <summary>
        /// Event raised when object is taken from the pool
        /// </summary>
        void OnTookFromPool();
        
        /// <summary>
        /// Event raised when object returns to the pool
        /// </summary>
        void OnReturnToPool();
    }
}
