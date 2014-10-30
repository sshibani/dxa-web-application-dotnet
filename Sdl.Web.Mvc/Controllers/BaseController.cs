﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.ContentProvider;
using Sdl.Web.Mvc.Resources;
using Sdl.Web.Mvc.Utils;
using Sdl.Web.Mvc.Formats;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Base controller containing main controller actions (Page, PageRaw, Region, Entity, Navigation, List etc.)
    /// </summary>
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }
        public virtual ModelType ModelType { get; set; }

        public BaseController()
        {
            //default model type
            ModelType = ModelType.Entity;
        }

        /// <summary>
        /// Given a page URL, load the corresponding Page Model, Map it to the View Model and render it. 
        /// Can return XML or JSON if specifically requested on the URL query string (e.g. ?format=xml). 
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>Rendered Page View Model</returns>
        [FormatData]
        public virtual ActionResult Page(string pageUrl)
        {
            ModelType = ModelType.Page;
            var page = ContentProvider.GetPageModel(pageUrl);
            if (page == null)
            {
                return NotFound();
            }

            var viewData = GetViewData(page);
            SetupViewData(0, viewData);
            var model = ProcessModel(page) ?? page;
            if (model is WebPage)
            {
                WebRequestContext.PageId = ((WebPage)model).Id;
            }
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Given a page URL, load the corresponding raw content and write it to the response
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>raw page content</returns>
        public virtual ActionResult PageRaw(string pageUrl = null)
        {
            pageUrl = pageUrl ?? Request.Url.AbsolutePath;
            var rawContent = ContentProvider.GetPageContent(pageUrl);
            if (rawContent == null)
            {
                return NotFound();
            }
            return GetRawActionResult(Path.GetExtension(pageUrl).Substring(1), rawContent);
        }

        /// <summary>
        /// Render a file not found page
        /// </summary>
        /// <returns>404 page or HttpException if there is none</returns>
        [FormatData]
        public virtual ActionResult NotFound()
        {
            var page = ContentProvider.GetPageModel(WebRequestContext.Localization.Path + "/error-404");
            if (page == null)
            {
                throw new HttpException(404, "Page Not Found");
            }
            var viewData = GetViewData(page);
            SetupViewData(0, viewData);
            var model = ProcessModel(page) ?? page;
            Response.StatusCode = 404;
            return View(viewData.ViewName, model);
        }
        
        /// <summary>
        /// Map and render a region model
        /// </summary>
        /// <param name="region">The region model</param>
        /// <param name="containerSize">The size (in grid units) of the container the region is in</param>
        /// <returns>Rendered region model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Region(IRegion region, int containerSize = 0)
        {
            ModelType = ModelType.Region;
            SetupViewData(containerSize, region.AppData);
            var model = ProcessModel(region) ?? region;
            return View(region.AppData.ViewName, model);
        }

        /// <summary>
        /// Map and render an entity model
        /// </summary>
        /// <param name="entity">The entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Entity(IEntity entity, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            SetupViewData(containerSize, entity.AppData);
            var model = ProcessModel(entity) ?? entity;
            return View(entity.AppData.ViewName, model);
        }

       

        /// <summary>
        /// Populate and render a navigation entity model
        /// </summary>
        /// <param name="entity">The navigation entity</param>
        /// <param name="navType">The type of navigation to render</param>
        /// <param name="containerSize">The size (in grid units) of the container the navigation element is in</param>
        /// <returns></returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Navigation(object entity, string navType, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            var model = ProcessNavigation(entity, navType) ?? entity;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Populate and render an XML site map
        /// </summary>
        /// <param name="entity">The sitemap entity</param>
        /// <returns>Rendered XML sitemap</returns>
        public virtual ActionResult SiteMap(object entity=null)
        {
            var model = ContentProvider.GetNavigationModel(SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization));
            var viewData = GetViewData(entity);
            if (viewData.ViewName != null)
            {
                SetupViewData(0, viewData);
                return View(viewData.ViewName, model);
            }
            return View("SiteMapXml", model);
        }

        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The item id to resolve</param>
        /// <param name="localizationId">The site localization in which to resolve the URL</param>
        /// <param name="defaultItemId"></param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, string localizationId, string defaultItemId = null, string defaultPath = null)
        {
            var url = ContentProvider.ContentResolver.ResolveLink("tcm:" + itemId, localizationId);
            if (url == null && defaultItemId!=null)
            {
                url = ContentProvider.ContentResolver.ResolveLink("tcm:" + defaultItemId, localizationId);
            }
            if (url == null)
            {
                url = String.IsNullOrEmpty(defaultPath) ? "/" : defaultPath;
            }
            return Redirect(url);
        }

        /// <summary>
        /// This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        /// </summary>
        /// <param name="sourceModel"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object ProcessModel(object model)
        {
            //Check if an exception was generated when creating the model, so now is the time to throw it
            if (model is ExceptionEntity)
            {
                throw ((ExceptionEntity)model).Exception;
            }
            return model;
        }
        
        /// <summary>
        /// This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        /// </summary>
        /// <param name="sourceModel"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object ProcessModel(object sourceModel, Type type)
        {
            return ContentProvider.MapModel(sourceModel, ModelType, type);
        }

        protected virtual object ProcessNavigation(object sourceModel, string navType)
        {
            var navigationUrl = SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization);
            var model = ProcessModel(sourceModel);
            var nav = model as NavigationLinks;
            NavigationLinks links = new NavigationLinks();
            switch (navType)
            {
                case "Top":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildTopNavigation(Request.Url.LocalPath);
                    break;
                case "Left":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildContextNavigation(Request.Url.LocalPath);
                    break;
                case "Breadcrumb":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildBreadcrumb(Request.Url.LocalPath);
                    break;
            }
            if (nav != null)
            {
                links.EntityData = nav.EntityData;
                links.PropertyData = nav.PropertyData;
            }
            return links;
        }
        
        protected virtual ActionResult GetRawActionResult(string type, string rawContent)
        {
            string contentType;
            switch (type)
            {
                case "json":
                    contentType = "application/json";
                    break;
                case "xml":
                case "rss":
                case "atom":
                    contentType = type.Equals("xml") ? "text/xml" : String.Format("application/{0}+xml", type);
                    break;
                default:
                    contentType = "text/" + type;
                    break;
            }
            return Content(rawContent, contentType);
        }

        protected virtual void SetupViewData(int containerSize = 0, MvcData viewData = null)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            if (viewData != null)
            {
                ViewBag.RegionName = viewData.RegionName;
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                ControllerContext.RouteData.DataTokens["area"] = viewData.AreaName;
            }
        }

        protected virtual Type GetViewType(MvcData viewData)
        {
            var key = SiteConfiguration.GetViewModelRegistryKey(viewData);
            return SiteConfiguration.ViewModelRegistry.ContainsKey(key) ? SiteConfiguration.ViewModelRegistry[key] : null;
        }

        protected virtual MvcData GetViewData(object sourceModel)
        {
            if (sourceModel is IEntity)
            {
                return (((IEntity)sourceModel).AppData);
            }
            if (sourceModel is IPage)
            {
                return (((IPage)sourceModel).AppData);
            }
            if (sourceModel is IRegion)
            {
                return (((IRegion)sourceModel).AppData);
            }
            return ContentProvider.ContentResolver.ResolveMvcData(sourceModel);
        }

        protected virtual T GetRequestParameter<T>(string name)
        {
            var val = Request.Params[name];
            if (!String.IsNullOrEmpty(val))
            {
                try
                {
                    return (T)Convert.ChangeType(val,typeof(T));
                }
                catch (Exception)
                {
                    Log.Warn("Could not convert request parameter {0} into type {1}, using type default.", val, typeof(T));
                }
            }
            return default(T);
        }

        /// <summary>
        /// Parse url to actual page with extension
        /// </summary>
        /// <param name="url">Requested url</param>
        /// <returns>Page url</returns>
        private string ParseUrl(string url)
        {
            string defaultPageFileName = ContentProvider.ContentResolver.DefaultPageName;
            if (String.IsNullOrEmpty(url))
            {
                url = defaultPageFileName;
            }
            if (url.EndsWith("/"))
            {
                url = url + defaultPageFileName;
            }
            if (!Path.HasExtension(url))
            {
                url = url + ContentProvider.ContentResolver.DefaultExtension;
            }
            return url;
        }
    }
}
