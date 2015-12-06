using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class CompaniesControllerTests
    {
        [TestCase(null)]
        [TestCase("FirstCompany")]
        public async void Manage_ReturnsMatchingModel(string newCompanyName)
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                },
                new Company
                {
                    Name = "HiddenCompany",
                    DevelopedGameProducts = new List<GameProduct>
                    {
                        new PhysicalGameProduct()
                    },
                    PublishedGameProducts = new List<GameProduct>()
                },
                new Company
                {
                    Name = "SecondCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            var result = await controller.Manage(newCompanyName) as ViewResult;

            Assert.That(result != null);

            var model = (CompanyViewModel)result.Model;

            Assert.That(model.Deletable.Count(), Is.EqualTo(2));
        }

        [Test]
        public async void Create_InvalidNewCompany_RedirectsToManage()
        {
            CompaniesController controller = new CompaniesController(null);
            controller.ModelState.AddModelError("key", "error message");

            var result = await controller.Create("") as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Manage"));
            Assert.That(result.RouteValues["newCompanyName"], Is.EqualTo(""));
        }

        [Test]
        public async void Create_ValidNewCompany_RedirectsToManage()
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            companyDbSetStub.Setup(db => db.Add(It.IsAny<Company>())).Callback<Company>(companies.Add);
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            var result = await controller.Create("NewCompany") as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Manage"));
            Assert.That(companies.Count, Is.EqualTo(2));
        }

        [Test]
        public async void Create_AlreadyExsists_RedirectsToManage()
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                },
                new Company
                {
                    Name = "NewCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            companyDbSetStub.Setup(db => db.Add(It.IsAny<Company>())).Callback<Company>(companies.Add);
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            var result = await controller.Create("NewCompany") as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Manage"));
            Assert.That(result.RouteValues["newCompanyName"], Is.EqualTo("NewCompany"));
            Assert.That(companies.Count, Is.EqualTo(2));
        }

        [Test]
        public void Delete_CompanyIsNull_Throws404Exception()
        {
            CompaniesController controller = new CompaniesController(null);

            Assert.That(async () => await controller.Delete(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Delete_CompanyDoesNotExist_Throws404Exception()
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Id = new Guid("99D59A63-ADFC-4C4D-82EB-DC9FADC6371D"),
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                }
            };
            
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            companyDbSetStub.Setup(db => db.Add(It.IsAny<Company>())).Callback<Company>(companies.Add);
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            Assert.That(async () => await controller.Delete(new Guid("E1F95A79-5C5A-4BBC-BF88-DA4E2DC177B3")), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Delete_CompanyHasGameProducts_HasCorrectError()
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Id = new Guid("99D59A63-ADFC-4C4D-82EB-DC9FADC6371D"),
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>
                    {
                        new PhysicalGameProduct()
                    },
                    PublishedGameProducts = new List<GameProduct>()
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            companyDbSetStub.Setup(db => db.FindAsync(companies[0].Id)).ReturnsAsync(companies[0]);
            companyDbSetStub.Setup(db => db.Remove(It.IsAny<Company>()))
                .Returns<Company>(c => { companies.Remove(c); return c; });
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            var result = await controller.Delete(companies[0].Id) as RedirectToRouteResult;

            Assert.That(result != null);

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo($"{companies[0].Name} cannot be deleted because it has related products."));

            Assert.That(companies.Count, Is.EqualTo(1));
        }

        [Test]
        public async void Delete_ValidDeletion_RedirectsToManage()
        {
            List<Company> companies = new List<Company>
            {
                new Company
                {
                    Id = new Guid("99D59A63-ADFC-4C4D-82EB-DC9FADC6371D"),
                    Name = "FirstCompany",
                    DevelopedGameProducts = new List<GameProduct>(),
                    PublishedGameProducts = new List<GameProduct>()
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Company>> companyDbSetStub = TestHelpers.GetFakeAsyncDbSet(companies.AsQueryable());
            companyDbSetStub.Setup(db => db.FindAsync(companies[0].Id)).ReturnsAsync(companies[0]);
            companyDbSetStub.Setup(db => db.Remove(It.IsAny<Company>()))
                .Returns<Company>(c => { companies.Remove(c); return c; });
            dbStub.Setup(db => db.Companies).Returns(companyDbSetStub.Object);

            CompaniesController controller = new CompaniesController(dbStub.Object);

            var result = await controller.Delete(companies[0].Id) as RedirectToRouteResult;

            Assert.That(result != null);

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Manage"));
            Assert.That(companies.Count, Is.EqualTo(0));
        }
    }
}
