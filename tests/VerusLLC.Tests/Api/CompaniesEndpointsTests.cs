using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace VerusLLC.Tests.Api;

public class CompaniesEndpointsTests
{
    private const string Endpoint = "/api/companies";

    private static StringContent Json(string body) =>
        new(body, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Post_WithAValidCompany_Returns201AndALocationThatResolves()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsync(
            Endpoint,
            Json("""{"name":"Contoso","websiteUrl":"https://contoso.com"}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.False(
            response.Headers.Location.IsAbsoluteUri,
            $"Location must be relative so it works behind a proxy, but was '{response.Headers.Location}'.");

        var created = await ReadAsync(response);

        Assert.NotEqual(Guid.Empty, created.GetProperty("id").GetGuid());
        Assert.Equal("Contoso", created.GetProperty("name").GetString());
        Assert.Equal("https://contoso.com", created.GetProperty("websiteUrl").GetString());

        using var followed = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);

        var fetched = await ReadAsync(followed);

        Assert.Equal(created.GetProperty("id").GetGuid(), fetched.GetProperty("id").GetGuid());
        Assert.Equal("Contoso", fetched.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Post_DoesNotAcceptAClientSuppliedId()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        var supplied = Guid.Parse("11111111-1111-1111-1111-111111111111");

        using var response = await client.PostAsync(
            Endpoint,
            Json($$"""{"id":"{{supplied}}","name":"Contoso","websiteUrl":"https://contoso.com"}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(supplied, (await ReadAsync(response)).GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("""{"name":"Microsoft","websiteUrl":"https://contoso.com"}""")]
    [InlineData("""{"name":"Microsoft","websiteUrl":"https://portal.microsoft.com"}""")]
    [InlineData("""{"name":"C","websiteUrl":"https://c.com"}""")]
    [InlineData("""{"name":"Contoso","websiteUrl":"not-a-url"}""")]
    public async Task Post_WithAnInvalidCompany_Returns400AndStoresNothing(string body)
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.PostAsync(Endpoint, Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var list = await client.GetAsync(Endpoint);

        Assert.Equal(0, (await ReadAsync(list)).GetArrayLength());
    }

    [Fact]
    public async Task Post_WithMalformedJson_Returns400InTheProblemFormat()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.PostAsync(Endpoint, Json("""{"name":"Contoso",,}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadAsync(response);

        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("errors", out _));
        Assert.True(problem.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Post_WithNoBody_Returns400InTheProblemFormat()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.PostAsync(Endpoint, Json(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadAsync(response);

        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Post_WithMissingFields_Returns400WithPerFieldErrors()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.PostAsync(Endpoint, Json("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = (await ReadAsync(response)).GetProperty("errors");

        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("WebsiteUrl", out _));
    }

    [Fact]
    public async Task Get_WithNoCompanies_Returns200AndAnEmptyArray()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadAsync(response);

        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task Get_WithFiltersThatMatchNothing_Returns200AndAnEmptyArray()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        await client.PostAsync(Endpoint, Json("""{"name":"Contoso","websiteUrl":"https://contoso.com"}"""));

        using var response = await client.GetAsync($"{Endpoint}?search=zzzznothing");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await ReadAsync(response)).GetArrayLength());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetById_WithAMalformedOrEmptyId_Returns400(string id)
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"{Endpoint}/{id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, (await ReadAsync(response)).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetById_WithAWellFormedUnknownId_Returns404()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"{Endpoint}/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadAsync(response);

        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal("company.not_found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Responses_ExposeOnlyTheHttpContract()
    {
        using var app = new CompaniesApiFactory();
        using var client = app.CreateClient();

        await client.PostAsync(Endpoint, Json("""{"name":"Contoso","websiteUrl":"https://contoso.com"}"""));

        using var response = await client.GetAsync(Endpoint);

        var fields = (await ReadAsync(response))[0]
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["id", "name", "websiteUrl"], fields);
    }

    [Fact]
    public async Task SampleDataSeeding_IsOffByDefaultForTestsAndOnWhenEnabled()
    {
        using var seeded = new CompaniesApiFactory(seedSampleData: true);
        using var client = seeded.CreateClient();

        using var response = await client.GetAsync(Endpoint);
        var companies = await ReadAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(companies.GetArrayLength() > 0, "Seeding enabled should populate the store.");

        var names = companies.EnumerateArray()
            .Select(company => company.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("Microsoft", names);
    }
}
