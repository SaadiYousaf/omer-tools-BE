using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface ITemplateRenderer
    {
        Task<string> RenderTemplateAsync<TModel>(string templateName, TModel model);
    }
}
