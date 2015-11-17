using System;

namespace Veil.Exceptions
{
    public class NotEnoughInventoryException : Exception
    {
        public NotEnoughInventoryException(string message) : base(message)
        {
            
        }
    }
}