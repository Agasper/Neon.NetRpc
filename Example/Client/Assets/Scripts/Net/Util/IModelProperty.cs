namespace Neon.ClientExample.Net.Util
{
    public interface IReadOnlyModelProperty<T>
    { 
        T Value { get; }

        void AddListener(GameAction<T> callback, bool autoRaise = false);
        void RemoveListener(GameAction<T> callback);
        
        void AddListener(GameAction<T, T> callback, bool autoRaise = false);
        void RemoveListener(GameAction<T, T> callback);
    }
}