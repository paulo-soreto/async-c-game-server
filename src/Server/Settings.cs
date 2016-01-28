using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    class Settings {
        static Hashtable _cache;

        public static void Initialize () {
            _cache = new Hashtable ();
            ParseConfigFile ("./conf.ini");
        }

        static void ParseConfigFile (string fileName) {
            if (!File.Exists (fileName))
                throw new Exception ("cannot find server configuration file");

            using (StreamReader reader = new StreamReader (fileName, Encoding.UTF8)) {
                string [] validLines = reader.ReadToEnd ().Split ('\n').Where (l => !l.StartsWith ("//")).ToArray ();
                foreach (string line in validLines) {
                    if (line == "\r")
                        continue;

                    string [] parameters = line.Split ('=');
                    _cache.Add (parameters [0].Trim (), parameters [1].Trim ());
                }
            }
        }

        public static string GetString(string paramName) {
            return _cache.ContainsKey(paramName) ? (string)_cache [paramName] : string.Empty;
        }
        public static int GetInt(string paramName) {
            return _cache.ContainsKey (paramName) ? Convert.ToInt32 (_cache [paramName]) : 0;
        }

    }
}
