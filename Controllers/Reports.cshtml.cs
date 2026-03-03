using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace WebApplication2.Controllers  // Add namespace
{
    public class ReportsModel : PageModel
    {
        // បង្កើតតំណាងទិន្នន័យ (Data Model)
        public class ChartData
        {
            public string? Month { get; set; }
            public decimal Amount { get; set; }
        }

        // លទ្ធផលដែលនឹងបញ្ជូនទៅកាន់ JavaScript
        public JsonResult OnGetRevenueData()
        {
            // ក្នុងករណីពិត បងអាចទាញពី Database ដូចជា:
            // var data = _context.Payments.GroupBy(p => p.Month).Select(...).ToList();

            var revenueList = new List<ChartData>
            {
                new() { Month = "Jan", Amount = 1200 },  // Simplified with new()
                new() { Month = "Feb", Amount = 1900 },  // Simplified with new()
                new() { Month = "Mar", Amount = 3000 },  // Simplified with new()
                new() { Month = "Apr", Amount = 2500 },  // Simplified with new()
                new() { Month = "May", Amount = 4200 },  // Simplified with new()
                new() { Month = "Jun", Amount = 3800 }   // Simplified with new()
            };

            return new JsonResult(revenueList);
        }
    }
}