﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;
using Newtonsoft.Json;
using PropertyChanged;
using Serilog;

namespace ME3TweaksCore.Services.ThirdPartyModIdentification
{
    [AddINotifyPropertyChangedInterface]
    public class TPMIService
    {
        public static bool ServiceLoaded { get; set; }

        private const string ThirdPartyIdentificationServiceURL = @"https://me3tweaks.com/modmanager/services/thirdpartyidentificationservice?highprioritysupport=true&allgames=true";


        /// <summary>
        /// Accesses the third party identification server. Key is the game enum as a string, results are dictionary of DLCName => Info.
        /// </summary>
        internal static Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>> Database;

        #region ONLINE FETCH AND LOAD
        public static Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>> FetchThirdPartyIdentificationManifest(bool overrideThrottling = false)
        {
            string cached = null;
            if (File.Exists(MCoreFilesystem.GetThirdPartyIdentificationCachedFile()))
            {
                try
                {
                    cached = File.ReadAllText(MCoreFilesystem.GetThirdPartyIdentificationCachedFile());
                }
                catch (Exception e)
                {
                    var relevantInfo = new Dictionary<string, string>()
                    {
                        {@"Error type", @"Error reading cached online content"},
                        {@"Service", @"Third Party Identification Service"},
                        {@"Message", e.Message}
                    };
                    TelemetryInterposer.UploadErrorLog(e, relevantInfo);
                }
            }


            if (!File.Exists(MCoreFilesystem.GetThirdPartyIdentificationCachedFile()) || overrideThrottling || MOnlineContent.CanFetchContentThrottleCheck())
            {
                try
                {
                    using var wc = new ShortTimeoutWebClient();

                    string json = wc.DownloadStringAwareOfEncoding(ThirdPartyIdentificationServiceURL);
                    File.WriteAllText(MCoreFilesystem.GetThirdPartyIdentificationCachedFile(), json);
                    return JsonConvert.DeserializeObject<Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>>>(json);
                }
                catch (Exception e)
                {
                    //Unable to fetch latest help.
                    Log.Error(@"Error fetching online third party identification service: " + e.Message);

                    if (cached != null)
                    {
                        Log.Warning(@"Using cached third party identification service  file instead");
                    }
                    else
                    {
                        Log.Error(@"Unable to load third party identification service and local file doesn't exist. Returning a blank copy.");
                        return getBlankTPIS();
                    }
                }
            }

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>>>(cached);
            }
            catch (Exception e)
            {
                Log.Error(@"Could not parse cached third party identification service file. Returning blank TPMI data instead. Reason: " + e.Message);
                return getBlankTPIS();
            }
        }

        private static Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>> getBlankTPIS()
        {
            return new Dictionary<string, CaseInsensitiveDictionary<ThirdPartyModInfo>>
            {
                [@"ME1"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>(),
                [@"ME2"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>(),
                [@"ME3"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>(),
                [@"LE1"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>(),
                [@"LE2"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>(),
                [@"LE3"] = new CaseInsensitiveDictionary<ThirdPartyModInfo>()
            };
        }
        #endregion


        /// <summary>
        /// Looks up information about a DLC mod through the third party identification service
        /// </summary>
        /// <param name="dlcName"></param>
        /// <param name="game">Game to look in database for</param>
        /// <returns>Third party mod info about dlc folder, null if not found</returns>
        public static ThirdPartyModInfo GetThirdPartyModInfo(string dlcName, MEGame game)
        {
            if (!ServiceLoaded) return null; //Not loaded
            if (Database.TryGetValue(game.ToString(), out var infosForGame))
            {
                if (infosForGame.TryGetValue(dlcName, out var info))
                {
                    return info;
                }
            }

            return null;
        }

        internal static List<ThirdPartyModInfo> GetThirdPartyModInfosByModuleNumber(int modDLCModuleNumber, MEGame game)
        {
            if (ServiceLoaded && Database.TryGetValue(game.ToString(), out var infosForGame))
            {
                var me2Values = Database[game.ToString()];
                return me2Values.Where(x => x.Value.modulenumber == modDLCModuleNumber.ToString()).Select(x => x.Value).ToList();
            }

            return new List<ThirdPartyModInfo>(); //Not loaded
        }

        internal static List<ThirdPartyModInfo> GetThirdPartyModInfosByMountPriority(MEGame game, int modMountPriority)
        {
            if (ServiceLoaded && Database.TryGetValue(game.ToString(), out var infosForGame))
            {
                var gameValues = Database[game.ToString()];
                return gameValues.Where(x => x.Value.MountPriorityInt == modMountPriority).Select(x => x.Value).ToList();
            }

            return new List<ThirdPartyModInfo>(); //Not loaded
        }

        public static bool TryGetModInfo(MEGame game, string dlcFolderName, out ThirdPartyModInfo tpmi)
        {
            tpmi = GetThirdPartyModInfo(dlcFolderName, game);
            return tpmi != null;
        }
    }
}