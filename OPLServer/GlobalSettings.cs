using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace OPLServer
{
    internal class GlobalSettings
    {
        private static GlobalSettings instance;
        private bool isLoadingSettings = false;
        private static readonly Dictionary<string, string> settings = new Dictionary<string, string>();

        private GlobalSettings()
        {
            settings.Clear();
        }

        public static GlobalSettings Instance
        {
            get
            { 
                if (instance == null)
                    instance = new GlobalSettings();

                return instance;
            }
        }

        public bool IsLoadingSettings
        {
            get { return isLoadingSettings; }
        }

        public string getSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "";
                return result;
            }
            catch (ConfigurationErrorsException) { }
            return "";
        }

        public void setSetting(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException) { }
        }
    }
}
