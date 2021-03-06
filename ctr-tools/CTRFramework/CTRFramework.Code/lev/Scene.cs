﻿using CTRFramework.Shared;
using CTRFramework.Vram;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;

namespace CTRFramework
{
    public class Scene : IRead, IDisposable
    {
        public string path;
        public string name;

        public SceneHeader header;
        public MeshInfo meshinfo;

        public List<Vertex> verts = new List<Vertex>();
        public List<VertexAnim> vertanims = new List<VertexAnim>();
        public List<QuadBlock> quads = new List<QuadBlock>();
        public List<PickupHeader> pickups = new List<PickupHeader>();
        public List<VisData> visdata = new List<VisData>();
        public List<LODModel> dynamics = new List<LODModel>();
        public SkyBox skybox;
        public Nav nav;

        public UnkAdv unkadv;
        public TrialData trial;

        public List<PosAng> restartPts = new List<PosAng>();

        public Tim ctrvram;

        public static Scene FromFile(string fn)
        {
            return new Scene(fn);
        }
        public Scene(string s)
        {
            path = s;
            name = Path.GetFileNameWithoutExtension(s);

            MemoryStream ms = new MemoryStream(File.ReadAllBytes(s));
            BinaryReaderEx br = new BinaryReaderEx(ms);

            int size = br.ReadInt32();

            //this is for files with external offs file
            if ((size & 0xFF) == 0x80)
            {
                ms = new MemoryStream(br.ReadBytes(size));
                br = new BinaryReaderEx(ms);
            }
            else
            {
                ms = new MemoryStream(br.ReadBytes((int)br.BaseStream.Length - 4));
                br = new BinaryReaderEx(ms);
            }

            Read(br);


            string vrmpath = Path.ChangeExtension(s, ".vram");

            if (File.Exists(vrmpath))
            {
                Console.WriteLine("VRAM found!");
                ctrvram = CtrVrm.FromFile(vrmpath);
            }
        }


        public List<Vector3s> posu2 = new List<Vector3s>();
        public List<PosAng> posu1 = new List<PosAng>();

        public void Read(BinaryReaderEx br)
        {
            //data that seems to be present in every level
            header = Instance<SceneHeader>.FromStream(br, 0);
            meshinfo = Instance<MeshInfo>.FromStream(br, header.ptrMeshInfo);
            verts = InstanceList<Vertex>.FromStream(br, meshinfo.ptrVertexArray, meshinfo.cntVertex);
            restartPts = InstanceList<PosAng>.FromStream(br, header.ptrRestartPts, header.cntRestartPts);
            visdata = InstanceList<VisData>.FromStream(br, meshinfo.ptrColDataArray, meshinfo.cntColData);
            quads = InstanceList<QuadBlock>.FromStream(br, meshinfo.ptrQuadBlockArray, meshinfo.cntQuadBlock);

            //optional stuff, can be missing
            if (header.ptrSkybox != 0) skybox = Instance<SkyBox>.FromStream(br, header.ptrSkybox);
            if (header.ptrVcolAnim != 0) vertanims = InstanceList<VertexAnim>.FromStream(br, header.ptrVcolAnim, header.cntVcolAnim);
            if (header.ptrAiNav != 0) nav = Instance<Nav>.FromStream(br, header.ptrAiNav);
            if (header.ptrTrialData != 0) trial = Instance<TrialData>.FromStream(br, header.ptrTrialData);

            if (header.cntSpawnPts != 0)
            {
                br.Jump(header.ptrSpawnPts);
                unkadv = new UnkAdv(br, (int)header.cntSpawnPts);
            }


            if (header.cntTrialData != 0)
            {
                br.Jump(header.ptrTrialData);

                int cnt = br.ReadInt32();
                int ptr = br.ReadInt32();

                br.Jump(ptr);

                for (int i = 0; i < cnt; i++)
                    posu1.Add(new PosAng(br));
            }


            if (header.cntu2 != 0)
            {
                br.Jump(header.ptru2);

                int cnt = br.ReadInt32();
                int ptr = br.ReadInt32();


                br.Jump(ptr);

                for (int i = 0; i < cnt; i++)
                    posu2.Add(new Vector3s(br));
            }

            /*
             //water texture
            br.BaseStream.Position = header.ptrWater;

            List<uint> vptr = new List<uint>();
            List<uint> wptr = new List<uint>();

            for (int i = 0; i < header.cntWater; i++)
            {
                vptr.Add(br.ReadUInt32());
                wptr.Add(br.ReadUInt32());
            }

            wptr.Sort();

            foreach(uint u in wptr)
            {
                Console.WriteLine(u.ToString("X8"));
            }

            Console.ReadKey();
            */

            //read pickups
            for (int i = 0; i < header.numPickupHeaders; i++)
            {
                br.Jump(header.ptrPickupHeadersPtrArray + 4 * i);
                br.Jump(br.ReadUInt32());

                pickups.Add(new PickupHeader(br));
            }

            //read pickup models
            //starts out right, but anims ruin it

            br.Jump(header.ptrPickupModelsPtr);
            int x = (int)br.BaseStream.Position;

            for (int i = 0; i < header.numPickupModels; i++)
            {
                br.BaseStream.Position = x + 4 * i;
                br.BaseStream.Position = br.ReadUInt32();

                dynamics.Add(new LODModel(br));
            }
        }



