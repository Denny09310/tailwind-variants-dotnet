using Markdig;

namespace TailwindVariants.NET.Docs.Services;

public class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseGlobalization()
        .Build();

    public async Task<string> RenderAsync(string filepath)
    {
        var markdown = await File.ReadAllTextAsync(filepath);
        return Markdown.ToHtml(markdown, _pipeline);
    }
}