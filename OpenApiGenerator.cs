using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.References;
using System.Runtime.CompilerServices;
using System.Text;

namespace ApiGenerator
{
    public class OpenAPiGenerator
    {
        /// <summary>
        ///     Covenrte un document oopenApi stampando su stringa
        /// </summary>
        /// <param name="doc">Document oda convertire</param>
        /// <returns>Stringa con client generato dal documento</returns>
        public static string ConvertOpenApi(OpenApiDocument doc, string desiredNamespace)
        {
            StringBuilder a = new StringBuilder();

            a.AppendLine("namespace "+ desiredNamespace +" {");
            a.AppendLine("  using ServiziCgn.Framework.WebClient;");
            a.AppendLine("  using System.Collections.Generic;");
            a.AppendLine();

            foreach (var ele in doc.Components.Schemas)
            {
                a.Append(ConvertClass(ele.Key, ele.Value));
            }

            var interfaceName = doc.Info.Title.Replace("servizicgn.", "").Replace(" ", "");
            a.AppendLine(" public interface I" + interfaceName + " {");

            foreach (var method in doc.Paths)
            {
                foreach (var op in method.Value.Operations)
                {
                    a.Append(ConvertMethodSignature(method.Key, op.Value, true));
                }
            }

            a.AppendLine("}");
            a.AppendLine();

            var security = GetSecurity(doc);

            a.AppendLine(" public partial class " + doc.Info.Title.Replace("servizicgn.", "").Replace(" ", "") + "Client : " + (security == "Basic" ? "ApiClient" : "AuthorizedApiClient") + ", " + interfaceName + " {");


            a.AppendLine();
            if (security == "Basic")
            {
                a.AppendLine("   public " + doc.Info.Title.Replace("servizicgn.", "").Replace(" ", "") +  "Client(IHttpClientFactory clientFactory, string url, string username, string password) : base(clientFactory, url) {");
                a.AppendLine("     this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"Applicazione:{0}:{1}\", username, password))));");
                a.AppendLine("   }");
                a.AppendLine();
            }
            else
            {
                a.AppendLine("   public " + doc.Info.Title.Replace("servizicgn.", "").Replace(" ", "") + "Client(IHttpClientFactory clientFactory, IJwtTokenProvider jwtTokenProvider, string url) : base(clientFactory, jwtTokenProvider, url) {");
                a.AppendLine("   }");
                a.AppendLine();
            }

            foreach (var method in doc.Paths)
            {
                foreach (var op in method.Value.Operations)
                {
                    a.Append(ConvertMethodSignature(method.Key, op.Value, false));
                    a.Append(ConvertMethodBody(method.Key, op.Key.ToString(), op.Value));
                }
            }

            a.AppendLine("}");
            a.AppendLine("}");

            return a.ToString();
        }

        /// <summary>
        ///     determina il tipo di security della'pi
        /// </summary>
        /// <param name="doc">Document openapi</param>
        /// <returns>Tipo di autenticazione identificata</returns>
        private static string GetSecurity(OpenApiDocument doc)
        {
            //TODO verificare
            if (doc.Paths.First().Value.Operations.First().Value.Security.Any(x => x is OpenApiSecurityRequirement))
            {
                var rew = (OpenApiSecurityRequirement)doc.Paths.First().Value.Operations.First().Value.Security.First((x => x is OpenApiSecurityRequirement));

                if (rew.Values.First().Count() > 0 && rew.Values.First().First() == "bearerAuth")
                {
                    return "Bearer";
                }
                else
                {
                    return "Basic";
                }

            }

            return "Basic";
        }

        /// <summary>
        ///     Crea una classe a partire dallo schema
        /// </summary>
        /// <param name="key">Nome delle classe</param>
        /// <param name="value">Schema da convertire</param>
        /// <param name="a">String builder da usare</param>
        private static string ConvertClass(string key, OpenApiSchema value)
        {
            string result = string.Empty;
            if (value.Enum.Count > 0)
            {
                result += "  public enum " + key + " {" + Environment.NewLine;
                int i = 0;
                foreach (var el in value.Enum)
                {
                    result += "   " + ((OpenApiAny)value.Extensions["x-enum-varnames"]).Node[i] + " = " + el.ToString() + Environment.NewLine;
                    i++;
                }

            }
            else
            {
                string baseClass = null;

                if (value.AllOf.Any(x => x is OpenApiSchemaReference))
                {
                    baseClass = value.AllOf.First(x => x is OpenApiSchemaReference).Reference.Id;
                }

                result += "  /// <summary>" + Environment.NewLine   ;
                result += $"  ///   {(value.Description??string.Empty).Replace("\r\n", "\r\n  ///   ")}" + Environment.NewLine;
                result += "  /// </summary>" + Environment.NewLine;
                result += "  public class " + key + (baseClass!= null ? (": " + baseClass) : string.Empty) + " {" + Environment.NewLine;

                foreach (var prop in value.Properties)
                {
                    result += Environment.NewLine;
                    result += "    /// <summary>" + Environment.NewLine;
                    result += "    /// " + (prop.Value.Description ?? string.Empty).Replace("\r\n", "\r\n    ///   ") + Environment.NewLine;
                    result += "    /// <summary>" + Environment.NewLine;
                    result += "    public " + Convert(prop.Value) + " " + prop.Key + " { get; set;}" + Environment.NewLine;
                }

                if (value.AllOf.Any(x => x is OpenApiSchema))
                {
                    foreach (var prop in value.AllOf.Where(x => x is OpenApiSchema).First().Properties)
                    {
                        result += Environment.NewLine;
                        result += "    /// <summary>" + Environment.NewLine;
                        result += "    /// " + (prop.Value.Description ?? string.Empty).Replace("\r\n", "\r\n    ///   ") + Environment.NewLine;
                        result += "    /// <summary>" + Environment.NewLine;
                        result += "    public " + Convert(prop.Value) + " " + prop.Key + " { get; set;}" + Environment.NewLine;
                    }
                }

            }

            result += "  }" + Environment.NewLine;
            result += Environment.NewLine;
            result += Environment.NewLine;

            return result;
        }

