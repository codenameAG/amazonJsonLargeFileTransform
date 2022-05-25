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



////string path = @"C:\01\prh\gitFinal\oi.prh.JsonTransform\files\search_terms.json";
////var data = JsonConvert.DeserializeObject(path);
////Console.WriteLine("File read complete!");
////path = @"C:\01\prh\gitFinal\oi.prh.JsonTransform\files\search_terms_indented.json";
////string content = JsonConvert.SerializeObject(data, Formatting.Indented);
////Console.WriteLine("File indent complete!");
////File.WriteAllText(path, content);
////Console.WriteLine("File indented write complete!");


//Console.WriteLine("Start!");
//string filePath = @"C:\01\prh\gitFinal\oi.prh.JsonTransform\files\search_terms.json";
//DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(object));
//using (StreamReader sr = new StreamReader(filePath))
//{
//    String line;
//    object myPerson;
//    // Read and display lines from the file until the end of
//    // the file is reached.
//    while ((line = sr.ReadLine()) != null)
//    {
//        byte[] jsonBytes = Encoding.UTF8.GetBytes(line);
//        XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(jsonBytes, XmlDictionaryReaderQuotas.Max);
//        myPerson = ser.ReadObject(jsonReader);
//        jsonReader.Close();

//        //var data = (object)myPerson;
//    }
//}
//Console.WriteLine("done!");