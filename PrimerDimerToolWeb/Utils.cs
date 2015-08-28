using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RDotNet;

namespace PrimerDimerToolWeb
{
    
    public class Utils
    {
        public static REngine engine = null;
        public static REngine getREngineInstance()
        {
            if (engine == null)
            {
                string primer3Home = System.Web.Configuration.WebConfigurationManager.AppSettings["primer3Home"];
                string RHome = System.Web.Configuration.WebConfigurationManager.AppSettings["RHome"];
                string javaHome = System.Web.Configuration.WebConfigurationManager.AppSettings["javaHome"];
                Environment.SetEnvironmentVariable("PATH", primer3Home);
                Environment.SetEnvironmentVariable("JAVA_HOME", javaHome);
                REngine.SetEnvironmentVariables(RHome + "/bin/x64", RHome);
                engine = REngine.GetInstance(null, true, null, null);
            }
            return engine;
        }

    }
}