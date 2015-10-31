using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Exceptions;
using Microsoft.AspNet.Identity;
using SendGrid;

namespace Veil.Services
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            return SendEmail(message);
        }

        private Task SendEmail(IdentityMessage message)
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
                return transport.DeliverAsync(mail);
            }
            catch (InvalidApiRequestException ex)
            {
                // TODO: We would want to log this, but don't really have any course of action for resolving
                Debug.WriteLine(ex.Message);
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

            // Return a completed result
            return Task.FromResult(0);
        }
    }
}