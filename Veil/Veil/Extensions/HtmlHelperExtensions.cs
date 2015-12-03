/* HtmlHelperExtensions.cs
 * Purpose: HtmlHelper extensions methods
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.11: Created
 */ 

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Veil.Extensions
{
    /// <summary>
    ///     Extension methods for <see cref="HtmlHelper{T}"/>
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        ///     Generates a editor input without the name attribute
        /// </summary>
        /// <typeparam name="TModel">
        ///     The model type
        /// </typeparam>
        /// <typeparam name="TProperty">
        ///     The property type
        /// </typeparam>
        /// <param name="htmlHelper">
        ///     The <see cref="HtmlHelper{TModel}"/> to use to generate the editor input
        /// </param>
        /// <param name = "expression"> 
        ///     An expression that identifies the object that contains the properties to display.
        /// </param>
        /// <param name="additionalViewData">
        ///     An anonymous object that can contain additional view data that will be merged into the
        ///     <see cref="T:System.Web.Mvc.ViewDataDictionary`1"/> instance that is created for 
        ///     the template.
        /// </param>
        /// <returns>
        ///     The generated <see cref="MvcHtmlString"/> without the name attribute
        /// </returns>
        public static MvcHtmlString NamelessEditorFor<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression,
            object additionalViewData)
        {
            MvcHtmlString textBox = htmlHelper.EditorFor(expression, additionalViewData);

            string pattern = @" name=""([^""]*)""";

            string fixedHtml = Regex.Replace(textBox.ToHtmlString(), pattern, "");

            return new MvcHtmlString(fixedHtml);
        }

        /// <summary>
        ///     Generates a drop down list without the name attribute
        /// </summary>
        /// <typeparam name="TModel">
        ///     The model type
        /// </typeparam>
        /// <typeparam name="TProperty">
        ///     The property type
        /// </typeparam>
        /// <param name="htmlHelper">
        ///     The <see cref="HtmlHelper{TModel}"/> to use to generate the editor input
        /// </param>
        /// <param name = "expression"> 
        ///     An expression that identifies the object that contains the properties to display.
        /// </param>
        /// <param name="selectList">
        ///     A collection of <see cref="T:System.Web.Mvc.SelectListItem"/> objects that are used
        ///     to populate the drop-down list.
        /// </param>
        /// <param name="htmlAttributes">
        ///     An object that contains the HTML attributes to set for the element.
        /// </param>
        /// <returns>
        ///     The generated <see cref="MvcHtmlString"/> without the name attribute
        /// </returns>
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