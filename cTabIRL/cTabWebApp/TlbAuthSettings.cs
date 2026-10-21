using System;
using System.Net;

namespace cTabWebApp
{
    /// <summary>
    /// Shared TLB sign-in (TLB Login Service). When PublicUrl is set, cTab accepts the suite session
    /// cookie instead of its own Steam sign-in; the Steam ID comes from the account's linked Steam.
    /// </summary>
    public class TlbAuthSettings
    {
        public const string Scheme = "TlbAuth";
        public const string SessionCookie = "tlb_session";
        public const string Audience = "tlb-suite";
        public const string SteamIdClaim = "steam_id";
        public const string MemberPermission = "member";

        /// <summary>Public address of the Login Service, e.g. https://auth.tlbmilsim.com/ (also the token issuer).</summary>
        public string PublicUrl { get; set; }

        /// <summary>Where cTab fetches the suite discovery document (signing keys); defaults to PublicUrl.
        /// In Docker: http://auth:8080/.well-known/tlb-suite-configuration</summary>
        public string MetadataAddress { get; set; }

        /// <summary>The Login Service's live session check; defaults to PublicUrl.
        /// In Docker: http://auth:8080/verify</summary>
        public string VerifyAddress { get; set; }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(PublicUrl);

        public string Issuer => PublicUrl.EndsWith("/") ? PublicUrl : PublicUrl + "/";

        public string MetadataUrl => string.IsNullOrWhiteSpace(MetadataAddress) ? Issuer + ".well-known/tlb-suite-configuration" : MetadataAddress;

        public string VerifyUrl => string.IsNullOrWhiteSpace(VerifyAddress) ? Issuer + "verify" : VerifyAddress;

        public string Url(string path, string returnUrl)
        {
            return Issuer + path + "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        }

        /// <summary>
        /// True for addresses that never leave the machine or the Docker network (localhost, private
        /// IPv4 addresses, single-label service names such as "auth"): plain http is accepted only there.
        /// </summary>
        public static bool IsInternal(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }
            if (uri.IsLoopback)
            {
                return true;
            }
            switch (uri.HostNameType)
            {
                case UriHostNameType.IPv4:
                    var b = IPAddress.Parse(uri.Host).GetAddressBytes();
                    return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
                case UriHostNameType.Dns:
                    return !uri.Host.Contains('.');
                default:
                    return false;
            }
        }

        /// <summary>Throws when a Login Service address would be fetched over plain http across a network.</summary>
        public void Validate()
        {
            foreach (var url in new[] { MetadataUrl, VerifyUrl })
            {
                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !IsInternal(url))
                {
                    throw new InvalidOperationException($"TlbAuth: {url} must use https (plain http only for internal addresses).");
                }
            }
        }
    }
}
