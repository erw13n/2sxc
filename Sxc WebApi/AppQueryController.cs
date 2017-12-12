﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using ToSic.Eav.Logging.Simple;
using ToSic.SexyContent.Security;
using ToSic.SexyContent.Serializers;

namespace ToSic.SexyContent.WebApi
{
    /// <summary>
    /// In charge of delivering Pipeline-Queries on the fly
    /// They will only be delivered if the security is confirmed - it must be publicly available
    /// </summary>
    [AllowAnonymous]
    public class AppQueryController : SxcApiController
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext); // very important!!!
            Log.Rename("2sApQr");
        }

        [HttpGet]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Anonymous)]   // will check security internally, so assume no requirements
        public Dictionary<string, IEnumerable<Dictionary<string, object>>> Query([FromUri] string name, [FromUri] bool includeGuid = false, [FromUri] string stream = null) 
            => BuildQueryAndRun(App, name, stream, includeGuid, Dnn.Module, Log);


        [HttpGet]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Anonymous)]   // will check security internally, so assume no requirements
        public Dictionary<string, IEnumerable<Dictionary<string, object>>> PublicQuery([FromUri] string appPath, [FromUri] string name, [FromUri] string stream = null)
        {
            Log.Add($"public query path:{appPath}, name:{name}");
            var queryApp = new App(PortalSettings, GetCurrentAppIdFromPath(appPath));

            // ensure the queries can be executed (needs configuration provider, usually given in SxcInstance, but we don't hav that here)
            var config = DataSources.ConfigurationProvider.GetConfigProviderForModule(0, queryApp, null);
            queryApp.InitData(false, false, config);

            // now just run the default query check and serializer
            return BuildQueryAndRun(queryApp, name, stream, false, null, Log);
        }


        private static Dictionary<string, IEnumerable<Dictionary<string, object>>> BuildQueryAndRun(App app, string name, string stream, bool includeGuid, ModuleInfo module, Log log)
        {
            log.Add($"build and run query name:{name}, with module:{module?.ModuleID}");
            var query = app.GetQuery(name);

            if (query == null)
                throw HttpErr(HttpStatusCode.NotFound, "query not found", $"query '{name}' not found");

            var permissionChecker = new PermissionController(query.QueryDefinition, log, module);
            var readExplicitlyAllowed = permissionChecker.UserMay(PermissionGrant.Read);

            var isAdmin = module != null && DotNetNuke.Security.Permissions
                              .ModulePermissionController.CanAdminModule(module);

            // Only return query if permissions ok
            if (!(readExplicitlyAllowed || isAdmin))
                throw HttpErr(HttpStatusCode.Unauthorized, "Request not allowed", $"Request not allowed. User does not have read permissions for query '{name}'");

            var serializer = new Serializer { IncludeGuid = includeGuid };
            return serializer.Prepare(query, stream?.Split(','));
        }

        private static HttpResponseException HttpErr(HttpStatusCode status, string title, string msg)
        {
            return new HttpResponseException(new HttpResponseMessage(status)
            {
                Content = new StringContent(msg),
                ReasonPhrase = title
            });
        }
    }
}