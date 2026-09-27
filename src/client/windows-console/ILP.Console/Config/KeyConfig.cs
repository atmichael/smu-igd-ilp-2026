using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace ILP.Console.Config
{
    internal static class KeyConfig
    {
        private static string m_openRouterApiKey;


        static KeyConfig()
        {
            InitializeOpenRouterAPIKey();
        }

        public static string OpenRouterApiKey
        {
            get { return m_openRouterApiKey; }
        }

        private static void InitializeOpenRouterAPIKey()
        {
            var configPath = ConfigurationManager.AppSettings["openrouter_key_path"];
            if (string.IsNullOrWhiteSpace(configPath) ||
                !File.Exists(configPath))
            {
                throw new ApplicationException("Missing mandatory: OpenRouter API Key");
            }

            using (var file = File.OpenRead(configPath))
            using (var reader = new StreamReader(file))
            {
                var content = reader.ReadLine();
                m_openRouterApiKey = content.Trim();
            }
        }

    }
}
