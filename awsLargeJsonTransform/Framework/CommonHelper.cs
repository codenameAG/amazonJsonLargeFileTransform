using System.IO.Compression;

namespace ca.awsLargeJsonTransform.Framework
{
    public static class CommonHelper
    {
        static CommonHelper() { }
        public static string Decompress(string filePath)
        {
            string newFileName = "";
            var fileToDecompress = new FileInfo(filePath);
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
            return newFileName;
        }
    }
}
