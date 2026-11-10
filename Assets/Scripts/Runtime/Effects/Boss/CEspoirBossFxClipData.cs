using System;
using System.IO;
using System.Text;
using UnityEngine;

public class CEspoirBossFxClipData
{
    public const int U_FLOAT = 0,
        U_VEC = 1,
        U_ARRAY = 2;
    public const int F_VIS = 1,
        F_MAT = 2,
        F_BB = 4,
        F_UNI = 8,
        F_BOLT = 16,
        F_GEO = 32,
        F_GONE = 64;
    public const int G_T = 0,
        G_SHAKE = 1,
        G_FOV = 2,
        G_CA = 3,
        G_EXPOSURE = 4,
        G_INK = 5,
        G_IMPMODE = 6,
        G_IMPSEED = 7,
        G_HITSTOP = 8,
        G_ORB = 9,
        G_LIGHT = 13,
        G_COUNT = 21;

    public string clip;
    public float dt;
    public int frames;
    public Vector3 pivot;
    public float[] globals;
    public CShaderData[] shaders;
    public CGeometry[] geoms;
    public CEffectObject[] objs;
    public int[][] startsAt;

    public float G(int frame, int ch) => globals[frame * G_COUNT + ch];

    public class CUniform
    {
        public string name;
        public int id;
        public int kind,
            comps,
            arrayLen;
        public bool isColor;
    }

    public class CShaderData
    {
        public string name;
        public CUniform[] uniforms;
    }

    public class CGeometry
    {
        public int vcount;
        public Vector3[] pos,
            nrm;
        public Vector2[] uv;
        public Vector4[][] slots;
        public int[] idx;
        public int drawStart,
            drawCount;
    }

    public struct SUniformValue
    {
        public int index;
        public float[] v;
    }

    public class CFrameKey
    {
        public int frame,
            flags;
        public bool vis;
        public Matrix4x4 matrix;
        public int bb;
        public SUniformValue[] uni;
        public float boltW;
        public float[] boltPts;
        public int geo = -1;
    }

    public class CEffectObject
    {
        public int shader,
            geom;
        public int src,
            dst,
            zwrite,
            ztest,
            cull;
        public bool distort;
        public int queue,
            first;
        public CFrameKey[] keys;
    }

