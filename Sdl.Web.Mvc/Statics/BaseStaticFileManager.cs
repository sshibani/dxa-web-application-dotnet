﻿using Sdl.Web.Mvc.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace Sdl.Web.Mvc
{
    /// <summary>
    /// Class to manage static assets such as config and resources. This contains generic methods to serialize assets
    /// based on a root boostrap json file, which recursively loads in more assets. 
    /// Implementations of this class are responsible for implementing the Serialize method
    /// in order to read the static asset from somewhere (eg Broker DB) and serialize it to the file system
    /// </summary>
    public abstract class BaseStaticFileManager : IStaticFileManager
    {
        public virtual string CreateStaticAssets(string applicationRoot)
        {
            DateTime timer = DateTime.Now;
            List<string> folders = new List<string>();
            string version = null;
            try
            {
                foreach (var loc in Configuration.Localizations.Values)
                {
                    var localizationRoot = String.Format("{0}{1}/{2}", applicationRoot, loc.Path, Configuration.SystemFolder);
                    if (!folders.Contains(localizationRoot))
                    {
                        try
                        {
                            //The other config etc files are bootstrapped from /system/_all.json
                            var url = String.Format("{0}/{1}/_all.json", loc.Path == "" || loc.Path.StartsWith("/") ? loc.Path : "/" + loc.Path, Configuration.SystemFolder);
                            SerializeFile(url, 2);
                            folders.Add(localizationRoot);
                            //The HTML Design version is published in /version.json
                            var versionUrl = String.Format("{0}/version.json", loc.Path == "" || loc.Path.StartsWith("/") ? loc.Path : "/" + loc.Path);
                            var versionJson = Serialize(versionUrl, true);
                            if (versionJson != null)
                            {
                                version = Json.Decode(versionJson).version;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error serializing localization {0}", localizationRoot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating files on disk.");
            }
            Log.Trace(timer, "serialize-config", applicationRoot);
            return version;
        }

        public abstract string Serialize(string url, bool returnContents = false);

        /// <summary>
        /// Recursively serialize a file (which may contain just a list of further files to recursively process)
        /// </summary>
        /// <param name="url">The url of the file</param>
        /// <param name="bootstrapLevel">Level of recursion expected 0=none (the file contains data to serialize rather than a list of other files)</param>
        protected virtual void SerializeFile(string url, int bootstrapLevel = 0)
        {
            string fileContents = this.Serialize(url, bootstrapLevel != 0);
            if (bootstrapLevel != 0)
            {
                var bootstrapJson = Json.Decode(fileContents);
                foreach (string file in bootstrapJson.files)
                {
                    try
                    {
                        SerializeFile(file, bootstrapLevel - 1);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error serializing file {0}", file);
                    }
                }
            }
        }
    }
}
