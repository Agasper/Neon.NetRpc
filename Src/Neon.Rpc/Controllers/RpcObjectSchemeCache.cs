using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neon.Rpc.Controllers
{
    public static class RpcObjectSchemeCache
    {
        static ConditionalWeakTable<Type, RpcObjectScheme> _weakTable =
            new ConditionalWeakTable<Type, RpcObjectScheme>();
        static ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        
        public static RpcObjectScheme TryGetObjectScheme(RpcInvocationRules rules, Type type)
        {
            _readerWriterLock.EnterUpgradeableReadLock();
            try
            {
                if (!_weakTable.TryGetValue(type, out var scheme))
                {
                    _readerWriterLock.EnterWriteLock();
                    try
                    {
                        scheme = RpcObjectScheme.Create(rules, type);
                        _weakTable.Add(type, scheme);
                    }
                    finally
                    {
                        _readerWriterLock.ExitWriteLock();
                    }
                }

                return scheme;
            }
            finally
            {
                _readerWriterLock.ExitUpgradeableReadLock();
            }
        }
    }
}