using ApiGenerator;
using CommandLine;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Net.Http;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Readers;

//return GenerateOpenApi(new OpenApiOptions() { ApiUrl = "https://raw.githubusercontent.com/Redocly/museum-openapi-example/refs/heads/main/openapi.yaml", OutputFile = "test.cs" });
return GenerateAsyncApi(new AsyncApiOptions() { FileName = "C:\\\\TEmp\\test.asyncapi.txt", OutputFile = @"C:\\temp\test.cs" });

return CommandLine.Parser.Default.ParseArguments<AsyncApiOptions, OpenApiOptions>(args)
   .MapResult(
     (AsyncApiOptions opts) => GenerateAsyncApi(opts),
     (OpenApiOptions opts) => GenerateOpenApi(opts),
     errs => 1);

static int GenerateAsyncApi(AsyncApiOptions opts)
{
    AsyncApiDocument doc = null;
    if (opts.ApiUrl != null)
    {
        var httpClient = new HttpClient();
        var stream =  httpClient.GetStreamAsync(opts.ApiUrl).Result;
        doc  = new AsyncApiStreamReader().Read(stream, out var diagnostic);
    }
    else
    {
        var res = new AsyncApiStringReader(new AsyncApiReaderSettings() { }).Read(File.ReadAllText(opts.FileName), out var diagnostic);
        doc = res;
    }

    var f = AsyncApiGenerator.ConvertAsyncApi(doc);

    File.WriteAllText(opts.OutputFile, f);

    return 0;
}

static int GenerateOpenApi(OpenApiOptions opts)
{
    OpenApiDocument doc = null;
    if (opts.ApiUrl != null)
    {
        doc = Microsoft.OpenApi.Reader.OpenApiModelFactory.Load(opts.ApiUrl).OpenApiDocument;
    }
    else
    {
        var res = Microsoft.OpenApi.Reader.OpenApiModelFactory.Load(opts.FileName);
        doc =res.OpenApiDocument;
    }

    var f = OpenAPiGenerator.ConvertOpenApi(doc);

    File.WriteAllText(opts.OutputFile, f);

    return 0;
}



static Stream OpenFile(string path)
{
    return File.OpenRead(path);
}
