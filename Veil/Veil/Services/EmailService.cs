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
    [UsedImplicitly]
    public class EmailService : IIdentityMessageService
    {
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
                // TODO: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ResponseStatusCode);

                foreach (var error in ex.Errors)
                {
                    Debug.WriteLine(error);
                }
            }
            catch (ProtocolViolationException ex)
            {
                // TODO: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
            }
            catch (ArgumentException ex)
            {
                // TODO: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
            }
        }
    }
}