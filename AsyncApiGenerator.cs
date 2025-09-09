namespace ApiGenerator
{
    using ByteBard.AsyncAPI.Models;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    internal class AsyncApiGenerator
    {
        public static string ConvertAsyncApi(AsyncApiDocument document)
        {
            StringBuilder sb = new StringBuilder();

            foreach(var evt in document.Channels.First().Value.Messages.Values)
            {
                var res = ConvertiMessaggio(evt);
                sb.Append(res);
            }

            foreach (var evt in document.Channels.First().Value.Messages.Values)
            {
                var res = ConvertiMessaggio(evt);
                sb.Append(res);
            }

            return sb.ToString(); ;
        }

        private static string ConvertiMessaggio(AsyncApiMessage evt)
        {
            var name = evt.Name;
            var namespac = name.Substring(0, name.LastIndexOf("."));
            var tipo = name.Substring(name.LastIndexOf(".") + 1);

            var message = string.Empty;
            message += "  namespace " + namespac + " { " + Environment.NewLine;
            message += "    public class " + tipo + " : DomainEvent { " + Environment.NewLine; //eredita dal necessario

            ICollection<AsyncApiJsonSchemaReference> schemas = new List<AsyncApiJsonSchemaReference>();

            var schema = (AsyncApiJsonSchema)evt.Payload.Schema;

            foreach (var prop in schema.AllOf)
            {
                if (prop is not AsyncApiJsonSchemaReference)
                {
                    foreach (var prope in prop.Properties)
                    {
                        message += "      " + ConvertiProp(prope.Key, prope.Value) + Environment.NewLine;
                        if(prope.Value.Items != null)
                        {
                            schemas.Add((AsyncApiJsonSchemaReference)prope.Value.Items);
                        }
                        //se prop.value.items aggiungi a lista di cose da fare
                    }
                }
            }

            message += "    }" + Environment.NewLine;

            foreach(var extraScheme in schemas)
            {
                message += Environment.NewLine;
                message += "    public class " + Upper(extraScheme.Reference.Reference.Substring(extraScheme.Reference.Reference.LastIndexOf("/")+1)) + " {" + Environment.NewLine;

                foreach(var extraProp in extraScheme.Properties)
                {
                    message += "      " + ConvertiProp(extraProp.Key, extraProp.Value) + Environment.NewLine;
                }

                message += "    }" + Environment.NewLine;
            }

            message+= "  }" + Environment.NewLine + Environment.NewLine;
            return message;
        }

        private static string ConvertiProp(string key, AsyncApiJsonSchema value)
        {
            if(value is AsyncApiJsonSchemaReference)
            {
                return "public " + Upper((value as AsyncApiJsonSchemaReference).Reference.FragmentId.Split('/').Last()) + " " + Upper(key) + " { get; set; }"; 
            }
            else
            {

                return "public " + ConvertiTipo(value.Type, (AsyncApiJsonSchemaReference)value.Items) + " " + Upper(key) + " { get; set; }";
            }

        }

        private static string ConvertiTipo(SchemaType? type, AsyncApiJsonSchemaReference items)
        {
            switch (type)
            {
                case SchemaType.String: return "String";
                    case SchemaType.Number: return "int";
                case SchemaType.Integer: return "int";
                case SchemaType.Boolean: return "bool";
                case SchemaType.Array: return "ICollection<" + Upper(items.Reference.FragmentId.Split('/').Last())  +">";
                default: return "unknown";
            }
        }

        private static string Upper(string input)
        {
            return input[0].ToString().ToUpper() + input.Substring(1);
        }
    }
}
