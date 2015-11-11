using System;
using System.Collections;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Serialization;

namespace Veil.Tests
{
    /// <summary>
    ///     Static class which uses reflection to create and setup an SqlException for testing purposes
    /// </summary>
    /// <remarks>
    ///     Source: https://gist.github.com/benjanderson/07e13d9a2068b32c2911
    /// </remarks>
    internal static class SqlExceptionCreator
    {
        internal static SqlException Create(string message, int number)
        {
            SqlException exception = Instantiate<SqlException>();
            SetProperty(exception, "_message", message);

            var errors = new ArrayList();

            var errorCollection = Instantiate<SqlErrorCollection>();
            SetProperty(errorCollection, "errors", errors);

            var error = Instantiate<SqlError>();
            SetProperty(error, "number", number);
            errors.Add(error);

            SetProperty(exception, "_errors", errorCollection);

            return exception;
        }

        private static T Instantiate<T>() where T : class
        {
            return FormatterServices.GetUninitializedObject(typeof(T)) as T;
        }

        private static void SetProperty<T>(T targetObject, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(targetObject, value);
            }
            else
            {
                throw new InvalidOperationException("No field with name " + fieldName);
            }
        }
    }
}
