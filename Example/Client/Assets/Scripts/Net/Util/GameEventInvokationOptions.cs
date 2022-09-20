namespace Neon.ClientExample.Net.Util
{
    public struct GameEventInvokationOptions
    {
        public bool safe;
        public bool throwOnEmptyInvocationList;

        public GameEventInvokationOptions(bool safe, bool throwOnEmptyInvocationList)
        {
            this.safe = safe;
            this.throwOnEmptyInvocationList = throwOnEmptyInvocationList;
        }

        public static GameEventInvokationOptions Default => new GameEventInvokationOptions(false, false);

        public GameEventInvokationOptions Safe()
        {
            this.safe = true;
            return this;
        }
        
        public GameEventInvokationOptions ThrowOnEmptyInvokationList()
        {
            this.throwOnEmptyInvocationList = true;
            return this;
        }
    }
}