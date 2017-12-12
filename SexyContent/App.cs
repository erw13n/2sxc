﻿using System.Threading;
using System.Web;
using DotNetNuke.Entities.Portals;
using ToSic.Eav.AppEngine;
using ToSic.Eav.Logging.Simple;
using ToSic.SexyContent.Environment.Dnn7;

namespace ToSic.SexyContent
{
    /// <summary>
    /// The app class gives access to the App-object - for the data and things like the App:Path placeholder in a template
    /// </summary>
    public class App : Eav.Apps.App<PortalSettings>, Interfaces.IApp
    {
        #region Dynamic Properties: Configuration, Settings, Resources
        public dynamic Configuration
        {
            get
            {
                if(!_configLoaded && AppMetadata != null)
                    _config = new DynamicEntity(AppMetadata, new[] {Thread.CurrentThread.CurrentCulture.Name}, null);
                _configLoaded = true;
                return _config;
            }
        }
        private bool _configLoaded;
        private dynamic _config;

        public dynamic Settings
        {
            get
            {
                if(!_settingsLoaded && AppSettings != null)
                    _settings = new DynamicEntity(AppSettings, new[] {Thread.CurrentThread.CurrentCulture.Name}, null);
                _settingsLoaded = true;
                return _settings;
            }
        }
        private bool _settingsLoaded;
        private dynamic _settings;

        public dynamic Resources
        {
            get
            {
                if(!_resLoaded && AppResources!= null)
                    _res = new DynamicEntity(AppResources, new[] {Thread.CurrentThread.CurrentCulture.Name}, null);
                _resLoaded = true;
                return _res;
            }
        }
        private bool _resLoaded;
        private dynamic _res;

        #endregion

        #region App-Level TemplateManager, ContentGroupManager, EavContext --> must move to EAV some time
        private TemplateManager _templateManager;
        public TemplateManager TemplateManager => _templateManager 
            ?? (_templateManager = new TemplateManager(ZoneId, AppId, Log));

        private ContentGroupManager _contentGroupManager;
        public ContentGroupManager ContentGroupManager => _contentGroupManager 
            ?? (_contentGroupManager = new ContentGroupManager(ZoneId, AppId, ShowDraftsInData, VersioningEnabled, Log));

        #endregion

        public App(PortalSettings ownerPortalSettings, int appId, Log parentLog = null) 
            : base(new Environment.DnnEnvironment(parentLog), AutoLookup, appId, new DnnTennant(ownerPortalSettings), true, parentLog) { }

        public App(int zoneId, int appId, PortalSettings portalSettings, bool allowSideEffects = true, Log parentLog = null)
            : base(new Environment.DnnEnvironment(parentLog), zoneId, appId, new DnnTennant(portalSettings), allowSideEffects, parentLog) { }

        

        #region Paths
        public string Path => VirtualPathUtility.ToAbsolute(GetRootPath());
        public string Thumbnail => System.IO.File.Exists(PhysicalPath + IconFile) ? Path + IconFile : null;

        #endregion

        
    }
}