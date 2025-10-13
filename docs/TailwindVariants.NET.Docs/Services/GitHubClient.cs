namespace TailwindVariants.NET.Docs.Services;

public class GitHubClient(HttpClient http)
{
	public async Task<int> GetRepostoryStars()
	{
		try
		{
			var repo = await http.GetFromJsonAsync<GitHubRepo>("https://api.github.com/repos/Denny09310/tailwind-variants-dotnet");
			return repo?.StarGazers ?? 0;
		}
		catch
		{
			return 0;
		}
	}
}

public record GitHubRepo
(
	[property: System.Text.Json.Serialization.JsonPropertyName("stargazers_count")]
		int StarGazers
);
