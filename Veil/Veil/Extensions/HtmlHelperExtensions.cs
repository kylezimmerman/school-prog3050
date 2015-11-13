using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Veil.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString NamelessEditorFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object additionalViewData)
        {
            MvcHtmlString textBox = htmlHelper.EditorFor(expression, additionalViewData);

            string pattern = @" name=""([^""]*)""";

            string fixedHtml = Regex.Replace(textBox.ToHtmlString(), pattern, "");

            return new MvcHtmlString(fixedHtml);
        }

        public static MvcHtmlString NamelessDropDownListFor<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            MvcHtmlString select = htmlHelper.DropDownListFor(expression, selectList, htmlAttributes);

            string pattern = @" name=""([^""]*)""";

            string fixedHtml = Regex.Replace(select.ToHtmlString(), pattern, "");

            return new MvcHtmlString(fixedHtml);
        }
    }
}