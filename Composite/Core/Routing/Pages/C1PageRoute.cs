﻿using System;
using System.Globalization;
using System.Web;
using System.Web.Routing;
using Composite.Core.WebClient;
using Composite.Core.Configuration;
using Composite.Core.Extensions;

namespace Composite.Core.Routing.Pages
{
    /// <summary>
    /// Implements C1 page route for ASP.NET routing
    /// </summary>
    public class C1PageRoute : RouteBase
    {
        /// <exclude />
        public static readonly string RouteDate_PageUrl = "C1Page";

        internal static readonly string HttpContextItem_C1PageUrl = "C1_PageUrl";
        private static readonly string HttpContextItem_PathInfoHandled = "C1PageRoute_PathInfoHandled";

        /// <exclude />
        public static PageUrlData PageUrlData
        {
            get
            {
                var httpContext = HttpContext.Current;
                return httpContext != null ? httpContext.Items[HttpContextItem_C1PageUrl] as PageUrlData : null;
            }
            set
            {
                var httpContext = HttpContext.Current;
                Verify.IsNotNull(httpContext, "HttpContext is not available");

                httpContext.Items[HttpContextItem_C1PageUrl] = value;
            }
        }

        /// <summary>
        /// Get the additional information that was passed in URL along with page url
        /// </summary>
        /// <returns>The PathInfo url part.</returns>
        public static string GetPathInfo()
        {
            var urlData = PageUrlData;
            return urlData != null ? urlData.PathInfo : null;
        }

        /// <summary>
        /// This method has to be called to notify the system that PathInfo was used, and the request will not be redirected to "Page not found" page
        /// </summary>
        public static void RegisterPathInfoUsage()
        {
            var httpContext = HttpContext.Current;

            if(!httpContext.Items.Contains(HttpContextItem_PathInfoHandled))
            {
                httpContext.Items.Add(HttpContextItem_PathInfoHandled, true);
            }
        }

        /// <exclude/>
        [Obsolete("User PathInfoUsed property instead")]
        public static bool PathInfoHasBeenUsed()
        {
            return PathInfoUsed;
        }

        /// <summary>
        /// Gets a value indicating whether path info part of C1 page url has been used.
        /// </summary>
        /// <value>
        ///   <c>true</c> if path info has been used; otherwise, <c>false</c>.
        /// </value>
        public static bool PathInfoUsed
        {
            get
            {
                var httpContext = HttpContext.Current;

                return httpContext != null && httpContext.Items.Contains(HttpContextItem_PathInfoHandled);
            }
        }



        /// <exclude />
        public override RouteData GetRouteData(HttpContextBase context)
        {
            if (!SystemSetupFacade.IsSystemFirstTimeInitialized)
            {
                return null;
            }

            string localPath = context.Request.Url.LocalPath;

            var urlProvider = PageUrls.UrlProvider;

            if (IsAdminPath(localPath) || IsRenderersPath(localPath))
            {
                return null;
            }

            string currentUrl = context.Request.Url.OriginalString;

            UrlKind urlKind;

            PageUrlData pageUrlData = urlProvider.ParseUrl(currentUrl, out urlKind);
            if (pageUrlData == null || urlKind == UrlKind.Renderer)
            {
                return null;
            }

            var urlSpace = new UrlSpace(context);

            // Redirecting friendly urls to public urls
            if (urlKind == UrlKind.Friendly || urlKind == UrlKind.Redirect || urlKind == UrlKind.Internal)
            {
                if(pageUrlData.PathInfo == "/")
                {
                    pageUrlData.PathInfo = null;
                }

                string publicUrl = urlProvider.BuildUrl(pageUrlData, UrlKind.Public, urlSpace);
                if(publicUrl == null)
                {
                    if (urlKind != UrlKind.Internal)
                    {
                        return null;
                    }
                    // Rendering internal url if public url is missing
                }
                else
                {
                    return SeoFriendlyRedirect(context, publicUrl);
                }
            }

            Verify.That(urlKind == UrlKind.Public || urlKind == UrlKind.Internal, "Unexpected url kind '{0}", urlKind);

            bool isPublicUrl = urlKind == UrlKind.Public;


            if (isPublicUrl)
            {
                // If url ends with a trailing slash - doing a redirect. F.e. http://localhost/a/ -> http://localhost/a
                if (pageUrlData.PathInfo == "/")
                {
                    pageUrlData.PathInfo = null;
                    return SeoFriendlyRedirect(context, urlProvider.BuildUrl(pageUrlData, UrlKind.Public, urlSpace));
                }

                // Checking casing in url, so the same page will appear as a few pages by a crawler
                string correctUrl = urlProvider.BuildUrl(pageUrlData, UrlKind.Public, urlSpace);

                string originalFilePath = new UrlBuilder(currentUrl).RelativeFilePath;
                string correctFilePath = new UrlBuilder(correctUrl).RelativeFilePath;

                if (string.Compare(originalFilePath, correctFilePath, false, CultureInfo.InvariantCulture) != 0 &&
                    string.Compare(originalFilePath, correctFilePath, true, CultureInfo.InvariantCulture) == 0)
                {
                    // redirect to a url with right casing
                    return SeoFriendlyRedirect(context, correctUrl);
                }
            }

            // Disabling ASP.NET cache if there's a logged-in user
            if (Composite.C1Console.Security.UserValidationFacade.IsLoggedIn())
            {
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            Verify.IsFalse(context.Items.Contains(HttpContextItem_C1PageUrl), "C1PageRoute was executed twice during the same request. If could be f.e. because there's an MVC player that have a route that matches a C1 page url.");
            PageUrlData = pageUrlData;

            var data = GetRouteData();
            data.Values.Add(RouteDate_PageUrl, pageUrlData);

            // Doing url rewriting so ASP.NET will get correct FilePath/PathInfo properties
            if(!pageUrlData.PathInfo.IsNullOrEmpty())
            {
                string filePath = localPath.Substring(0, localPath.Length - pageUrlData.PathInfo.Length);

                string queryString = context.Request.Url.Query;
                if(queryString.StartsWith("?"))
                {
                    queryString = queryString.Substring(1);
                }
                context.RewritePath(filePath, pageUrlData.PathInfo, queryString);
            }

            return data;
        }

        private RouteData GetRouteData()
        {
            return new RouteData(this, new C1PageRouteHandler());
        }

        /// <exclude />
        public void Dispose()
        {
        }

        private static bool IsAdminPath(string relativeUrl)
        {
            return string.Compare(relativeUrl, UrlUtils.AdminRootPath, true) == 0
                   || relativeUrl.StartsWith(UrlUtils.AdminRootPath + "/", true);
        }

        private static bool IsRenderersPath(string relativeUrl)
        {
            return relativeUrl.StartsWith(UrlUtils.RenderersRootPath + "/", true);
        }

        private RouteData SeoFriendlyRedirect(HttpContextBase context, string url)
        {
            context.Response.AddHeader("Location", url);
            context.Response.StatusCode = 301; // Http 301 - "Permanently moved"
            context.ApplicationInstance.CompleteRequest();

            return GetRouteData(); // returning route so other routers will not be executed
        }

        /// <exclude />
        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}