    public static CEspoirBossFxClipData Parse(byte[] bytes)
    {
        using (var ms = new MemoryStream(bytes))
        using (var r = new BinaryReader(ms))
        {
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "EBF1")
                throw new Exception("EspoirBossFx: 데이터 형식이 아님");
            r.ReadInt32();
            var d = new CEspoirBossFxClipData();
            d.clip = Str(r);
            d.dt = r.ReadSingle();
            d.frames = r.ReadInt32();
            d.pivot = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            d.globals = Floats(r, d.frames * G_COUNT);

            d.shaders = new CShaderData[r.ReadInt32()];
            for (int i = 0; i < d.shaders.Length; i++)
            {
                var s = new CShaderData { name = Str(r) };
                s.uniforms = new CUniform[r.ReadInt32()];
                for (int u = 0; u < s.uniforms.Length; u++)
                {
                    var un = new CUniform
                    {
                        name = Str(r),
                        kind = r.ReadByte(),
                        comps = r.ReadByte(),
                        arrayLen = r.ReadInt32(),
                    };
                    if (s.name == "Basic")
                        un.name = un.name == "color" ? "_Color" : "_Opacity";
                    un.id = UnityEngine.Shader.PropertyToID(un.name);
                    s.uniforms[u] = un;
                }
                d.shaders[i] = s;
            }

            d.geoms = new CGeometry[r.ReadInt32()];
            for (int i = 0; i < d.geoms.Length; i++)
            {
                var g = new CGeometry { vcount = r.ReadInt32() };
                bool hn = r.ReadByte() != 0,
                    hu = r.ReadByte() != 0;
                int ns = r.ReadInt32();
                g.pos = V3(r, g.vcount);
                if (hn)
                    g.nrm = V3(r, g.vcount);
                if (hu)
                    g.uv = V2(r, g.vcount);
                g.slots = new Vector4[ns][];
                for (int s = 0; s < ns; s++)
                    g.slots[s] = V4(r, g.vcount);
                int ni = r.ReadInt32();
                if (ni >= 0)
                {
                    g.idx = new int[ni];
                    for (int k = 0; k < ni; k++)
                        g.idx[k] = r.ReadInt32();
                }
                g.drawStart = r.ReadInt32();
                g.drawCount = r.ReadInt32();
                d.geoms[i] = g;
            }

            d.objs = new CEffectObject[r.ReadInt32()];
            var starts = new System.Collections.Generic.List<int>[d.frames];
            for (int i = 0; i < d.objs.Length; i++)
            {
                var o = new CEffectObject { shader = r.ReadInt32(), geom = r.ReadInt32() };
                o.src = r.ReadByte();
                o.dst = r.ReadByte();
                o.zwrite = r.ReadByte();
                o.ztest = r.ReadByte();
                o.cull = r.ReadByte();
                o.distort = r.ReadByte() != 0;
                o.queue = r.ReadInt32();
                o.first = r.ReadInt32();
                o.keys = new CFrameKey[r.ReadInt32()];
                for (int k = 0; k < o.keys.Length; k++)
                {
                    var key = new CFrameKey { frame = r.ReadInt32(), flags = r.ReadByte() };
                    if ((key.flags & F_VIS) != 0)
                        key.vis = r.ReadByte() != 0;
                    if ((key.flags & F_MAT) != 0)
                    {
                        var m = new Matrix4x4();
                        for (int e = 0; e < 16; e++)
                            m[e % 4, e / 4] = r.ReadSingle();
                        key.matrix = m;
                    }
                    if ((key.flags & F_BB) != 0)
                        key.bb = r.ReadByte();
                    if ((key.flags & F_UNI) != 0)
                    {
                        key.uni = new SUniformValue[r.ReadInt32()];
                        for (int u = 0; u < key.uni.Length; u++)
                        {
                            int idx = r.ReadInt32();
                            key.uni[u] = new SUniformValue
                            {
                                index = idx,
                                v = Floats(r, r.ReadInt32()),
                            };
                        }
                    }
                    if ((key.flags & F_BOLT) != 0)
                    {
                        key.boltW = r.ReadSingle();
                        key.boltPts = Floats(r, r.ReadInt32());
                    }
                    if ((key.flags & F_GEO) != 0)
                        key.geo = r.ReadInt32();
                    o.keys[k] = key;
                }
                d.objs[i] = o;
                int f0 = Mathf.Clamp(o.first, 0, d.frames - 1);
                (starts[f0] ?? (starts[f0] = new System.Collections.Generic.List<int>())).Add(i);
            }
            d.startsAt = new int[d.frames][];
            for (int f = 0; f < d.frames; f++)
                d.startsAt[f] = starts[f]?.ToArray();
            return d;
        }
    }

    static string Str(BinaryReader r) => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));

    static float[] Floats(BinaryReader r, int n)
    {
        var a = new float[n];
        for (int i = 0; i < n; i++)
            a[i] = r.ReadSingle();
        return a;
    }

    static Vector3[] V3(BinaryReader r, int n)
    {
        var a = new Vector3[n];
        for (int i = 0; i < n; i++)
            a[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        return a;
    }

    static Vector2[] V2(BinaryReader r, int n)
    {
        var a = new Vector2[n];
        for (int i = 0; i < n; i++)
            a[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
        return a;
    }

    static Vector4[] V4(BinaryReader r, int n)
    {
        var a = new Vector4[n];
        for (int i = 0; i < n; i++)
            a[i] = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        return a;
    }
}
