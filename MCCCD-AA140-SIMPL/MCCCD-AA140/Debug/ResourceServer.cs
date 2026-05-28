// ResourceServer — reads embedded debug UI files from the assembly manifest.
// Resources are embedded via <EmbeddedResource> in the csproj with LogicalName
// "MCCCD_AA140.Resources.debug.{html|js|css}".

using System.IO;
using System.Reflection;

namespace MCCCD_AA140.Debug
{
    public static class ResourceServer
    {
        public class Resource
        {
            public byte[] Bytes;
            public string ContentType;
        }

        private const string Prefix = "MCCCD_AA140.Resources.";

        public static Resource Get(string fileName)
        {
            string contentType;
            switch (fileName) {
                case "debug.html": contentType = "text/html; charset=utf-8"; break;
                case "debug.js":   contentType = "application/javascript";   break;
                case "debug.css":  contentType = "text/css; charset=utf-8";  break;
                default: return null;
            }

            var asm = Assembly.GetExecutingAssembly();
            using (var s = asm.GetManifestResourceStream(Prefix + fileName)) {
                if (s == null) return null;
                using (var ms = new MemoryStream()) {
                    s.CopyTo(ms);
                    return new Resource { Bytes = ms.ToArray(), ContentType = contentType };
                }
            }
        }

        public static string[] ListEmbeddedNames()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames();
        }
    }
}
