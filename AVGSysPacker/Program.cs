using AVGSysMan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AVGSysPacker {
    class Program {

        static string Executable = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
        static void Main(string[] args) {
            if (args == null || args.Length == 0) {
                Console.WriteLine("Usage:");
                Console.WriteLine("Extract: {0} \"C:\\Sample.pak\"", Executable);
                Console.WriteLine("Repack: {0} \"C:\\DirToPack\" \"C:\\NewPackget.pak\"", Executable);
                Console.ReadKey();
                return;
            }

            if (args.Length == 1 && System.IO.File.Exists(args[0]))
                Unpack(args[0]);

            if (args.Length == 2 && Directory.Exists(args[0]))
                Pack(args[0], args[1]);
        }

        static void Unpack(string PakPath) {
            Stream Packget = new StreamReader(PakPath).BaseStream;
            var Files = PAK.Open(Packget);

            string ExDir = PakPath + "~\\";
            foreach (var File in Files) {
                string Path = ExDir + File.Path;
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));

                Console.WriteLine("{0:X8} \"{1}\"", File.Length, File.Path);
                using (var Output = new StreamWriter(Path).BaseStream) {
                    File.Content.CopyTo(Output, 1024 * 1024);
                    File.Content.Close();
                    Output.Close();
                }
            }

            Packget.Close();
        }

        static void Pack(string BaseDir, string NewPak) {
            if (!BaseDir.EndsWith("\\"))
                BaseDir += "\\";

            string[] Files = ListDir(BaseDir);
            var Entries = new AVGSysMan.File[Files.LongLength];
            for (uint i = 0; i < Entries.LongLength; i++) {
                Entries[i] = new AVGSysMan.File() {
                    Path = Files[i],
                    Content = new StreamReader(BaseDir + Files[i]).BaseStream
                };
            }

            int Prog = 0;
            PAK.Save(Entries, new StreamWriter(NewPak).BaseStream, true, (a) => {
                Console.WriteLine("[{1}%] Compressing: {0}", a, (uint)(((double)++Prog/Files.LongLength)*100));
            });
        }
        static string[] ListDir(string Dir) {
            string[] Files = Directory.GetFiles(Dir, "*.*", SearchOption.AllDirectories);
            for (uint i = 0; i < Files.LongLength; i++) {
                Files[i] = Files[i].Substring(Dir.Length, Files[i].Length - Dir.Length);
            }

            return Files;
        }
    }
}
