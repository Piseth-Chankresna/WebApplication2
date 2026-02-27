using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace YourProject.Controllers
{
    public class StudentController : Controller
    {
        #region Configuration & Storage

        // ទីតាំងរក្សាទុកទិន្នន័យ (Permanent File Storage)
        private readonly string _classFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.json");
        private readonly string _studentFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "students.json");

        // Helper check ប្រសិនបើជាការហៅតាមរយៈ AJAX
        private bool IsAjaxRequest => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // --- Helper សម្រាប់អាន និងរក្សាទុកទិន្នន័យ ---
        private List<T> LoadData<T>(string path)
        {
            if (!System.IO.File.Exists(path)) return new List<T>();
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        private void SaveData<T>(string path, List<T> data)
        {
            var folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }

        #endregion

        #region View Actions (Navigation)

        // គ្រប់គ្រងរាល់ Navigation ទាំងអស់ឲ្យប្រើ PartialView() នៅពេលហៅតាម AJAX ដើម្បីកុំឲ្យជាន់ Layout
        public IActionResult StudentCenter() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Index() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Create() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Payment() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Reports() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Classes() => IsAjaxRequest ? PartialView() : View();
        public IActionResult Grades() => PartialView();
        public IActionResult Scholarship() => PartialView();

        // --- កែសម្រួលត្រង់នេះ ដើម្បីកុំឲ្យខូច Layout ពេលចុច Attendance ---

        // សម្រាប់ទំព័រ 3.1 Check Attendance
        #region Attendance Management (3.1 & 3.2)

        // សម្រាប់ទំព័រ 3.1 Check Attendance
        public IActionResult CheckAttendance()
        {
            // អានទិន្នន័យសិស្សទាំងអស់ ដើម្បីយកទៅជ្រើសរើសតាមថ្នាក់ក្នុង View
            var students = LoadData<StudentModel>(_studentFilePath);

            // ប្រសិនបើជា AJAX ឱ្យផ្ញើតែ PartialView ជាមួយ Model សិស្ស
            if (IsAjaxRequest)
            {
                return PartialView(students);
            }

            // ប្រសិនបើ Refresh ទំព័រ ឱ្យផ្ញើ View ពេញ (Layout នឹងស្គាល់ដោយស្វ័យប្រវត្តិ)
            return View(students);
        }

        // សម្រាប់ទំព័រ 3.2 Attendance List
        public IActionResult AttendanceList()
        {
            // អានទិន្នន័យសិស្ស ឬអាចទាញទិន្នន័យពី attendance.json (បើបងមាន file រក្សាទុកវត្តមាន)
            var students = LoadData<StudentModel>(_studentFilePath);

            if (IsAjaxRequest)
            {
                return PartialView(students);
            }

            return View(students);
        }

        #endregion
        #endregion

        #region API Actions (Data Management)

        // ========================
        // ១. គ្រប់គ្រងទិន្នន័យសិស្ស (Student Management)
        // ========================

        [HttpGet]
        public JsonResult GetStudents()
        {
            var data = LoadData<StudentModel>(_studentFilePath);
            return Json(data);
        }

        [HttpPost]
        public JsonResult SaveStudent([FromBody] StudentModel model)
        {
            if (model == null) return Json(new { success = false, message = "ទិន្នន័យមិនត្រឹមត្រូវ" });

            var data = LoadData<StudentModel>(_studentFilePath);

            // ស្វែងរកសិស្សតាមរយៈ ID
            var existing = data.FirstOrDefault(x => x.Id == model.Id);

            if (existing == null) // បន្ថែមថ្មី (Create)
            {
                data.Add(model);
            }
            else // កែប្រែចាស់ (Update)
            {
                existing.Name = model.Name;
                existing.Gender = model.Gender;
                existing.Dob = model.Dob;
                existing.Pob = model.Pob;
                existing.Phone = model.Phone;
                existing.Degree = model.Degree;
                existing.Major = model.Major;
                existing.Year = model.Year;
                existing.Scholarship = model.Scholarship;
                existing.Room = model.Room;
                existing.Batch = model.Batch;
                existing.Photo = model.Photo;

                // --- បន្ថែមការ Update Field ថ្មីៗត្រង់នេះ ---
                existing.AcademicYear = model.AcademicYear;
                existing.AmountDue = model.AmountDue;
                existing.Status = model.Status;
            }

            SaveData(_studentFilePath, data);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteStudent(string id)
        {
            var data = LoadData<StudentModel>(_studentFilePath);
            var itemToRemove = data.FirstOrDefault(x => x.Id == id);
            if (itemToRemove != null)
            {
                data.Remove(itemToRemove);
                SaveData(_studentFilePath, data);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ========================
        // ២. គ្រប់គ្រងថ្នាក់រៀន (Class Management)
        // ========================

        [HttpGet]
        public JsonResult GetClasses()
        {
            var data = LoadData<StudentClass>(_classFilePath);
            if (data.Count == 0)
            {
                data.Add(new StudentClass { Id = 1, ClassCode = "IT-GEN12", ClassName = "Full-Stack Dev", Room = "B-01", StudentCount = 24, Time = "08:00 - 10:00" });
                SaveData(_classFilePath, data);
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult SaveClass([FromBody] StudentClass model)
        {
            if (model == null) return Json(new { success = false });
            var data = LoadData<StudentClass>(_classFilePath);
            if (model.Id == 0)
            {
                model.Id = data.Count > 0 ? data.Max(x => x.Id) + 1 : 1;
                data.Add(model);
            }
            else
            {
                var existing = data.FirstOrDefault(x => x.Id == model.Id);
                if (existing != null)
                {
                    existing.ClassCode = model.ClassCode;
                    existing.ClassName = model.ClassName;
                    existing.Room = model.Room;
                    existing.Time = model.Time;
                    existing.StudentCount = model.StudentCount;
                }
            }
            SaveData(_classFilePath, data);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteClass(int id)
        {
            var data = LoadData<StudentClass>(_classFilePath);
            var itemToRemove = data.FirstOrDefault(x => x.Id == id);
            if (itemToRemove != null)
            {
                data.Remove(itemToRemove);
                SaveData(_classFilePath, data);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        #endregion

        // បន្ថែម Action នេះក្នុង StudentController.cs
        [HttpGet]
        public JsonResult GetStudentsByClass(string className)
        {
            var allStudents = LoadData<StudentModel>(_studentFilePath);

            // បើ className ទំនេរ ឱ្យវាមកទាំងអស់ ឬ Filter តាម Class ដែលអ្នកប្រើជ្រើសរើស
            var filteredStudents = string.IsNullOrEmpty(className)
                ? allStudents
                : allStudents.Where(s => s.Room == className).ToList();

            return Json(filteredStudents);
        }

        #region Models
        public class StudentClass
        {
            public int Id { get; set; }
            public string ClassCode { get; set; }
            public string ClassName { get; set; }
            public string Room { get; set; }
            public string Time { get; set; }
            public int StudentCount { get; set; }
        }

        public class StudentModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Gender { get; set; }
            public string Dob { get; set; }
            public string Pob { get; set; }
            public string Phone { get; set; }
            public string Degree { get; set; }
            public string Major { get; set; }
            public string Year { get; set; }
            public string Scholarship { get; set; }
            public string Room { get; set; }
            public string Batch { get; set; }
            public string Photo { get; set; }

            // --- Field ថ្មីៗដែលបន្ថែមសម្រាប់បង ---
            public string AcademicYear { get; set; } // ឆ្នាំសិក្សា
            public string AmountDue { get; set; }    // ចំនួនទឹកប្រាក់
            public string Status { get; set; }       // ស្ថានភាព (Paid/Pending)
        }
        #endregion
    }
}