/* StripeServiceException.cs
 * Purpose: Exception thrown by IStripeService when an error occurs
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.03: Created
 */ 

using System;
using Veil.Services.Interfaces;

namespace Veil.Exceptions
{
    /// <summary>
    ///     Enumeration of the types of StripeServiceException
    /// </summary>
    public enum StripeExceptionType
    {
        /// <summary>
        ///     The Api key being used is invalid
        /// </summary>
        ApiKeyError,

        /// <summary>
        ///     There was an error with the card or card information
        /// </summary>
        CardError,

        /// <summary>
        ///     The error was on Stripe's end
        /// </summary>
        ServiceError,

        /// <summary>
        ///     Unable to determine the cause of the error
        /// </summary>
        UnknownError
    }

    /// <summary>
    ///     Exception thrown by <see cref="IStripeService"/> when an error occurs while
    ///     interacting with Stripe
    /// </summary>
    public class StripeServiceException : Exception
    {
        /// <summary>
        ///     The specific type for this exception
        /// </summary>
        public StripeExceptionType ExceptionType { get; set; } = StripeExceptionType.UnknownError;

        /// <summary>
        ///     Instantiates a new instance of StripeServiceException with the provided arguments
        /// </summary>
        /// <param name="message">
        ///     The exception message
        /// </param>
        /// <param name="type">
        ///     The <see cref="StripeExceptionType"/> for the exception
        /// </param>
        public StripeServiceException(string message, StripeExceptionType type) : base(message)
        {
            ExceptionType = type;
        }

        /// <summary>
        ///     Instantiates a new instance of StripeServiceException with the provided arguments
        /// </summary>
        /// <param name="message">
        ///     The exception message
        /// </param>
        /// <param name="type">
        ///     The <see cref="StripeExceptionType"/> for the exception
        /// </param>
        /// <param name="inner">
        ///     The inner exception that originally caused this exception
        /// </param>
        public StripeServiceException(string message, StripeExceptionType type, Exception inner)
            : base(message, inner)
        {
            ExceptionType = type;
        }
    }
}