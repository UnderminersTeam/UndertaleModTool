using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UndertaleModLib.Scripting
{
    public class ScriptConfiguration
    {

        public ScriptConfiguration(string FileName)
        {
            ConfigurationFile = FileName;
        }

        private string _configuration_file;

        public string ConfigurationFile
        {
            get
            {
                return _configuration_file;
            }

            set
            {
                if (!System.IO.File.Exists(value))
                    System.IO.File.WriteAllText(value, "");

                _configuration_file = value;
            }
        }

        private Dictionary<string, string> GetConfigurationData()
        {
            var configuration_file_text = System.IO.File.ReadAllText(ConfigurationFile);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(configuration_file_text);
        }

        public string this[string key]
        {
            get
            {
                GetConfigurationData().TryGetValue(key, out var output);
                return output;
            }

            set
            {
                var configuration_data = GetConfigurationData();
                configuration_data[key] = value;
                System.IO.File.WriteAllText(ConfigurationFile, JsonConvert.SerializeObject(configuration_data));
            }
        }
    }
}
