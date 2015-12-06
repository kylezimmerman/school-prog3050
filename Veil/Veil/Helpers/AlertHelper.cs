/* AlertHelper.cs
 * Purpose: Contains code related to adding Alerts on pages
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.10.30: Created
 */ 

using System.Collections.Generic;
using System.Web.Mvc;

namespace Veil.Helpers
{
    /// <summary>
    ///     Enumeration of the types of Alerts
    /// </summary>
    public enum AlertType
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Success = 3
    }

    /// <summary>
    ///     Class for information about a specific alert
    /// </summary>
    public class AlertMessage
    {
        /// <summary>
        ///     The <see cref="AlertType"/> for this alert message
        /// </summary>
        public AlertType Type { get; set; }

        /// <summary>
        ///     The message for this alert
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     WARNING: This is rendered using Html.Raw. Do not include user input in this string
        ///     The link, including anchor &lt;a&gt; tags, to include in the alert
        /// </summary>
        public string Link { get; set; }

        public string AlertClass
        {
            get
            {
                switch (Type)
                {
                    case AlertType.Error:
                        return "alert";
                    case AlertType.Warning:
                        return "warning";
                    case AlertType.Info:
                        return "info";
                    case AlertType.Success:
                        return "success";
                    default:
                        return "";
                }
            }
        }
    }

    /// <summary>
    ///     Extensions methods for adding Alerts to a Controller
    /// </summary>
    public static class AlertHelper
    {
        public const string ALERT_MESSAGE_KEY = "AlertMessages";

        /// <summary>
        ///     Adds an alert of the specified type with the specified message
        /// </summary>
        /// <param name="controller">
        ///     The <see cref="Controller"/> to use to add the alert
        /// </param>
        /// <param name="type">
        ///     The <see cref="AlertType"/> for the alert
        /// </param>
        /// <param name="message">
        ///     The message for the alert
        /// </param>
        public static void AddAlert(
            this Controller controller, AlertType type, string message)
        {
            List<AlertMessage> alerts = controller.TempData[ALERT_MESSAGE_KEY] as List<AlertMessage> ??
                new List<AlertMessage>();

            alerts.Add(new AlertMessage() { Type = type, Message = message});
            controller.TempData["AlertMessages"] = alerts;
        }

        /// <summary>
        ///     Adds an alert of the specified type with the specified message and link
        /// </summary>
        /// <param name="controller">
        ///     The <see cref="Controller"/> to use to add the alert
        /// </param>
        /// <param name="type">
        ///     The <see cref="AlertType"/> for the alert
        /// </param>
        /// <param name="message">
        ///     The message for the alert
        /// </param>
        /// <param name="link">
        ///     WARNING: This is rendered using Html.Raw. Do not include user input in this string
        ///     <br/>
        ///     The link, including anchor &lt;a&gt; tags, to include in the alert
        /// </param>
        public static void AddAlert(
            this Controller controller, AlertType type, string message, string link)
        {
            List<AlertMessage> alerts = controller.TempData[ALERT_MESSAGE_KEY] as List<AlertMessage> ??
                new List<AlertMessage>();

            alerts.Add(new AlertMessage() { Type = type, Message = message, Link = link });
            controller.TempData["AlertMessages"] = alerts;
        }

        /// <summary>
        ///     Removes all alerts 
        /// </summary>
        /// <param name="controller">
        ///     The <see cref="Controller"/> to clear the alerts from
        /// </param>
        public static void ClearAlerts(this Controller controller)
        {
            controller.TempData.Remove("AlertMessages");
        }
    }
}