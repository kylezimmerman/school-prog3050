/* TestHelpers.cs
 * Purpose: Helper methods for setting up test mocks using Moq
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.04: Created
 */

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Veil.DataAccess.Interfaces;

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
    }
}
