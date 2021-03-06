﻿using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
namespace CemsMobilePcrSrv
{
    public class ConfigCls
    {

        System.Configuration.Configuration config;
        string strFileName;
        string strTempSaveFilePath;
        public ConfigurationSectionCollection Sections
        {
            get { return config.Sections; }
        }
        public ConfigurationSectionGroupCollection SectionGroups
        {
            get { return config.SectionGroups; }
        }
        public ConfigCls(string filename = "")
        {
            try
            {
                strTempSaveFilePath = System.IO.Path.GetTempPath() + filename;
                if (!string.IsNullOrEmpty(filename))
                {
                    ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                    if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "\\" + filename))
                    {
                        File.Copy(Path.GetDirectoryName(Application.ExecutablePath) + "\\" + filename, strTempSaveFilePath, true);
                    }
                    fileMap.ExeConfigFilename = strTempSaveFilePath;
                    config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                }
                else
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                }
                this.strFileName = filename;
                if (config == null)
                {
                    //MessageBox.Show("ReadProfile", "Error Reading Configuration File " + filename);
                    //ReadProfile = False
                    return;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }
        public string FileName
        {
            get { return strFileName; }
            set { strFileName = value; }
        }

        public void UpdateAppSettings(string SectionName, string key, string value)
        {
            Logger.LogActivity("UpdateAppSettings Breakpoint 1", "Breakpoints");
            // Get the configuration file.
            //config.AppSettings.File = Path.GetDirectoryName(Application.ExecutablePath) & "\app.config"
            // Add an entry to appSettings.
            //Dim appStgCnt As Integer = ConfigurationManager.AppSettings.Count
            try { 
            System.Configuration.AppSettingsSection section = default(System.Configuration.AppSettingsSection);
            if (!string.IsNullOrEmpty(SectionName))
            {
                if (config.Sections[SectionName] == null)
                {
                    section = new System.Configuration.AppSettingsSection();
                    section.Settings.Add(key, value);
                    config.Sections.Add(SectionName, section);
                }
                else
                {
                    try
                    {
                        section = (AppSettingsSection)config.Sections[SectionName];
                    }
                    catch
                    {
                        section = new System.Configuration.AppSettingsSection();
                        config.Sections.Add(SectionName, section);
                    }

                    if (section.Settings[key] == null)
                    {
                        section.Settings.Add(key, value);
                    }
                    else
                    {
                        section.Settings[key].Value = value;
                    }
                }
            }
            else
            {
                if (config.AppSettings.Settings[key] == null)
                {
                    config.AppSettings.Settings.Add(key, value);
                }
                else
                {
                    config.AppSettings.Settings[key].Value = value;
                }
            }
            // Save the configuration file.
            //config.Save(ConfigurationSaveMode.Modified)
            SaveConfigFile();

            // Force a reload of the changed section.
            if (!string.IsNullOrEmpty(SectionName))
            {
                ConfigurationManager.RefreshSection(SectionName);
            }
            else
            {
                ConfigurationManager.RefreshSection("appSettings");
            }
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }
        public string GetProfileEntry(string SectionName, string Key, string DefaultValue = "")
        {
            Logger.LogActivity("GetProfileEntry Breakpoint " + SectionName + " " + Key, "Breakpoints");
            string Entry = null;
            try
            {
            System.Configuration.AppSettingsSection section = (AppSettingsSection)config.Sections[SectionName];
            if (!string.IsNullOrEmpty(SectionName))
            {
                ConfigurationManager.RefreshSection(SectionName);
            }
            else
            {
                ConfigurationManager.RefreshSection("appSettings");
            }
            if (!string.IsNullOrEmpty(SectionName))
            {
                if ((section != null))
                {
                    if ((section.Settings[Key] != null))
                    {
                        Entry = section.Settings[Key].Value;
                    }
                }
            }
            else
            {
                Entry = config.AppSettings.Settings[Key].Value;
            }
            if (Entry == null | section == null)
            {
                UpdateAppSettings(SectionName, Key, DefaultValue);
                Entry = DefaultValue;
            }
             }
            catch (Exception ex) { Logger.LogException(ex); }
            return Entry;
        }

        private void SaveConfigFile()
        {
            // ERROR: Not supported in C#: OnErrorStatement
            try
            {
                //Dim TempSaveFilePath As String = 
                Logger.LogActivity("SaveConfigFile Breakpoint 1", "Breakpoints");
                string AppFilePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + strFileName;
                Logger.LogActivity("SaveConfigFile Breakpoint 2, " + strTempSaveFilePath, "Breakpoints");
                Logger.LogActivity("SaveConfigFile Breakpoint 3, " + AppFilePath, "Breakpoints");
                string PrevPath = config.FilePath;
                //config.SaveAs(TempSaveFilePath)
                //File.Copy(AppFilePath, strTempSaveFilePath, True)
                config.Save(ConfigurationSaveMode.Modified);
                File.Copy(strTempSaveFilePath, AppFilePath, true);
            }
            catch (Exception ex) { Logger.LogException(ex); }
            //File.Delete(strTempSaveFilePath)
        }

        //=======================================================
        //Service provided by Telerik (www.telerik.com)
        //Conversion powered by NRefactory.
        //Twitter: @telerik
        //Facebook: facebook.com/telerik
        //=======================================================

    }

}