using DemoContentLoader;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Data.VertexData;
using ObjLoader.Loader.Loaders;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DemoContentBuilder.Meshes
{
    public static class MeshBuilder
    {
        class MaterialStubLoader : IMaterialStreamProvider
        {
            public Stream Open(string materialFilePath)
            {
                return null;
            }
        }

        public static MeshContent Build(Stream dataStream)
        {
            LoadResult result = new ObjLoaderFactory().Create(new MaterialStubLoader()).Load(dataStream);
            List<TriangleContent> triangles = new List<TriangleContent>();
            for (int i = 0; i < result.Groups.Count; ++i)
            {
                Group group = result.Groups[i];
                for (int j = 0; j < group.Faces.Count; ++j)
                {
                    Face face = group.Faces[j];
                    Vertex a = result.Vertices[face[0].VertexIndex - 1];
                    for (int k = 1; k < face.Count - 1; ++k)
                    {
                        Vertex b = result.Vertices[face[k].VertexIndex - 1];
                        Vertex c = result.Vertices[face[k + 1].VertexIndex - 1];
                        triangles.Add(new TriangleContent
                        {
                            A = new Vector3(a.X, a.Y, a.Z),
                            B = new Vector3(b.X, b.Y, b.Z),
                            C = new Vector3(c.X, c.Y, c.Z)
                        });
                    }
                }
            }
            return new MeshContent(triangles.ToArray());
        }
    }
}
