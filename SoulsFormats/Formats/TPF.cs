﻿using System.Collections.Generic;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// A multi-file DDS container used in DS1, DSR, DS2, DS3, DeS, BB, and NB.
    /// </summary>
    public class TPF
    {
        #region Public Read
        /// <summary>
        /// Reads an array of bytes as a TPF.
        /// </summary>
        public static TPF Read(byte[] bytes)
        {
            BinaryReaderEx br = new BinaryReaderEx(false, bytes);
            return new TPF(br);
        }

        /// <summary>
        /// Reads a file as a TPF using file streams.
        /// </summary>
        public static TPF Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                BinaryReaderEx br = new BinaryReaderEx(false, stream);
                return new TPF(br);
            }
        }
        #endregion

        /// <summary>
        /// The textures contained within this TPF.
        /// </summary>
        public List<Texture> Textures;

        private byte flag1, flag2, encoding, flag4;

        private TPF(BinaryReaderEx br)
        {
            br.AssertASCII("TPF\0");
            int totalFileSize = br.ReadInt32();
            int fileCount = br.ReadInt32();

            flag1 = br.ReadByte();
            flag2 = br.ReadByte();
            encoding = br.AssertByte(1, 2);
            flag4 = br.ReadByte();

            Textures = new List<Texture>();
            for (int i = 0; i < fileCount; i++)
            {
                Textures.Add(new Texture(br, encoding));
            }
        }

        #region Public Write
        /// <summary>
        /// Writes a TPF file to an array of bytes.
        /// </summary>
        public byte[] Write()
        {
            BinaryWriterEx bw = new BinaryWriterEx(false);
            Write(bw);
            return bw.FinishBytes();
        }

        /// <summary>
        /// Writes a TPF file to the specified path using file streams.
        /// </summary>
        public void Write(string path)
        {
            using (FileStream stream = File.Create(path))
            {
                BinaryWriterEx bw = new BinaryWriterEx(false, stream);
                Write(bw);
                bw.Finish();
            }
        }
        #endregion

        private void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("TPF\0");
            bw.ReserveInt32("DataSize");
            bw.WriteInt32(Textures.Count);
            bw.WriteByte(flag1);
            bw.WriteByte(flag2);
            bw.WriteByte(encoding);
            bw.WriteByte(flag4);

            for (int i = 0; i < Textures.Count; i++)
            {
                Textures[i].Write(bw, i);
            }
            bw.Pad(0x10);

            for (int i = 0; i < Textures.Count; i++)
            {
                Texture texture = Textures[i];
                bw.FillInt32($"FileName{i}", (int)bw.Position);
                if (encoding == 1)
                    bw.WriteUTF16(texture.Name, true);
                else if (encoding == 2)
                    bw.WriteASCII(texture.Name, true);
            }

            int dataStart = (int)bw.Position;
            for (int i = 0; i < Textures.Count; i++)
            {
                Texture texture = Textures[i];
                if (texture.Bytes.Length > 0)
                    bw.Pad(0x10);

                bw.FillInt32($"FileData{i}", (int)bw.Position);
                bw.WriteBytes(texture.Bytes);
            }
            bw.FillInt32("DataSize", (int)bw.Position - dataStart);
        }

        /// <summary>
        /// A DDS texture in a TPF container.
        /// </summary>
        public class Texture
        {
            /// <summary>
            /// The name of the texture; should not include a path or extension.
            /// </summary>
            public string Name;

            /// <summary>
            /// Flags indicating something or other.
            /// </summary>
            public int Flags1, Flags2;

            /// <summary>
            /// The raw data of the texture.
            /// </summary>
            public byte[] Bytes;

            internal Texture(BinaryReaderEx br, byte encoding)
            {
                int fileOffset = br.ReadInt32();
                int fileSize = br.ReadInt32();
                Flags1 = br.ReadInt32();
                int nameOffset = br.ReadInt32();
                Flags2 = br.ReadInt32();

                Bytes = br.GetBytes(fileOffset, fileSize);
                if (encoding == 1)
                    Name = br.GetUTF16(nameOffset);
                else if (encoding == 2)
                    Name = br.GetASCII(nameOffset);
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.ReserveInt32($"FileData{index}");
                bw.WriteInt32(Bytes.Length);
                bw.WriteInt32(Flags1);
                bw.ReserveInt32($"FileName{index}");
                bw.WriteInt32(Flags2);
            }
        }
    }
}
