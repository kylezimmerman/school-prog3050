/* CompaniesController.cs
 * Purpose: Controller for adding or removing publisher/development companies
 * 
 * Revision History:
 *      Isaac West, 2015.12.02: Created
 */ 

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller used to manage companies for use as a product developer and/or publisher
    /// </summary>
    [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
    public class CompaniesController : BaseController
    {
        private readonly IVeilDataAccess db;

        /// <summary>
        ///     Instantiates a new instance of CompaniesController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        public CompaniesController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        ///     Presents a page to create and delete companies for use as a developer and/or publisher
        /// </summary>
        /// <param name="newCompanyName">
        ///     The name of a company that could not be added
        /// </param>
        /// <returns>
        ///     View with controls for the creation and deletion of companies
        /// </returns>
        public async Task<ActionResult> Manage(string newCompanyName)
        {
            var deletableCompanies = await db.Companies.Where(
                c =>
                    c.DevelopedGameProducts.Count +
                        c.PublishedGameProducts.Count < 1)
                .OrderBy(c => c.Name).ToListAsync();

            var model = new CompanyViewModel
            {
                Deletable = new SelectList(deletableCompanies, "Id", "Name"),
                NewCompany = newCompanyName
            };

            return View(model);
        }

        /// <summary>
        ///     Adds a company for use as a developer and/or publisher
        /// </summary>
        /// <param name="newCompany">
        ///     The name of the new company to add
        /// </param>
        /// <returns>
        ///     Redirects to the Manage action
        /// </returns>
        public async Task<ActionResult> Create(string newCompany)
        {
            if (ModelState.IsValid)
            {
                if (await db.Companies.AnyAsync(c => c.Name == newCompany))
                {
                    this.AddAlert(
                        AlertType.Info, $"{newCompany} is already in the system and cannot be added again.");
                }
                else
                {
                    db.Companies.Add(
                        new Company
                        {
                            Name = newCompany
                        });

                    await db.SaveChangesAsync();

                    this.AddAlert(AlertType.Success, $"{newCompany} has been added.");

                    return RedirectToAction("Manage");
                }
            }

            return RedirectToAction("Manage", new { newCompanyName = newCompany });
        }

        /// <summary>
        ///     Deletes a company
        /// </summary>
        /// <param name="toDelete">
        ///     The id of the company to delete
        /// </param>
        /// <returns>
        ///     Redirects to the Manage action
        ///     404 Not Found view if the id does not match a company
        ///     Manage view with error if the company cannot be deleted due to referrential constraints
        /// </returns>
        public async Task<ActionResult> Delete(Guid? toDelete)
        {
            if (toDelete == null)
            {
                throw new HttpException(NotFound, nameof(Company));
            }

            var company = await db.Companies.FindAsync(toDelete);

            if (company == null)
            {
                throw new HttpException(NotFound, nameof(Company));
            }

            if (company.DevelopedGameProducts.Count +
                company.PublishedGameProducts.Count > 0)
            {
                this.AddAlert(
                    AlertType.Error, $"{company.Name} cannot be deleted because it has related products.");
            }
            else
            {
                db.Companies.Remove(company);

                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"{company.Name} has been deleted.");
            }

            return RedirectToAction("Manage");
        }
    }
}