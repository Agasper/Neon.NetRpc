using System.Collections.Generic;

namespace Neon.ClientExample.Net.Util
{
    public class ModelProperty<T> : IReadOnlyModelProperty<T>
    {
        public T Value
        {
            get => value;
            set => Update(value);
        }

        GameEvent<T> onPropertyChanged;
        GameEvent<T, T> onPropertyChangedExtra;
        T value;

        public ModelProperty()
        {
            onPropertyChanged = new GameEvent<T>();
            onPropertyChangedExtra = new GameEvent<T, T>();
        }
        
        public ModelProperty(T defaultValue) : this()
        {
            this.value = defaultValue;
        }

        public void AddListener(GameAction<T> callback, bool autoRaise = false)
        {
            if (autoRaise)
                callback.Invoke(Value);
            this.onPropertyChanged.AddListener(callback);
        }
        
        public void AddListener(GameAction<T, T> callback, bool autoRaise = false)
        {
            if (autoRaise)
                callback.Invoke(Value, Value);
            this.onPropertyChangedExtra.AddListener(callback);
        }
        
        public void RemoveListener(GameAction<T> callback)
        {
            this.onPropertyChanged.RemoveListener(callback);
        }
        
        public void RemoveListener(GameAction<T, T> callback)
        {
            this.onPropertyChangedExtra.RemoveListener(callback);
        }

        public void RemoveAllListeners()
        {
            this.onPropertyChanged.RemoveAllListeners();
            this.onPropertyChangedExtra.RemoveAllListeners();
        }

        void Update(T newValue)
        {
            if (newValue == null && this.value == null)
                return;
            if (this.value != null && this.value.Equals(newValue))
                return;
            var oldValue = this.value;
            this.value = newValue;
            this.onPropertyChanged.Invoke(newValue);
            this.onPropertyChangedExtra.Invoke(newValue, oldValue);
        }
    }
}