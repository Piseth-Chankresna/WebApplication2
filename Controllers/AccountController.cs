using Microsoft.AspNetCore.Mvc;

namespace YourProjectName.Controllers

{
    public class AccountController : Controller
    {
        // នេះគឺជា Action សម្រាប់បង្ហាញទំព័រ Login
        [HttpGet]
        public IActionResult Login()
        {
            // វានឹងទៅរក File នៅក្នុង Views/Account/Login.cshtml ដោយស្វ័យប្រវត្តិ
            return View();
        }

        // នេះគឺជា Action សម្រាប់ទទួលទិន្នន័យពេលចុចប៊ូតុង Login
        [HttpPost]
        public IActionResult ProcessLogin(string username, string password)
        {
            // កូដសម្រាប់ត្រួតពិនិត្យ Username & Password នៅទីនេះ
            // ...

            return RedirectToAction("Index", "Home");
        }
    }
}
