using System.Collections.Concurrent;

namespace Duende.TokenManagement.ClientCredentials
{
    /// <summary>
    /// Default implementation for token request synchronization primitive
    /// </summary>
    public class TokenRequestSynchronization : ITokenRequestSynchronization
    {
        /// <inheritdoc />
        public ConcurrentDictionary<string, Lazy<Task<string?>>> Dictionary { get; } = new();
    }
}