using System;
using System.Transactions;
using NUnit.Framework;

namespace Veil.DataAccess.Tests.Helpers
{
    /// <summary>
    ///     Adding this attribute wraps a test's execution with transaction and 
    ///     cancels it after the test is done.
    ///     <para>
    ///         This attribute can be applied to both individual [Test] methods as 
    ///         well as [TestFixture] classes.
    ///     </para>
    /// </summary>
    class RollbackAttribute : Attribute, ITestAction
    {
        private TransactionScope transaction;

        public void BeforeTest(TestDetails testDetails)
        {
            transaction = new TransactionScope();
        }

        public void AfterTest(TestDetails testDetails)
        {
            transaction.Dispose();
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}
