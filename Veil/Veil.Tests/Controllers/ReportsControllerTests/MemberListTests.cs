using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models.Reports;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    public class MemberListTests : ReportsControllerTestsBase
    {
        [Test]
        public async void MemberWithoutOrders_IncludesMemberInList()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model, Has.Count.EqualTo(1));
        }

        [Test]
        public async void WhenCalled_SetsUserNameToUsersUserName()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>) result.Model;

            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
        }

        [Test]
        public async void WhenCalled_SetsFullNameFromUsersNames()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().FullName, Is.EqualTo("John Doe"));
        }

        [Test]
        public async void MemberWithNoOrders_SetsOrderCountTo0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(0));
        }

        [Test]
        public async void MemberWithNoOrders_SetsTotalSpentOnOrders0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(0));
        }

        [Test]
        public async void MemberWithNoOrders_SetsAverageOrderTotal0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(0));
        }

        [Test]
        public async void MemberWithFiveOrders_SetsOrderCountToFive()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId },
                new WebOrder { MemberId = memberId },
                new WebOrder { MemberId = memberId },
                new WebOrder { MemberId = memberId },
                new WebOrder { MemberId = memberId }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(5));
        }

        [Test]
        public async void MemberWithOrders_SetsCorrectTotalSpentOnOrders()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(100m));
        }

        [Test]
        public async void MemberWithOrders_SetsCorrectAverageOrderTotal()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 1m, TaxAmount = 0m, ShippingCost = 1m }, /* $2 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 0m, TaxAmount = 1m, ShippingCost = 1m }, /* $2 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 2m, TaxAmount = 0m, ShippingCost = 2m }, /* $4 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 2m, TaxAmount = 2m, ShippingCost = 0m }  /* $4 */
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(3m));
        }

        [Test]
        public async void TwoMembersWithDifferentTotals_OrdersByTotalSpentDescending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThanOrEqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void TwoMembersWithSameTotals_ThenOrdersByAverageOrderDescending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().AverageOrderTotal, Is.GreaterThanOrEqualTo(model.Last().AverageOrderTotal));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void DateFilter_MemberWithoutOrders_IncludesMemberInList()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model, Has.Count.EqualTo(1));
        }

        [Test]
        public async void DateFilter_WhenCalled_SetsUserNameToUsersUserName()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
        }

        [Test]
        public async void DateFilter_WhenCalled_SetsFullNameFromUsersNames()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().FullName, Is.EqualTo("John Doe"));
        }

        [Test]
        public async void DateFilter_MemberWithNoOrders_SetsOrderCountTo0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(0));
        }

        [Test]
        public async void DateFilter_MemberWithNoOrders_SetsTotalSpentOnOrders0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(0));
        }

        [Test]
        public async void DateFilter_MemberWithNoOrders_SetsAverageOrderTotal0()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MinValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(0));
        }

        [Test]
        public async void DateFilter_MemberWithFiveOrders_SetsOrderCountToFive()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MaxValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(5));
        }

        [Test]
        public async void DateFilter_MemberWithThreeOrdersInDateRange_SetsOrderCountToThree()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2010, 01, 01) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderDate = new DateTime(2010, 01, 01) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 22), end: new DateTime(2015, 11, 22)) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(3));
        }

        [Test]
        public async void DateFilter_MemberWithOrders_SetsCorrectTotalSpentOnOrders()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MaxValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(100m));
        }

        [Test]
        public async void DateFilter_MemberWithThreeOrdersInDateRanges_SetsCorrectTotalSpentOnOrders()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 22), end: new DateTime(2015, 11, 22)) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(60m));
        }

        [Test]
        public async void DateFilter_MemberWithOrders_SetsCorrectAverageOrderTotal()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 1m, TaxAmount = 0m, ShippingCost = 1m, OrderDate = new DateTime(2015, 11, 22) }, /* $2 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 0m, TaxAmount = 1m, ShippingCost = 1m, OrderDate = new DateTime(2015, 11, 22) }, /* $2 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 2m, TaxAmount = 0m, ShippingCost = 2m, OrderDate = new DateTime(2015, 11, 22) }, /* $4 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 2m, TaxAmount = 2m, ShippingCost = 0m, OrderDate = new DateTime(2015, 11, 22) }  /* $4 */
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MaxValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(3m));
        }

        [Test]
        public async void DateFilter_MemberWithSomeOrdersInDateRange_SetsCorrectAverageOrderTotal()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 1m, TaxAmount = 0m, ShippingCost = 1m, OrderDate = new DateTime(2015, 11, 22) }, /* $2 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) }, /* $30 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 2m, TaxAmount = 0m, ShippingCost = 2m, OrderDate = new DateTime(2015, 11, 22) }, /* $4 */
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) }  /* $30 */
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 22), end: new DateTime(2015, 11, 22)) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(3m));
        }

        [Test]
        public async void DateFilter_TwoMembersWithDifferentTotals_OrdersByTotalSpentDescending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MaxValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThan(0));
            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThanOrEqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void DateFilter_TwoMembersWithDifferentTotals_OrdersByTotalSpentDescendingOfOrdersInDateRange()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2010, 01, 01) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 22), end: new DateTime(2015, 11, 22)) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThan(0));
            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThanOrEqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void DateFilter_TwoMembersWithSameTotals_ThenOrdersByAverageOrderDescending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: DateTime.MinValue, end: DateTime.MaxValue) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThan(0));
            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().AverageOrderTotal, Is.GreaterThanOrEqualTo(model.Last().AverageOrderTotal));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void DateFilter_TwoMembersWithSameTotals_ThenOrdersByAverageOrderDescendingOfOrdersInDateRange()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2010, 01, 01) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2015, 11, 22) },
                new WebOrder { MemberId = memberId2, OrderSubtotal = 5m, TaxAmount = 5m, ShippingCost = 5m, OrderDate = new DateTime(2010, 01, 01) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 22), end: new DateTime(2015, 11, 22)) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().TotalSpentOnOrders, Is.GreaterThan(0));
            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(model.Last().TotalSpentOnOrders));
            Assert.That(model.First().AverageOrderTotal, Is.GreaterThanOrEqualTo(model.Last().AverageOrderTotal));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
            Assert.That(model.Last().UserName, Is.EqualTo(memberUser2.UserName));
        }

        [Test]
        public async void DateFilter_NullEndDate_DefaultsToNow()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser, memberUser2);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>
            {
                new WebOrder { MemberId = memberId, OrderSubtotal = 10m, TaxAmount = 10m, ShippingCost = 10m, OrderDate = new DateTime(2015, 11, 22) }
            });

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberList(start: new DateTime(2015, 11, 21), end: null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<List<MemberListItemViewModel>>());

            var model = (List<MemberListItemViewModel>)result.Model;

            Assert.That(model.First().OrderCount, Is.EqualTo(1));
            Assert.That(model.First().TotalSpentOnOrders, Is.EqualTo(30));
            Assert.That(model.First().AverageOrderTotal, Is.EqualTo(30));
            Assert.That(model.First().UserName, Is.EqualTo(memberUser.UserName));
        }
    }
}
