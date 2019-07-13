﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace model_reader
{
    class TextureLayout : IRead
    {
        public List<Vector2b> uv = new List<Vector2b>();

        public ushort PalX;
        public ushort PalY;

        public ushort PageX;
        public ushort PageY;


        public TextureLayout(BinaryReader br)
        {
            Read(br);
        }

        public void Read(BinaryReader br)
        {
            uv.Add(new Vector2b(br));

            ushort buf = br.ReadUInt16();

            PalX = (ushort)(buf & 0x3F);
            PalY = (ushort)(buf >> 6);

            uv.Add(new Vector2b(br));

            buf = br.ReadByte();

            PageX = (ushort)(buf & 0xF);
            PageY = (ushort)((buf >> 4) & 1);

            //checking page byte 2 if it's ever not 0
            if (br.ReadByte() != 0)
            {
                Console.Write("---WTF---page 2nd byte != 0");
                Console.ReadKey();
            }

            uv.Add(new Vector2b(br));
            uv.Add(new Vector2b(br));

            Console.WriteLine("done texture layout\r\n" + ToString());
        }

        public override string ToString()
        {
            return
                uv[0].ToString() + "|\t" +
                uv[1].ToString() + "|\t" +
                uv[2].ToString() + "|\t" +
                uv[3].ToString() + "|\t" +
                PalX + ", " + PalY + "|\t" +
                PageX + ", " + PageY;
        }

        public string ToObj()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Vector2b v in uv)
                sb.AppendLine(
                    String.Format(
                        "vt {0} {1}", 
                        Math.Round(v.X / 256f, 3).ToString(),  
                        Math.Round(v.Y / 256f, 3).ToString()
                    )
                );

            return sb.ToString();
        }
    }
}