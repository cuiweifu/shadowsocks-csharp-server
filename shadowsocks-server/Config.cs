using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SimpleJson;

namespace shadowsocks
{
    [Serializable]
    public class Config
    {
        public string server;
        public int server_port;
        public string password;
        public string method;
        public bool multiuser_pylisten;

        private static void assert(bool condition)
        {
            if(!condition) 
            {
                throw new Exception("assertion failure");
            }
        }

        public Config Copy()
        {
            return new Config
            {
                server = this.server,
                server_port = this.server_port,
                password = this.password,
                method = this.method,
                multiuser_pylisten = this.multiuser_pylisten
            };
        }

        public static Config Load()
        {
            try
            {
                using (StreamReader sr = new StreamReader(File.OpenRead(@"config.json")))
                {
                    Config config = SimpleJson.SimpleJson.DeserializeObject<Config>(sr.ReadToEnd());
                    assert(!string.IsNullOrEmpty(config.server));
                    assert(!string.IsNullOrEmpty(config.password));
                    assert(config.server_port > 0);
                    return config;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new Config
                {
                    server = "127.0.0.1",
                    server_port = 8388,
                    password = "barfoo!",
                    multiuser_pylisten = false
                };
            }
        }

        public static void Save(Config config)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(@"config.json", FileMode.Create)))
                {
                    string jsonString = SimpleJson.SimpleJson.SerializeObject(new
                    {
                        server = config.server,
                        server_port = config.server_port,
                        password = config.password,
                        method = config.method,
                        multiuser_pylisten = config.multiuser_pylisten
                    });
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
