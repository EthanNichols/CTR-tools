﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace cseq
{
    class BinaryWriterEx : BinaryWriter
    {
        public BinaryWriterEx(MemoryStream ms) : base(ms)
        {
        }

        public BinaryWriterEx(FileStream ms) : base(ms)
        {
        }

        public void WriteBig(int value)
        {
            byte[] x = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++) Write(x[3-i]);
        }

    }
}
