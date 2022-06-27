using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Duende.TokenManagement.OpenIdConnect
{
    /// <summary>
    /// Options-based configuration service for token clients
    /// </summary>
    public class DefaultUserTokenConfigurationService : IUserTokenConfigurationService
    {
        private readonly UserAccessTokenManagementOptions _userAccessTokenManagementOptions;
        private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly ILogger<DefaultUserTokenConfigurationService> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="userAccessTokenManagementOptions"></param>
        /// <param name="clientAccessTokenManagementOptions"></param>
        /// <param name="oidcOptions"></param>
        /// <param name="schemeProvider"></param>
        /// <param name="logger"></param>
        public DefaultUserTokenConfigurationService(
            UserAccessTokenManagementOptions userAccessTokenManagementOptions,
            ClientAccessTokenManagementOptions clientAccessTokenManagementOptions,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IAuthenticationSchemeProvider schemeProvider,
            ILogger<DefaultTokenClientConfigurationService> logger)
        {
            _userAccessTokenManagementOptions = userAccessTokenManagementOptions;
            _clientAccessTokenManagementOptions = clientAccessTokenManagementOptions;
            
            _oidcOptions = oidcOptions;
            _schemeProvider = schemeProvider;
            _logger = logger;
        }
        
        /// <inheritdoc />
        public virtual async Task<RefreshTokenRequest> GetRefreshTokenRequestAsync(UserAccessTokenParameters? parameters = null)
        {
            var (options, configuration) = await GetOpenIdConnectSettingsAsync(parameters?.ChallengeScheme ?? _userAccessTokenManagementOptions.SchemeName);

            var requestDetails = new RefreshTokenRequest
            {
                Address = configuration.TokenEndpoint,
                ClientCredentialStyle = _userAccessTokenManagementOptions.ClientCredentialStyle,

                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret
            };
            
            var assertion = await CreateAssertionAsync();
            if (assertion != null)
            {
                requestDetails.ClientCredentialStyle = ClientCredentialStyle.PostBody;
                requestDetails.ClientAssertion = assertion;
            }

            return requestDetails;
        }

        /// <inheritdoc />
        public virtual async Task<TokenRevocationRequest> GetTokenRevocationRequestAsync(UserAccessTokenParameters? parameters = null)
        {
            var (options, configuration) = await GetOpenIdConnectSettingsAsync(parameters?.ChallengeScheme ?? _userAccessTokenManagementOptions.SchemeName);
            
            var requestDetails = new TokenRevocationRequest
            {
                Address = configuration.AdditionalData[OidcConstants.Discovery.RevocationEndpoint].ToString(),
                ClientCredentialStyle = _userAccessTokenManagementOptions.ClientCredentialStyle,

                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret
            };
            
            var assertion = await CreateAssertionAsync();
            if (assertion != null)
            {
                requestDetails.ClientCredentialStyle = ClientCredentialStyle.PostBody;
                requestDetails.ClientAssertion = assertion;
            }

            return requestDetails;
        }
        
        /// <summary>
        /// Retrieves configuration from a named OpenID Connect handler
        /// </summary>
        /// <param name="schemeName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual async Task<(OpenIdConnectOptions options, OpenIdConnectConfiguration configuration)> GetOpenIdConnectSettingsAsync(string? schemeName)
        {
            OpenIdConnectOptions options;

            if (string.IsNullOrWhiteSpace(schemeName))
            {
                var scheme = await _schemeProvider.GetDefaultChallengeSchemeAsync();

                if (scheme is null)
                {
                    throw new InvalidOperationException("No OpenID Connect authentication scheme configured for getting client configuration. Either set the scheme name explicitly or set the default challenge scheme");
                }

                options = _oidcOptions.Get(scheme.Name);
            }
            else
            {
                options = _oidcOptions.Get(schemeName);
            }

            OpenIdConnectConfiguration configuration;
            try
            {
                configuration = await options.ConfigurationManager!.GetConfigurationAsync(default);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Unable to load OpenID configuration for configured scheme: {e.Message}");
            }

            return (options, configuration);
        }

        /// <summary>
        /// Allows injecting a client assertion into outgoing requests
        /// </summary>
        /// <param name="clientName">Name of client (if present)</param>
        /// <returns></returns>
        protected virtual Task<ClientAssertion?> CreateAssertionAsync(string? clientName = null)
        {
            return Task.FromResult<ClientAssertion?>(null);
        }
    }
}