        public void ExportMesh(string dir, Detail lod)
        {
            if (quads.Count > 0)
            {
                string fname = name + "_" + lod.ToString();

                StringBuilder sb = new StringBuilder();

                sb.Append("#Converted to OBJ using model_reader, CTR-Tools by DCxDemo*.\r\n");
                sb.Append("#" + Meta.GetVersion()+"\r\n");
                sb.Append("#(C) 1999, Activision, Naughty Dog.\r\n\r\n");
                sb.Append("mtllib " + Path.GetFileName(fname + ".mtl") + "\r\n\r\n");

                int a = 0;
                int b = 0;

                foreach (QuadBlock g in quads)
                    sb.AppendLine(g.ToObj(verts, lod, ref a, ref b));

                Helpers.WriteToFile(Path.Combine(dir, fname + ".obj"), sb.ToString());

                sb.Clear();

                Dictionary<string, TextureLayout> tex = GetTexturesList(lod);

                foreach (var s in tex.Values)
                {
                    if (s.Position != 0)
                    {
                        sb.Append(String.Format("newmtl {0}\r\n", s.Tag()));
                        sb.Append(String.Format("map_Kd tex{0}\\{1}.png\r\n\r\n", lod.ToString(), s.Tag()));

                        if (!File.Exists(Path.Combine(dir, "tex" + lod.ToString() + "\\" + s.Tag() + ".png")))
                        {
                            Bitmap bmp = new Bitmap(1, 1);
                            bmp.Save(Path.Combine(dir, "tex" + lod.ToString() + "\\" + s.Tag() + ".png"));
                        }
                    }
                }

                sb.Append("newmtl default\r\n");
                //sb.Append("map_kd tex\\default.png\r\n");

                Helpers.WriteToFile(Path.Combine(dir, fname + ".mtl"), sb.ToString());

                sb.Clear();
            }
        }

        public void ExportModels(string dir)
        {
            foreach (LODModel m in dynamics)
            {
                foreach (LODHeader lh in m.lh)
                {
                    string fn = Path.Combine(dir, String.Format("models\\{0}\\{1}.obj", m.name, lh.name));
                    Helpers.WriteToFile(fn, lh.ToObj());
                }
            }

            Console.WriteLine("Models done!");
        }

        public void ExportSkyBox(string dir)
        {
            if (skybox != null)
                Helpers.WriteToFile(Path.Combine(dir, name + "_sky.obj"), skybox.ToObj());
        }

        public void ExportAll(string dir, ExportFlags flags)
        {
            Console.WriteLine("Exporting to: " + dir);
            if (dir.Contains(" "))
                Console.WriteLine("Warning, there are spaces in the path. This may affect material import.");

            if (flags.HasFlag(ExportFlags.TexLow)) ExportTextures(Path.Combine(dir, "texLow"), Detail.Low);
            if (flags.HasFlag(ExportFlags.MeshLow)) ExportMesh(dir, Detail.Low);

            if (flags.HasFlag(ExportFlags.TexMed)) ExportTextures(Path.Combine(dir, "texMed"), Detail.Med);
            if (flags.HasFlag(ExportFlags.MeshMed)) ExportMesh(dir, Detail.Med);

            if (flags.HasFlag(ExportFlags.Models)) ExportModels(dir);
            if (flags.HasFlag(ExportFlags.SkyBox)) ExportSkyBox(dir);
        }

        public void ExportTextures(string dir, Detail lod)
        {
            if (ctrvram != null)
            {
                // ctrvram.SaveBMP("lol.bmp", BMPHeader.GrayScalePalette(16));

                Console.WriteLine(ctrvram.ToString());
                Console.WriteLine("Exporting textures...");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                foreach (TextureLayout tl in GetTexturesList(lod).Values)
                    ctrvram.GetTexture(tl, dir);

                Console.WriteLine("Textures done!");
            }
            else
            {
                Console.WriteLine("No vram found. Make sure to copy vram file along with lev.");
            }
        }

