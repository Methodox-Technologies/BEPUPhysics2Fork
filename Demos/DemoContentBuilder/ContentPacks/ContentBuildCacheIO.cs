using System;
using System.Collections.Generic;
using System.IO;
using DemoContentLoader;

namespace DemoContentBuilder.ContentPacks
{
    public struct ContentElement
    {
        public long LastModifiedTimestamp;
        public IContent Content;
    }

    /// <summary>
    /// Stores built content and associated timestamps needed to know which content needs to be freshly built.
    /// </summary>
    public static class ContentBuildCacheIO
    {
        public static void Save(Dictionary<string, ContentElement> cache, string path)
        {
            using (FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(cache, stream);
            }
        }

        public static void Save(Dictionary<string, ContentElement> cache, Stream outputStream)
        {
            //Save the number of shaders.
            using (BinaryWriter writer = new(outputStream))
            {
                writer.Write(cache.Count);

                //Save every element in sequence.
                foreach (KeyValuePair<string, ContentElement> element in cache)
                {
                    writer.Write(element.Key);
                    writer.Write(element.Value.LastModifiedTimestamp);
                    writer.Write((int)element.Value.Content.ContentType);
                    ContentArchive.Save(element.Value.Content, writer);
                }
            }
        }

        public static bool Load(string path, out Dictionary<string, ContentElement> cache)
        {
            if (!File.Exists(path))
            {
                cache = new Dictionary<string, ContentElement>();
                return false;
            }
            using (FileStream stream = File.OpenRead(path))
            {
                cache = Load(stream);
                return true;
            }
        }



        public static Dictionary<string, ContentElement> Load(Stream stream)
        {
            using (BinaryReader reader = new(stream))
            {
                try
                {
                    Dictionary<string, ContentElement> cache = new();
                    int contentCount = reader.ReadInt32();

                    for (int i = 0; i < contentCount; ++i)
                    {
                        string path = reader.ReadString();
                        long lastModifiedTimestamp = reader.ReadInt64();
                        ContentType contentType = (ContentType)reader.ReadInt32();
                        IContent content = ContentArchive.Load(contentType, reader);
                        cache.Add(path, new ContentElement { LastModifiedTimestamp = lastModifiedTimestamp, Content = content });
                    }
                    return cache;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Content build cache load failed; may be corrupted. Assuming fresh build. Message:");
                    Console.WriteLine(e.Message);
                    return new Dictionary<string, ContentElement>();
                }
            }
        }
    }
}
