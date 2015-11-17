/* TestHelpers.cs
 * Purpose: Helper methods for setting up test mocks using Moq
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.04: Created
 */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Moq;
using Moq.Language.Flow;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;

namespace Veil.Tests
{
    /// <summary>
    ///     Contains helper methods for setting up test mocks using Moq
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        ///     Shortcut method for getting a mock of IVeilDataAccess
        /// </summary>
        /// <returns>
        ///     Returns a new instance of Mock for the interface IVeilDataAccess
        /// </returns>
        public static Mock<IVeilDataAccess> GetVeilDataAccessFake()
        {
            Mock<IVeilDataAccess> contextMock = new Mock<IVeilDataAccess>();

            return contextMock;
        }

        public static Mock<ControllerContext> GetSetupControllerContextFakeWithUserIdentitySetup()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            return contextStub;;
        } 

        public static Mock<IGuidUserIdGetter> GetSetupIUserIdGetterFake(Guid returnedId)
        {
            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(returnedId);

            return idGetterStub;
        }

        public static Mock<DbSet<Location>> GetLocationDbSetWithOnlineWarehouse()
        {
            Location onlineWarehouse = new Location
            {
                SiteName = Location.ONLINE_WAREHOUSE_NAME
            };

            Mock<DbSet<Location>> locationDbSetStub =
                GetFakeAsyncDbSet(new List<Location> { onlineWarehouse }.AsQueryable());

            return locationDbSetStub;
        } 

        /// <summary>
        ///     Generic method for getting a <see cref="Mock"/> of <see cref="DbSet{TEntity}"/> of type <see cref="T"/> which supports async methods
        /// </summary>
        /// <typeparam name="T">
        ///     The type for the DbSet
        /// </typeparam>
        /// <param name="queryable">
        ///     IQueryable to use as the Mock DbSet's data source
        /// </param>
        /// <returns>
        ///     Returns a new instance of Mock for the DbSet of type <see cref="T"/>
        /// </returns>
        public static Mock<DbSet<T>> GetFakeAsyncDbSet<T>(IQueryable<T> queryable) where T : class
        {
            Mock<DbSet<T>> mockDbSet = new Mock<DbSet<T>>();

            mockDbSet.
                As<IDbAsyncEnumerable<T>>().
                Setup(dbSet => dbSet.GetAsyncEnumerator()).
                Returns(new TestDbAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockDbSet.As<IQueryable<T>>().
                Setup(m => m.Provider).
                Returns(new TestDbAsyncQueryProvider<T>(queryable.Provider));

            mockDbSet.
                As<IQueryable<T>>().
                Setup(m => m.Expression).
                Returns(queryable.Expression);

            mockDbSet.
                As<IQueryable<T>>().
                Setup(m => m.ElementType).
                Returns(queryable.ElementType);

            mockDbSet.
                As<IQueryable<T>>().
                Setup(m => m.GetEnumerator()).
                Returns(queryable.GetEnumerator());

            return mockDbSet;
        }

        /// <summary>
        ///     Sets up the <see cref="Mock"/> of <see cref="DbSet{Tentity}"/> of type <see cref="T"/> to support calls to Include
        /// </summary>
        /// <typeparam name="T">
        ///     The type for the DbSet
        /// </typeparam>
        /// <param name="dbSetFake">
        ///     The <see cref="Mock{T}"/> of <see cref="DbSet{TEntity}"/> of <see cref="T"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnsResult allowing further setup
        /// </returns>
        /// <remarks>
        ///     This must be called after all setup as it calls .Object on the Mock which prevents further setup
        /// </remarks>
        public static IReturnsResult<DbSet<T>> SetupForInclude<T>(this Mock<DbSet<T>> dbSetFake) where T: class
        {
            return dbSetFake.Setup(dbs => dbs.Include(It.IsAny<string>())).Returns(dbSetFake.Object);
        }

        /// <summary>
        ///     Fluent method to make setting up User settings more readable
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     The same mock this was called with
        /// </returns>
        public static Mock<ControllerContext> SetupUser(this Mock<ControllerContext> contextFake)
        {
            return contextFake;
        }

        /// <summary>
        ///     Sets up the user as in any role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> InAllRoles(this Mock<ControllerContext> contextFake)
        {
            return contextFake.Setup(cm => cm.HttpContext.User.IsInRole(It.IsAny<string>())).Returns(true);
        }

        /// <summary>
        ///     Sets up the user as not being in any roles
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> InNoRoles(this Mock<ControllerContext> contextFake)
        {
            return contextFake.Setup(cm => cm.HttpContext.User.IsInRole(It.IsAny<string>())).Returns(false);
        }

        /// <summary>
        ///     Sets up the user as in the passed role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <param name="role">
        ///     The role to set the user as in
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> IsInRole(this Mock<ControllerContext> contextFake, string role)
        {
            return contextFake.Setup(cm => cm.HttpContext.User.IsInRole(role)).Returns(true);
        }

        /// <summary>
        ///     Sets up the user as not in the passed role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <param name="role">
        ///     The role to set the user as not in
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> IsNotInRole(this Mock<ControllerContext> contextFake, string role)
        {
            return contextFake.Setup(cm => cm.HttpContext.User.IsInRole(role)).Returns(false);
        }

        /// <summary>
        ///     Sets up the user as in the Employee role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> InEmployeeRole(this Mock<ControllerContext> contextFake)
        {
            return IsInRole(contextFake, VeilRoles.EMPLOYEE_ROLE);
        }

        /// <summary>
        ///     Sets up the user as in the Member role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> InMemberRole(this Mock<ControllerContext> contextFake)
        {
            return IsInRole(contextFake, VeilRoles.MEMBER_ROLE);
        }

        /// <summary>
        ///     Sets up the user as in the Admin role
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> InAdminRole(this Mock<ControllerContext> contextFake)
        {
            return IsInRole(contextFake, VeilRoles.ADMIN_ROLE);
        }

        /// <summary>
        ///     Sets up the user's authenticated status equal to the passed isAuthenticated
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <param name="isAuthenticated">
        ///     bool indicating the authentication status
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        private static IReturnsResult<ControllerContext> AuthenticatedStatus(this Mock<ControllerContext> contextFake, bool isAuthenticated)
        {
            return contextFake.Setup(cm => cm.HttpContext.User.Identity.IsAuthenticated).Returns(isAuthenticated);
        }

        /// <summary>
        ///     Sets up the user as not authenticated
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> IsNotAuthenticated(this Mock<ControllerContext> contextFake)
        {
            return AuthenticatedStatus(contextFake, false);
        }

        /// <summary>
        ///     Sets up the user as authenticated
        /// </summary>
        /// <param name="contextFake">
        ///     The <see cref="Mock{T}"/> of <see cref="ControllerContext"/> to setup
        /// </param>
        /// <returns>
        ///     IReturnResults allowing further setup
        /// </returns>
        public static IReturnsResult<ControllerContext> IsAuthenticated(this Mock<ControllerContext> contextFake)
        {
            return AuthenticatedStatus(contextFake, true);
        }

        #region Async Infrastructure Classes
        /* Code from: https://msdn.microsoft.com/en-us/data/dn314429#async */
        private class TestDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal TestDbAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestDbAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestDbAsyncEnumerable<TElement>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute(expression));
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute<TResult>(expression));
            }
        }

        private class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
        {
            public TestDbAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            public TestDbAsyncEnumerable(Expression expression)
                : base(expression)
            {
            }

            public IDbAsyncEnumerator<T> GetAsyncEnumerator()
            {
                return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
            {
                return GetAsyncEnumerator();
            }

            IQueryProvider IQueryable.Provider
            {
                get
                {
                    return new TestDbAsyncQueryProvider<T>(this);
                }
            }
        }

        private class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestDbAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public void Dispose()
            {
                _inner.Dispose();
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_inner.MoveNext());
            }

            public T Current
            {
                get
                {
                    return _inner.Current;
                }
            }

            object IDbAsyncEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    #endregion Async Infrastructure Classes
    }
}
