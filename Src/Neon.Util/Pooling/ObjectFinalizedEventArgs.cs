using System;

namespace Neon.Util.Pooling
{
    public class ObjectFinalizedEventArgs
    {
        /// <summary>
        /// Finalized object id
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Finalized object type
        /// </summary>
        public Type ObjectType { get; }
        
        /// <summary>
        /// Stack where the stream was allocated.
        /// </summary>
        public string AllocationStack { get; }

        public ObjectFinalizedEventArgs(Guid id, Type objType, string allocationStack)
        {
            Id = id;
            ObjectType = objType;
            AllocationStack = allocationStack;
        }
    }
}