using FluentAssertions;
using Microsoft.Extensions.FileProviders;

namespace LLL.AutoCompute.EFCore.Explorer.Tests;

public class EmbeddedManifestTests
{
    [Fact]
    public void Explorer_assembly_has_embedded_file_manifest()
    {
        var assembly = typeof(AutoComputeExplorerExtensions).Assembly;

        var act = () => new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        act.Should().NotThrow(
            "the NuGet package must include the embedded files manifest for all target frameworks");
    }

    [Fact]
    public void Explorer_embedded_files_include_index_html()
    {
        var assembly = typeof(AutoComputeExplorerExtensions).Assembly;
        var fileProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        var fileInfo = fileProvider.GetFileInfo("index.html");

        fileInfo.Exists.Should().BeTrue("index.html must be embedded in the Explorer assembly");
    }
}
