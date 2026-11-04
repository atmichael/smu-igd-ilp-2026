using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.AccountsPayable.Parser;

namespace ILP.Shared.Test.AccountsPayable;

public sealed class AccountsPayableCodingParserTests
{
    [Fact]
    public void Parse_JsonResponse_ReturnsGstAndPerLineSuggestions()
    {
        var recommendation = AccountsPayableCodingParser.Parse(
            """
            {
              "gst_code": "SG_STD_9",
              "coa_suggestions": [
                { "line_index": 1, "account_code": "6000", "reason": "Stationery" }
              ],
              "review_reasons": []
            }
            """);

        Assert.Equal("SG_STD_9", recommendation.GstCode);
        var suggestion = Assert.Single(recommendation.CoaSuggestions);
        Assert.Equal(1, suggestion.LineIndex);
        Assert.Equal("6000", suggestion.AccountCode);
    }

    [Fact]
    public void Parse_FencedJsonResponse_ParsesJsonWithoutTrustingExtraText()
    {
        var recommendation = AccountsPayableCodingParser.Parse(
            "Model response:\n```json\n{\"gst_code\":\"SG_UNKNOWN\",\"coa_suggestions\":[],\"review_reasons\":[]}\n```");

        Assert.Equal("SG_UNKNOWN", recommendation.GstCode);
        Assert.Empty(recommendation.CoaSuggestions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"coa_suggestions\":[]}")]
    public void Parse_InvalidOrIncompleteResponse_ThrowsFormatException(string response)
    {
        Assert.Throws<FormatException>(() => AccountsPayableCodingParser.Parse(response));
    }
}
