using AdvancedBinary;
using System.IO;
using System.Text;

namespace AVGSysMan {
    public static class PAK {
        public static File[] Open(Stream Packget) {
            StructReader Reader = new StructReader(Packget, Encoding: Encoding.Unicode);
            Reader.BaseStream.Seek(-0x8, SeekOrigin.End);
            uint FileCount = Reader.ReadUInt32();
            uint HeaderPos = Reader.ReadUInt32() + 0x8;
            Reader.BaseStream.Seek(-HeaderPos, SeekOrigin.End);

            File[] Files = new File[FileCount];

            //System.Diagnostics.Debug.Assert(Reader.PeekInt() == 0);

            for (uint i = 0; i < FileCount; i++) {
                Files[i] = new File();
                Reader.ReadStruct(ref Files[i]);

                Files[i].Path = Files[i].Path.Replace("/", "\\");
                var RawReader = new VirtStream(Packget, Files[i].Offset, Files[i].cLength);
                if (Files[i].IsCompressed)
                    Files[i].Content = new ZInputStream(RawReader);
                else
                    Files[i].Content = RawReader;
            }

            return Files;
        }

        public static void Save(File[] Files, Stream Output, bool CloseStreams = true, LogOutput Log = null) {
            var Header = new StructWriter(new MemoryStream(), Encoding: Encoding.Unicode);

            for (uint i = 0; i < Files.LongLength; i++) {
                File File = Files[i];

                uint Pos = (uint)Output.Position;

                if (Log != null)
                    Log.Invoke(File.Path);

                var Compressor = new ZOutputStream(Output, (int)CompressionLevel.Z_BEST_COMPRESSION);
                File.Content.CopyTo(Compressor, 1024 * 1024);
                Compressor.Finish();
                Compressor.Flush();
                

                uint cLen = (uint)Output.Position - Pos;

                File.Offset = Pos;
                File.dLength = (uint)File.Content.Length;
                File.cLength = cLen;
                File.Path = File.Path.Replace("\\", "/");

                Header.WriteStruct(ref File);

                if (CloseStreams)
                    File.Content.Close();
            }

            uint HeaderLen = (uint)Header.BaseStream.Length;
            Header.Write((uint)Files.LongLength);
            Header.Write(HeaderLen);
            Header.Flush();

            Header.Seek(0, SeekOrigin.Begin);
            Header.BaseStream.CopyTo(Output);
            Header.Close();

            if (CloseStreams)
                Output.Close();
        }
    }

    public delegate void LogOutput(string Message);
    public struct File {
        internal uint Offset;
        internal uint cLength;//Compressed Len
        internal uint dLength;//Decompressed Len

        [PString(PrefixType = Const.UINT32, UnicodeLength = true)]
        public string Path;

        [Ignore]
        public dynamic Content;

        [Ignore]
        public uint Length { get { return dLength; } }

        [Ignore]
        public bool IsCompressed { get { return dLength != uint.MaxValue; } }
    }
}
