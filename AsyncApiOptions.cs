namespace ApiGenerator
{
    using CommandLine;

    [Verb("asyncapi", HelpText = "Genera un client a partire da un file asyncapi")]
    public class AsyncApiOptions
    {
        [Option('u', "url", Required = false, HelpText = "Url del file asyncapi")]
        public string ApiUrl { get; set; }

        [Option('f', "file", Required = false, HelpText = "File da convertire")]
        public string FileName { get; set; }

        [Option('o', "outputfile", Required = true, HelpText = "File di output")]
        public string OutputFile { get; set; }
    }
}