        public void ExportTexturesAll(string dir)
        {
            if (ctrvram != null)
            {
                // ctrvram.SaveBMP("lol.bmp", BMPHeader.GrayScalePalette(16));

                Console.WriteLine(ctrvram.ToString());
                Console.WriteLine("Exporting textures...");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                foreach (TextureLayout tl in GetTexturesList().Values)
                    ctrvram.GetTexture(tl, dir);

                Console.WriteLine("Textures done!");
            }
            else
            {
                Console.WriteLine("No vram found. Make sure to copy vram file along with lev.");
            }
        }

        public string Info()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}: {1}\r\n", "verts", verts.Count);
            sb.AppendFormat("{0}: {1}\r\n", "primitives", quads.Count);
            sb.AppendFormat("{0}: {1}\r\n", "lo quads", quads.Count);
            sb.AppendFormat("{0}: {1}\r\n", "lo tris", quads.Count * 2);
            sb.AppendFormat("{0}: {1}\r\n", "hi quads", quads.Count * 4);
            sb.AppendFormat("{0}: {1}\r\n", "hi tris", quads.Count * 4 * 2);
            sb.AppendFormat("{0}: {1}\r\n", "skybox verts", (skybox != null ? skybox.verts.Count : 0));
            sb.AppendFormat("{0}: {1}\r\n", "skybox tris", (skybox != null ? skybox.faces.Count : 0));

            return sb.ToString();
        }

        public string InfoCsv()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0},", verts.Count);
            sb.AppendFormat("{0},", quads.Count);
            sb.AppendFormat("{0},", quads.Count);
            sb.AppendFormat("{0},", quads.Count * 2);
            sb.AppendFormat("{0},",  quads.Count * 4);
            sb.AppendFormat("{0},", quads.Count * 4 * 2);
            sb.AppendFormat("{0},", (skybox != null ? skybox.verts.Count : 0));
            sb.AppendFormat("{0},", (skybox != null ? skybox.faces.Count : 0));

            return sb.ToString();
        }


        public Dictionary<string, TextureLayout> GetTexturesList(Detail lod)
        {
            Dictionary<string, TextureLayout> tex = new Dictionary<string, TextureLayout>();

            foreach (QuadBlock qb in quads)
            {
                switch (lod)
                {
                    case Detail.Low:
                        if (qb.ptrTexLow != 0)
                            if (!tex.ContainsKey(qb.texlow.Tag()))
                                tex.Add(qb.texlow.Tag(), qb.texlow);
                        break;
                    case Detail.Med:
                        foreach (CtrTex t in qb.tex)
                            if (t.midlods[2].Position != 0)
                                if (!tex.ContainsKey(t.midlods[2].Tag()))
                                    tex.Add(t.midlods[2].Tag(), t.midlods[2]);
                        break;
                }
            }

            return tex;
        }


        public Dictionary<string, TextureLayout> GetTexturesList()
        {
            Dictionary<string, TextureLayout> tex = new Dictionary<string, TextureLayout>();

            foreach (QuadBlock qb in quads)
            {
                if (qb.ptrTexLow != 0)
                {
                    if (!tex.ContainsKey(qb.texlow.Tag()))
                    {
                        tex.Add(qb.texlow.Tag(), qb.texlow);
                    }
                }

                foreach (CtrTex t in qb.tex)
                    foreach (TextureLayout tl in t.midlods)
                    {
                        if (!tex.ContainsKey(tl.Tag()))
                        {
                            tex.Add(tl.Tag(), tl);
                        }
                    }
                /*
                foreach (CtrTex t in qb.tex)
                {
                    foreach (TextureLayout tl in t.hi)
                    {
                        if (tl != null)
                            if (!tex.ContainsKey(tl.Tag()))
                            {
                                tex.Add(tl.Tag(), tl);
                            }
                    }
                }
                */

            }

            /*
            List<string> tt = new List<string>();
            StringBuilder sb = new StringBuilder();

            foreach (string s in tex.Keys) tt.Add(s);
            tt.Sort();
            foreach (string s in tt) sb.AppendLine(s);
            */
            //File.WriteAllText("texture.txt", sb.ToString());

            return tex;
        }

        public void Dispose()
        {
            header = null;
            meshinfo = null;
            verts.Clear();
            vertanims.Clear();
            quads.Clear();
            pickups.Clear();
            visdata.Clear();
            dynamics.Clear();
            skybox = null;
            nav = null;
            unkadv = null;
            restartPts.Clear();
            ctrvram = null;
        }
    }
}