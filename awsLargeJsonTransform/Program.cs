// See https://aka.ms/new-console-template for more information
using ca.awsLargeJsonTransform.Framework;
using ca.awsLargeJsonTransform.Services;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;


using (IJsonLargeFileProcessService service = new JsonLargeFileProcessService())
{
    service.Execute();
}
