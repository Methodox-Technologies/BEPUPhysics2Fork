using DemoContentBuilder.Meshes;
using DemoContentBuilder.Shaders;
using DemoContentBuilder.Textures;
using DemoContentLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DemoContentBuilder.ContentPacks
{
    public struct ContentBuildResult
    {
        public string File;
        public string Message;
    }

    public struct ContentBuildInput
    {
        public string Path;
        public ContentType Type;
    }

    public static class ContentBuilder
    {
        public static void BuildContent(string workingPath, string buildCachePath, string runtimeCachePath, List<ContentBuildInput> contentToBuild,
            out List<ContentBuildResult> warnings, out List<ContentBuildResult> errors)
        {
            errors = new List<ContentBuildResult>();
            warnings = new List<ContentBuildResult>();
            bool newContentBuilt = false;
            ContentBuildCacheIO.Load(buildCachePath, out Dictionary<string, ContentElement> loadedBuildCache);
            Dictionary<string, ContentElement> newBuildCache = new();
            foreach (ContentBuildInput content in contentToBuild)
            {
                long currentTimeStamp = File.GetLastWriteTime(content.Path).Ticks;
                if (loadedBuildCache.TryGetValue(content.Path, out ContentElement cachedContent) && currentTimeStamp == cachedContent.LastModifiedTimestamp)
                {
                    //We can just used the cached version of this content.
                    newBuildCache.Add(content.Path, cachedContent);
                    Console.WriteLine($"Content up to date: {content.Path}");
                }
                else
                {
                    //This is a new or modified content element, so we'll have to build it.
                    ContentElement newElement;
                    newElement.LastModifiedTimestamp = currentTimeStamp;
                    using (FileStream stream = File.OpenRead(content.Path))
                    {
                        try
                        {
                            //Just like the ContentArchive, we're being a little lazy- this isn't very extensible, but there are very few content types.
                            //If that changes, it would be pretty easy to open this up.
                            switch (content.Type)
                            {
                                case ContentType.Font:
                                    newElement.Content = FontBuilder.Build(stream);
                                    break;
                                case ContentType.Mesh:
                                    newElement.Content = MeshBuilder.Build(stream);
                                    break;
                                case ContentType.Image:
                                    newElement.Content = Texture2DBuilder.Build(stream);
                                    break;
                                case ContentType.GLSL:
                                    newElement.Content = GLSLBuilder.Build(stream);
                                    break;
                                default:
                                    throw new ArgumentException("Requested content type does not have a registered builder.");
                            }
                            newBuildCache.Add(content.Path, newElement);
                            newContentBuilt = true;
                            Console.WriteLine($"Content built: {content.Path}");
                        }
                        catch (Exception e)
                        {
                            //You could be a little more clever with these errors by letting builders report more specific problems. We can worry about that if this ever gets generalized.
                            errors.Add(new ContentBuildResult { File = content.Path, Message = "Content build failed: " + e.Message });
                        }
                    }
                }
            }

            //If we have new OR less content, the files should be rewritten.
            if (newContentBuilt || newBuildCache.Count < loadedBuildCache.Count)
            {
                Dictionary<string, IContent> archive = new();
                foreach (KeyValuePair<string, ContentElement> pair in newBuildCache)
                {
                    //Prune out all of the extra path bits and save it.
                    string relativePath = ProjectBuilder.GetRelativePathFromDirectory(pair.Key, workingPath);
                    archive.Add(relativePath.Replace(Path.DirectorySeparatorChar, '\\'), pair.Value.Content);
                }


                const int retryCount = 10;
                const int retryDelay = 200;
                for (int i = 0; i < retryCount; ++i)
                {
                    try
                    {
                        ContentBuildCacheIO.Save(newBuildCache, buildCachePath);
                        using (FileStream stream = File.OpenWrite(runtimeCachePath))
                        {
                            ContentArchive.Save(archive, stream);
                        }
                        break;
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"Failed to write content caches (attempt {i}): {e.Message}, retrying...");
                        Thread.Sleep(retryDelay);
                    }
                }
            }
        }

    }
}