        /// <summary>
        ///     Converte un tipo nella sua descrizione
        /// </summary>
        /// <param name="type">Tipo da riferire</param>
        /// <returns></returns>
        private static string Convert(OpenApiSchema? type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (type.Reference != null)
            {
                return type.Reference.Id.ToString();
            }

            if (type.Type.HasValue)
            {
                switch (type.Type)
                {
                    case JsonSchemaType.String:
                        switch (type.Format)
                        {
                            case "date-time": return type.Nullable ? "DateTime?": "DateTime";
                            case "date": return type.Nullable ? "DateOnly?" : "DateOnly";
                            case "uuid": return "Guid";
                            case null: return "string";
                            default: return "test";
                        }
                    case JsonSchemaType.Boolean: return type.Nullable ? "bool?" : "bool";
                    case JsonSchemaType.Integer: return "int";
                    case JsonSchemaType.Number: return "float";
                    case JsonSchemaType.Object: return type.Type.Value.ToString();
                    case JsonSchemaType.Array: return "ICollection<" + (type.Items.Reference != null ? type.Items.Reference.Id : type.Items.Type) + ">";
                    default:
                        return "boh";
                }
            }

            if (type.AllOf.Count > 0)
            {
                return type.AllOf.First().Reference.Id;
            }

            return type.ToString(); // TODO gestire
        }

        /// <summary>
        ///     Cinverte la signature di un metodo
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">Metodo da analizzare</param>
        /// <param name="a">String builder da usare</param>
        /// <param name="inter">Indica se usare signature come metodo o interfaccia</param>
        private static string ConvertMethodSignature(string key, OpenApiOperation value, bool inter)
        {
            string result = string.Empty;
            var resp = value.Responses.First().Value.Content.Values.FirstOrDefault()?.Schema;
            var responseType = Convert(resp);
            var finalresponse = responseType == string.Empty ? "Task" : $"Task<{responseType}>";

            var par = value.Parameters.Select(x => Convert(x.Schema) + " " + x.Name).ToList();

            if (value.RequestBody != null)
            {
                par.Add(value.RequestBody.Content.First().Value.Schema.AllOf.First().Reference.Id + " param");
            }

            var parameters = String.Join(", ", par);

            var name = value.OperationId.Replace("_", "");

            result += $"   /// <summary> " + Environment.NewLine;
            result += $"   ///   {value.Summary} " + Environment.NewLine;
            result += $"   /// </summary> " + Environment.NewLine;
            result += $"   public {finalresponse} {name}Async({parameters})" + (inter ? ";" : "{") + Environment.NewLine;
            result += Environment.NewLine;

            return result;
        }

        /// <summary>
        ///     Converte il corpo di un metodo
        /// </summary>
        /// <param name="key">Path della chiamata</param>
        /// <param name="method">Metodo usato</param>
        /// <param name="value">Descrizione della chiamata</param>
        /// <param name="a">String buildre da usare</param>
        private static string ConvertMethodBody(string key, string method, OpenApiOperation value)
        {
            string result = String.Empty;
            var resp = value.Responses.First().Value.Content.Values.FirstOrDefault()?.Schema;
            var responseType = Convert(resp);
            var finalresponse = responseType == string.Empty ? "" : $"<{responseType}>";

            var parameters = String.Join(", ", value.Parameters.Select(x => Convert(x.Schema) + " " + x.Name));

            var name = value.OperationId.Replace("_", "");
            result += "     Object localVarPostBody = new { };" + Environment.NewLine;

            if (value.RequestBody != null)
            {
                result += "     localVarPostBody = " + value.RequestBody.Content.First().Value.Schema.AllOf.First().Reference.Id + " ;" + Environment.NewLine;
            }
            result += "" + Environment.NewLine;

            result += "     var localVarQueryParams = new {" + Environment.NewLine;
            foreach (var parameter in value.Parameters.Where(x => x.In == ParameterLocation.Query))
            {
                result += $"       {parameter.Name}" + Environment.NewLine;
            }
            result += "     };" + Environment.NewLine;
            result += "" + Environment.NewLine;

            result += $"     return this.SendAsync{finalresponse}(\"{method}\", $\"{key}\", localVarQueryParams, localVarPostBody);" + Environment.NewLine;

            result += "   }" + Environment.NewLine;
            result += Environment.NewLine;

            return result;
        }
    }
}
