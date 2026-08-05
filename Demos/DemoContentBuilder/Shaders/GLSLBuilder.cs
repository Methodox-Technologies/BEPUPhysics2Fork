using System.IO;
using DemoContentLoader;

namespace DemoContentBuilder.Shaders
{
    public static class GLSLBuilder
    {
        public static GLSLContent Build(Stream dataStream)
        {
            using (StreamReader reader = new StreamReader(dataStream))
                return new GLSLContent(reader.ReadToEnd());
        }
    }
}
