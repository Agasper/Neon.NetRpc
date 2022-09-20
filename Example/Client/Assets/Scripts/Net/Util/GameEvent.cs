using System;
using System.Collections.Generic;
using Neon.Logging;

namespace Neon.ClientExample.Net.Util
{
    public delegate void GameAction();
    public delegate void GameAction<T>(T arg);
    public delegate void GameAction<T, T2>(T arg, T2 arg2);

    public interface IGameEvent
    {
        void AddListener( GameAction action);
        bool RemoveListener(GameAction action);
    }
    
    public interface IGameEvent<T>
    {
        void AddListener(GameAction<T> action);
        bool RemoveListener(GameAction<T> action);
    }
    
    public interface IGameEvent<T,T2>
    {
        void AddListener(GameAction<T,T2> action);
        bool RemoveListener(GameAction<T,T2> action);
    }
    
    public class GameEvent : IGameEvent
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(GameEvent));
        
        List<GameAction> actions;

        public GameEvent()
        {
            this.actions = new List<GameAction>();
        }

        public void AddListener(GameAction action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));

            actions.Add(action);
        }
        
        public bool RemoveListener(GameAction action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));
            return actions.Remove(action);
        }
        
        public void RemoveAllListeners()
        {
            actions.Clear();
        }

        public void Invoke() => this.Invoke(GameEventInvokationOptions.Default);
        public void Invoke(GameEventInvokationOptions options)
        {
            if (options.throwOnEmptyInvocationList && actions.Count == 0)
                throw new InvalidOperationException("Invocation list is empty");
            
            for (int i = 0; i < actions.Count; i++)
            {
                Invoke(options, actions[i]);
            }
        }

        void Invoke(GameEventInvokationOptions options, GameAction action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                if (options.safe)
                    logger.Error($"Unhandled exception on target invocation: {e}");
                else
                    throw;
            }
        }
    }
    
    public class GameEvent<T,T2> : IGameEvent<T,T2>
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(GameEvent));
        List<GameAction<T,T2>> actions;

        public GameEvent()
        {
            actions = new List<GameAction<T,T2>>();
        }

        public void AddListener(GameAction<T,T2> action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));
            actions.Add(action);
        }
        
        public bool RemoveListener(GameAction<T,T2> action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));
            return actions.Remove(action);
        }

        public void RemoveAllListeners()
        {
            actions.Clear();
        }
        
        public void Invoke(T arg, T2 arg2) => this.Invoke(arg, arg2, GameEventInvokationOptions.Default);
        public void Invoke(T arg, T2 arg2,  GameEventInvokationOptions options)
        {
            if (options.throwOnEmptyInvocationList && actions.Count == 0)
                throw new InvalidOperationException("Invocation list is empty");
            
            for (int i = 0; i < actions.Count; i++)
            {
                Invoke(arg, arg2, options, actions[i]);
            }
        }

        void Invoke(T arg, T2 arg2, GameEventInvokationOptions options, GameAction<T, T2> action)
        {
            try
            {
                action.Invoke(arg, arg2);
            }
            catch (Exception e)
            {
                if (options.safe)
                    logger.Error($"Unhandled exception on target invocation: {e}");
                else
                    throw;
            }
        }
    }
    
    public class GameEvent<T> : IGameEvent<T>
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(GameEvent));
        List<GameAction<T>> actions;

        public GameEvent()
        {
            actions = new List<GameAction<T>>();
        }

        public void AddListener(GameAction<T> action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));
            actions.Add(action);
        }
        
        public bool RemoveListener(GameAction<T> action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));
            return actions.Remove(action);
        }

        public void RemoveAllListeners()
        {
            actions.Clear();
        }
        
        public void Invoke(T arg) => this.Invoke(arg, GameEventInvokationOptions.Default);
        public void Invoke(T arg,  GameEventInvokationOptions options)
        {
            if (options.throwOnEmptyInvocationList && actions.Count == 0)
                throw new InvalidOperationException("Invocation list is empty");
            
            for (int i = 0; i < actions.Count; i++)
            {
                Invoke(arg, options, actions[i]);
            }
        }

        void Invoke(T arg, GameEventInvokationOptions options, GameAction<T> action)
        {
            try
            {
                action.Invoke(arg);
            }
            catch (Exception e)
            {
                if (options.safe)
                    logger.Error($"Unhandled exception on target invocation: {e}");
                else
                    throw;
            }
        }
    }
}