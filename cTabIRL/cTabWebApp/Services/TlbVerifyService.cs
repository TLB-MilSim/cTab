using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace cTabWebApp.Services
{
    /// <summary>
    /// Live check of a TLB suite session against the Login Service's /verify endpoint: session still
    /// active, account not blocked, current Discord roles and Steam link. Answers are reused for
    /// <see cref="CacheFor"/> per session, so signing out, a block or a removed role reaches cTab within
    /// that time. When the Login Service can't be reached, there is no access (fail closed).
    /// </summary>
    public class TlbVerifyService
    {
        public static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
        {
            // /verify answers directly; never follow a redirect with the session cookie.
            AllowAutoRedirect = false,
            UseCookies = false,
        })
        {
            Timeout = TimeSpan.FromSeconds(3),
        };

        private readonly TlbAuthSettings _settings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TlbVerifyService> _logger;

        public TlbVerifyService(TlbAuthSettings settings, IMemoryCache cache, ILogger<TlbVerifyService> logger)
        {
            _settings = settings;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>The live identity for a session cookie value, or null when there is no access.</summary>
        public async Task<TlbVerifyResult> VerifyAsync(string sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return null;
            }
            // Keyed by hash: the cache never holds the session itself.
            var key = "tlb-verify:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionToken)));
            if (_cache.TryGetValue(key, out CacheEntry cached))
            {
                return cached.Result;
            }

            var (result, cacheable) = await FetchAsync(sessionToken, cancellationToken);
            if (cacheable)
            {
                _cache.Set(key, new CacheEntry(result), CacheFor);
            }
            return result;
        }

        private async Task<(TlbVerifyResult, bool)> FetchAsync(string sessionToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _settings.VerifyUrl);
            request.Headers.Add("Cookie", TlbAuthSettings.SessionCookie + "=" + sessionToken);
            try
            {
                using var response = await Http.SendAsync(request, cancellationToken);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadFromJsonAsync<TlbVerifyResult>(cancellationToken);
                        return string.IsNullOrEmpty(result?.DiscordId) ? (null, false) : (result, true);
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return (null, true);
                    default:
                        _logger.LogWarning("TLB /verify answered HTTP {Status}", (int)response.StatusCode);
                        return (null, false);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "TLB /verify unreachable");
                return (null, false);
            }
        }

        private sealed record CacheEntry(TlbVerifyResult Result);
    }

    /// <summary>The Login Service's /verify answer.</summary>
    public class TlbVerifyResult
    {
        [JsonPropertyName("discord_id")]
        public string DiscordId { get; set; }

        [JsonPropertyName("steam_id")]
        public string SteamId { get; set; }

        [JsonPropertyName("perms")]
        public string[] Perms { get; set; } = Array.Empty<string>();

        public bool Has(string permission) => Perms?.Contains(permission) ?? false;
    }
}
