namespace Neon.Rpc.Messages
{
    public enum RpcResponseStatusCode : byte
    {
        /// <summary>
        /// Successful operation
        /// </summary>
        Success = 0,
        /// <summary>
        /// Some requested entity was not found
        /// </summary>
        NotFound = 1,
        /// <summary>
        /// Client specified an invalid argument.  Note that this differs
        /// from FailedPrecondition.  InvalidArgument indicates arguments
        /// that are problematic regardless of the state of the system
        /// </summary>
        InvalidArgument = 2,
        /// <summary>
        /// Operation could not be completed
        /// </summary>
        InvalidOperation = 3,
        /// <summary>
        /// The caller does not have permission to execute the specified
        /// operation. PermissionDenied must not be
        /// used if the caller can not be identified (use Unauthenticated
        /// instead for those errors)
        /// </summary>
        PermissionDenied = 4,
        /// <summary>
        /// The request does not have valid authentication credentials for the operation
        /// </summary>
        Unauthenticated = 5,
        /// <summary>
        /// The request does not have valid access for the operation
        /// </summary>
        Unauthorized = 6,
        /// <summary>
        /// The operation was cancelled
        /// </summary>
        Cancelled = 7,
        /// <summary>
        /// Timeout expired before operation could complete. For operations
        /// that change the state of the system, this error may be returned
        /// even if the operation has completed successfully.  For example, a
        /// successful response from a server could have been delayed long
        /// enough for the deadline to expire. 
        /// </summary>
        TimeoutExceeded = 8,
        /// <summary>
        /// Some entity that we attempted to create already exists
        /// </summary>
        AlreadyExists = 9,
        /// <summary>
        /// Some resource has been exhausted, perhaps a per-user quota, or
        /// perhaps the entire file system is out of space.
        /// </summary>
        Exhausted = 10,
        /// <summary>
        /// Operation was rejected because the system is not in a state
        /// required for the operation's execution.
        /// </summary>
        FailedPrecondition = 11,
        /// <summary>
        /// Operation is not implemented in this service.
        /// </summary>
        NotImplemented = 12,
        /// <summary>
        /// Operation is not supported/enabled in this service.
        /// </summary>
        NotSupported = 13,
        /// <summary>
        /// Operation could not be completed due to connection issues
        /// </summary>
        ConnectionIssues = 14,
        /// <summary>
        /// Internal errors.  Means some invariants expected by underlying
        /// system has been broken.  If you see one of these errors,
        /// something is very broken.
        /// </summary>
        Internal = 100,
    }
}