using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ReportsModel : PageModel
{
    // បង្កើតតំណាងទិន្នន័យ (Data Model)
    public class ChartData
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
    }

    // លទ្ធផលដែលនឹងបញ្ជូនទៅកាន់ JavaScript
    public JsonResult OnGetRevenueData()
    {
        // ក្នុងករណីពិត បងអាចទាញពី Database ដូចជា:
        // var data = _context.Payments.GroupBy(p => p.Month).Select(...).ToList();

        var revenueList = new List<ChartData>
        {
            new ChartData { Month = "Jan", Amount = 1200 },
            new ChartData { Month = "Feb", Amount = 1900 },
            new ChartData { Month = "Mar", Amount = 3000 },
            new ChartData { Month = "Apr", Amount = 2500 },
            new ChartData { Month = "May", Amount = 4200 },
            new ChartData { Month = "Jun", Amount = 3800 }
        };

        return new JsonResult(revenueList);
    }
}