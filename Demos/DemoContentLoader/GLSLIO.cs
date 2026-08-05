using System.IO;

namespace DemoContentLoader
{
    public class GLSLIO
    {
        public static GLSLContent Load(BinaryReader reader) =>
            new(reader.ReadString());
        public static void Save(GLSLContent content, BinaryWriter writer) =>
            writer.Write(content.Source);
    }
}
