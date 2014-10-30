﻿using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Controllers;
using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    public class ListController : BaseController
    {
        public ListController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }

        /// <summary>
        /// Populate/Map and render a list entity model
        /// </summary>
        /// <param name="entity">The list entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered list entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult List(object entity, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            var model = ProcessModel(entity, GetViewType(viewData)) ?? entity;
            return View(viewData.ViewName, model);
        }

        public override object ProcessModel(object sourceModel, System.Type type)
        {
            var model = base.ProcessModel(sourceModel, type) as ContentList<Teaser>;
            if (model != null)
            {
                if (model.ItemListElements.Count == 0)
                {
                    //we need to run a query to populate the list
                    int start = GetRequestParameter<int>("start");
                    if (model.Id == Request.Params["id"])
                    {
                        //we only take the start from the query string if there is also an id parameter matching the model entity id
                        //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                        model.CurrentPage = (start / model.PageSize) + 1;
                        model.Start = start;
                    }
                    ContentProvider.PopulateDynamicList(model);
                }
            }
            return model;
        }
    }
}
