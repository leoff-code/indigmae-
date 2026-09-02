using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrystalSprint
{
    // Splits the imported model only when felled; UVs, normals, colours and material slots survive.
    public static class TreeMeshCut
    {
        private struct Vertex
        {
            public Vector3 p, n; public Vector2 uv; public Color color;
            public static Vertex Lerp(Vertex a, Vertex b, float t) => new()
            { p=Vector3.Lerp(a.p,b.p,t), n=Vector3.Lerp(a.n,b.n,t).normalized, uv=Vector2.Lerp(a.uv,b.uv,t), color=Color.Lerp(a.color,b.color,t) };
        }
        public static Mesh Cut(MeshFilter source, Vector3 origin, bool upper, Mesh readableSource)
        {
            Mesh mesh=readableSource;
            var v=mesh.vertices; var n=mesh.normals; var uv=mesh.uv; var colors=mesh.colors;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var tex=new List<Vector2>(); var tint=new List<Color>();
            var sections=new List<int>[mesh.subMeshCount+1]; var rim=new List<Vector3>();
            for(int s=0;s<sections.Length;s++) sections[s]=new();
            Vertex Read(int j) => new() { p=source.transform.TransformPoint(v[j])-origin,
                n=source.transform.TransformDirection(n[j]).normalized, uv=uv.Length==v.Length?uv[j]:Vector2.zero, color=colors.Length==v.Length?colors[j]:Color.white };
            void Add(Vertex a) { vertices.Add(a.p); normals.Add(a.n); tex.Add(a.uv); tint.Add(a.color); }
            for(int s=0;s<mesh.subMeshCount;s++)
            {
                var triangles=mesh.GetTriangles(s);
                for(int j=0;j<triangles.Length;j+=3)
                {
                    Vertex[] tri={Read(triangles[j]),Read(triangles[j+1]),Read(triangles[j+2])};
                    var polygon=new List<Vertex>(4);
                    for(int e=0;e<3;e++)
                    {
                        Vertex a=tri[e], b=tri[(e+1)%3];
                        bool ia=upper?a.p.y>=0:a.p.y<=0, ib=upper?b.p.y>=0:b.p.y<=0;
                        if(ia) polygon.Add(a);
                        if(ia!=ib)
                        {
                            Vertex cut=Vertex.Lerp(a,b,-a.p.y/(b.p.y-a.p.y)); polygon.Add(cut);
                            if(!rim.Exists(p=>(p-cut.p).sqrMagnitude<.000001f)) rim.Add(cut.p);
                        }
                    }
                    for(int k=1;k<polygon.Count-1;k++)
                    { int start=vertices.Count; Add(polygon[0]);Add(polygon[k]);Add(polygon[k+1]);sections[s].AddRange(new[]{start,start+1,start+2}); }
                }
            }
            if(rim.Count>=3)
            {
                Vector3 center=Vector3.zero; foreach(var p in rim)center+=p; center/=rim.Count;
                rim.Sort((a,b)=>Mathf.Atan2(a.z-center.z,a.x-center.x).CompareTo(Mathf.Atan2(b.z-center.z,b.x-center.x)));
                float diameter=.01f; foreach(var p in rim)diameter=Mathf.Max(diameter,Vector3.Distance(p,center)*2);
                Vertex Cap(Vector3 p)=>new(){p=p,n=upper?Vector3.down:Vector3.up,uv=new Vector2(p.x-center.x,p.z-center.z)/diameter+Vector2.one*.5f,color=Color.white};
                for(int j=0;j<rim.Count;j++)
                {
                    int start=vertices.Count; Add(Cap(center));Add(Cap(rim[j]));Add(Cap(rim[(j+1)%rim.Count]));
                    sections[^1].AddRange(upper?new[]{start,start+1,start+2}:new[]{start,start+2,start+1});
                }
            }
            var result=new Mesh {name=mesh.name+(upper?" Felled":" Stump"),indexFormat=IndexFormat.UInt32};
            result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,tex);result.SetColors(tint);result.subMeshCount=sections.Length;
            for(int s=0;s<sections.Length;s++)result.SetTriangles(sections[s],s);
            result.RecalculateTangents();result.RecalculateBounds();return result;
        }
    }
}
