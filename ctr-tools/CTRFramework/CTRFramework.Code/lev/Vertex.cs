﻿using CTRFramework.Shared;
using System;

namespace CTRFramework
{
    public class Vertex : IRead, IWrite
    {
        public Vector4s coord;
        public Vector4b color;
        public Vector4b color_morph;
        public Vector2b uv; //not used by CTR, added for convinience

        public Vertex()
        {
        }

        public Vertex(BinaryReaderEx br)
        {
            Read(br);
        }

        public void SetColor(Vcolor mode, Vector4b col)
        {
            switch (mode)
            {
                case Vcolor.Default: color = col; break;
                case Vcolor.Morph: color_morph = col; break;
                    //case Vcolor.Flag: Vertex.flagColor = col; break;
            }
        }

        public void ReadShort(BinaryReaderEx br)
        {
            coord = new Vector4s(br);
            color = new Vector4b(br);
        }

        public void Read(BinaryReaderEx br)
        {
            coord = new Vector4s(br);
            color = new Vector4b(br);
            color_morph = new Vector4b(br);
        }

        public void Write(BinaryWriterEx bw)
        {
            coord.Write(bw);
            color.Write(bw);
            color_morph.Write(bw);
        }

        public override string ToString()
        {
            string fmt = "v {0} {1}";

            return String.Format(fmt,
                coord.ToString(VecFormat.Numbers),
                color.ToString(VecFormat.Numbers)
            );
        }

        public string ToString(uint b)
        {
            string fmt = "v {0} {1}";

            return String.Format(fmt,
                coord.ToString(VecFormat.Numbers),
                new Vector4b(b).ToString(VecFormat.Numbers)
            );
        }
    }
}
