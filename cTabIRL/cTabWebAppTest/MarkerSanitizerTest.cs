using Arma3TacMapLibrary.Maps;

namespace cTabWebAppTest
{
    // HtmlSanitizer runs on a newer AngleSharp than it declares (see cTabWebApp.csproj): check it still sanitizes.
    public class MarkerSanitizerTest
    {
        [Fact]
        public void NoteContent_IsSanitized()
        {
            var data = new MarkerData
            {
                type = "note",
                config = new Dictionary<string, string>
                {
                    ["content"] = "<b>Rally</b><img src=x onerror=alert(1)><script>alert(2)</script><math><annotation-xml encoding=\"text/html\"><style><img src=x onerror=alert(3)></style></annotation-xml></math>",
                },
            };
            var json = MarkerData.Serialize(data);
            Assert.Contains("Rally", json);
            Assert.DoesNotContain("onerror", json);
            Assert.DoesNotContain("<script", json);
        }
    }
}
