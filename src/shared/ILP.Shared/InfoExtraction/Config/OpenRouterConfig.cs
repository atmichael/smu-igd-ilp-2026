using Microsoft.Extensions.Configuration;
using System;

namespace ILP.Server.Config
{
    public static class OpenRouterConfig
    {
        public static string m_key = string.Empty;
        public static string m_model = string.Empty;

        public static string Key { get { return m_key; } }
        public static string ModelName { get { return m_model; } }

        public static void Initialize(IConfiguration config)
        {
            InitOpenRouterKey(config);
            m_model = config["AppSettings:OpenRouter:ModelName"];
            if (string.IsNullOrEmpty(m_model))
            {
                m_model = "google/gemini-2.5-flash";
            }
        }

        private static void InitOpenRouterKey(IConfiguration config)
        {
            var configPath = config["AppSettings:OpenRouter:KeyPath"];
            if (string.IsNullOrWhiteSpace(configPath) ||
                !File.Exists(configPath))
            {
                throw new ApplicationException("Missing mandatory: OpenRouter API Key");
            }
            var content = File.ReadAllText(configPath);
            m_key = content == null ? "" : content.Trim();
        }
    }
}
