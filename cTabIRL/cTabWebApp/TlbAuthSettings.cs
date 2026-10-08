using System;

namespace cTabWebApp
{
    /// <summary>
    /// Shared TLB sign-in (auth service). When PublicUrl is set, cTab accepts the suite session
    /// cookie instead of its own Steam sign-in; the Steam ID comes from the account's linked Steam.
    /// </summary>
    public class TlbAuthSettings
    {
        public const string Scheme = "TlbAuth";
        public const string SessionCookie = "tlb_session";
        public const string Audience = "tlb-suite";
        public const string SteamIdClaim = "steam_id";
        public const string MemberPermission = "member";

        /// <summary>Public address of the auth service, e.g. https://auth.tlbmilsim.com/ (also the token issuer).</summary>
        public string PublicUrl { get; set; }

        /// <summary>Where cTab fetches the auth service's discovery document; defaults to PublicUrl.
        /// In Docker: http://auth:8080/.well-known/openid-configuration</summary>
        public string MetadataAddress { get; set; }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(PublicUrl);

        public string Issuer => PublicUrl.EndsWith("/") ? PublicUrl : PublicUrl + "/";

        public string MetadataUrl => string.IsNullOrWhiteSpace(MetadataAddress) ? Issuer + ".well-known/openid-configuration" : MetadataAddress;

        public string Url(string path, string returnUrl)
        {
            return Issuer + path + "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        }
    }
}
