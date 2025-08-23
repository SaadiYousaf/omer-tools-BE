using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ProductService.Business.Services
{
    public class RazorTemplateRenderer : ITemplateRenderer
    {
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public RazorTemplateRenderer(
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        public async Task<string> RenderTemplateAsync<TModel>(string templateName, TModel model)
        {
            var viewEngineResult = _viewEngine.GetView("~/", $"~/Templates/{templateName}.cshtml", false);

            if (!viewEngineResult.Success)
            {
                throw new FileNotFoundException($"Template {templateName} not found");
            }

            var view = viewEngineResult.View;
            var tempData = new TempDataDictionary(new DefaultHttpContext(), _tempDataProvider);
            var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            using var writer = new StringWriter();
            var viewContext = new ViewContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()),
                view,
                viewData,
                tempData,
                writer,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}