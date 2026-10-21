using cTabWebApp;

namespace cTabWebAppTest
{
    public class TlbAuthSettingsTest
    {
        [Fact]
        public void Defaults_UsePublicUrl()
        {
            var settings = new TlbAuthSettings { PublicUrl = "https://auth.tlbmilsim.com" };
            Assert.Equal("https://auth.tlbmilsim.com/.well-known/tlb-suite-configuration", settings.MetadataUrl);
            Assert.Equal("https://auth.tlbmilsim.com/verify", settings.VerifyUrl);
            settings.Validate();
        }

        [Theory]
        [InlineData("http://auth:8080/verify", true)]
        [InlineData("http://localhost:5100/verify", true)]
        [InlineData("http://127.0.0.1:5100/verify", true)]
        [InlineData("http://192.168.1.20/verify", true)]
        [InlineData("http://172.20.0.5/verify", true)]
        [InlineData("http://auth.tlbmilsim.com/verify", false)]
        [InlineData("http://8.8.8.8/verify", false)]
        [InlineData("not a url", false)]
        public void IsInternal(string url, bool expected)
        {
            Assert.Equal(expected, TlbAuthSettings.IsInternal(url));
        }

        [Fact]
        public void Validate_RejectsPlainHttpAcrossTheNetwork()
        {
            var settings = new TlbAuthSettings { PublicUrl = "http://auth.tlbmilsim.com/" };
            Assert.Throws<InvalidOperationException>(settings.Validate);

            settings.MetadataAddress = "http://auth:8080/.well-known/tlb-suite-configuration";
            settings.VerifyAddress = "http://auth:8080/verify";
            settings.Validate();
        }
    }
}
