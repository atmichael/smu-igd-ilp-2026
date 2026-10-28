using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ILP.AppHost.Config
{
    public static class AspireConfig
    {
        private static string m_frontendPath = "";

        public static string FrontendPath { get { return m_frontendPath; } }

        public static void Initialize(IConfiguration config)
        {
            m_frontendPath = config["AspireConfig:FrontendPath"];
        }
    }
}
