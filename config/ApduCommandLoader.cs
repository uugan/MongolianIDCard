using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonSign.config
{
    class ApduCommandLoader
    {
        public static List<ApduCommand> LoadFromJson(string filePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(filePath))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("APDU configuration resource not found.");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var commands = JsonConvert.DeserializeObject<Dictionary<string, List<ApduCommand>>>(json);
                    return commands["commands"];
                }
            }
        }

    }
}
