/* EmailService.cs
 * Purpose: Email service implementation of IIdentityMEssageService using SendGrid
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.26: Created
 */ 

using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Exceptions;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using SendGrid;

namespace Veil.Services
{
    /// <summary>
    ///     Implementation of <see cref="IIdentityMessageService"/> using SendGrid
    /// </summary>
    [UsedImplicitly]
    public class EmailService : IIdentityMessageService
    {
        /// <summary>
        ///     Sends the message via SendGrid
        /// </summary>
        /// <param name="message">
        ///     The <see cref="IdentityMessage"/> containing the details to be used for sending the email
        /// </param>
        /// <returns>
        ///     A task to be awaited
        /// </returns>
        public async Task SendAsync(IdentityMessage message)
        {
            SendGridMessage mail = new SendGridMessage
            {
                From = new MailAddress(ConfigurationManager.AppSettings["SenderEmailAddress"]),
                To = new[]
                {
                    new MailAddress(message.Destination)
                },
                Subject = message.Subject,
                Html = message.Body
            };

            var transport = new Web(ConfigurationManager.AppSettings["SendGridApiKey"]);

            try
            {
                await transport.DeliverAsync(mail);
            }
            catch (InvalidApiRequestException ex)
            {
                // Note: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ResponseStatusCode);

                foreach (var error in ex.Errors)
                {
                    Debug.WriteLine(error);
                }
            }
            catch (ProtocolViolationException ex)
            {
                // Note: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Note: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
            }
        }
    }
}