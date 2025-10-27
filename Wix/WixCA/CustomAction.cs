using WixToolset.Dtf;
using System;
using System.IO;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System.Runtime.Serialization;
using Serilog.Debugging;
using Serilog.Core;
using Serilog.Configuration;
using Serilog.Data;
using Serilog.Filters;
using Serilog.Context;
using Serilog.Events;
using WixToolset.Dtf.WindowsInstaller;


namespace WixCA
{
    public class CustomActions : Attribute
    {
        [CustomAction]
        public static ActionResult CopyCustomisationFiles(Session session)
        {
            Serilog.Log.Debug("Begin CopyCustomisationFiles");

            string path = session.CustomActionData["SourceDir"];

            Serilog.Log.Debug("source dir is " + path);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\iSpy\";
            path = path.Trim().Trim('\\') + @"\";
            try
            {
                if (System.IO.File.Exists(path + "custom.txt"))
                {
                    TryCopy(path + @"custom.txt", appDataPath + @"custom.txt", true);
                    TryCopy(path + @"logo.jpg", appDataPath + @"logo.jpg", true);
                    TryCopy(path + @"logo.png", appDataPath + @"logo.png", true);
                }
            }
            catch
            {
            }

            return ActionResult.Success;
        }

        private static void TryCopy(string source, string target, bool overwrite)
        {
            try
            {
                System.IO.File.Copy(source, target, overwrite);
            }
            catch
            {
            }
        }

        private class CustomActionAttribute : Attribute
        {
        }
    }
}