namespace ApiGenerator
{
    using CommandLine;

    [Verb("openapi", HelpText = "Genera un client a partire da un file openapi")]
    public class OpenApiOptions
    {
        [Option('u', "url", Required = false, HelpText ="Url del file openapi")]
        public string ApiUrl { get; set; }

        [Option('f', "file", Required = false, HelpText = "File da convertire")]
        public string FileName { get; set; }

        [Option('o', "outputfile", Required = true, HelpText = "File di output")]
        public string OutputFile { get; set; }

        [Option('n', "namespace", Required = true, HelpText = "Namespace da usare")]
        public string DesiredNamespace { get; set; }
    }
}
