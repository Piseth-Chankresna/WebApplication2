using Microsoft.AspNetCore.Mvc;
using WebApplication2.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace WebApplication2.Controllers
{
    public class UserController : Controller
    {
        // ទីតាំង File សម្រាប់រក្សាទុកទិន្នន័យ (ទាំង Users និង Permissions)
        private static readonly string _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "system_database.json");

        private static List<UserAccount> _users = new List<UserAccount>();
        private static Dictionary<string, List<RolePermission>> _rolePermissions = new Dictionary<string, List<RolePermission>>();

        public UserController()
        {
            LoadData(); // Load ទិន្នន័យរាល់ពេល Controller ហៅប្រើ
        }

        // --- មុខងារ Persistence (Save/Load) ---
        private void LoadData()
        {
            if (System.IO.File.Exists(_dbPath))
            {
                var json = System.IO.File.ReadAllText(_dbPath);
                var data = JsonSerializer.Deserialize<SystemDataWrapper>(json);
                if (data != null)
                {
                    _users = data.Users ?? new List<UserAccount>();
                    _rolePermissions = data.RolePermissions ?? new Dictionary<string, List<RolePermission>>();
                }
            }

            // បើកដំបូងបំផុត មិនទាន់មានទិន្នន័យ ឱ្យវាបង្កើត Admin មួយទុកសិន
            if (_users.Count == 0)
            {
                _users.Add(new UserAccount { Id = 1, FullName = "Admin User", Email = "admin@hru.edu.kh", Role = "Super Admin", IsActive = true });
                SaveData();
            }
        }

        private void SaveData()
        {
            var data = new SystemDataWrapper { Users = _users, RolePermissions = _rolePermissions };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_dbPath, json);
        }

        // 1. បង្ហាញបញ្ជីឈ្មោះអ្នកប្រើប្រាស់ (Index)
        public IActionResult Index()
        {
            var model = _users.OrderByDescending(u => u.Id).ToList();
            return PartialView(model);
        }

        // 2. បើកទំព័របង្កើតអ្នកប្រើប្រាស់ (GET)
        public IActionResult Create() => PartialView();

        // 3. ទទួលទិន្នន័យពី AJAX Form ហើយរក្សាទុក (POST)
        [HttpPost]
        public IActionResult Create(UserAccount user)
        {
            if (ModelState.IsValid)
            {
                user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
                _users.Add(user);
                SaveData(); // រក្សាទុកចូល File ភ្លាមៗ
                return Json(new { success = true, message = "រក្សាទុកអ្នកប្រើប្រាស់ថ្មីបានជោគជ័យ!" });
            }
            return Json(new { success = false, message = "សូមបំពេញព័ត៌មានឱ្យបានត្រឹមត្រូវ!" });
        }

        // 4. បើកទំព័រកែប្រែទិន្នន័យ (GET)
        public IActionResult Edit(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return PartialView(user);
        }

        // 5. រក្សាទុកការកែប្រែ (POST)
        [HttpPost]
        public IActionResult Edit(UserAccount updatedUser)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (existingUser != null && ModelState.IsValid)
            {
                existingUser.FullName = updatedUser.FullName;
                existingUser.Email = updatedUser.Email;
                existingUser.Role = updatedUser.Role;
                existingUser.IsActive = updatedUser.IsActive;
                SaveData(); // រក្សាទុកការកែប្រែចូល File
                return Json(new { success = true, message = "ធ្វើបច្ចុប្បន្នភាពបានជោគជ័យ!" });
            }
            return Json(new { success = false, message = "មិនអាចកែប្រែទិន្នន័យបានទេ។" });
        }

        // 6. លុបអ្នកប្រើប្រាស់ (POST)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
                SaveData(); // រក្សាទុកការលុបចូល File
                return Json(new { success = true, message = "លុបបានជោគជ័យ!" });
            }
            return Json(new { success = false, message = "រកមិនឃើញទិន្នន័យសម្រាប់លុប!" });
        }

        // --- មុខងារសម្រាប់ Role & Permissions ---
        public IActionResult Roles() => PartialView();

        [HttpGet]
        public IActionResult GetPermissions(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return Json(new List<RolePermission>());

            if (!_rolePermissions.ContainsKey(roleName))
            {
                var modules = new List<string> { "User Management", "Student Records", "Financial Reports", "Inventory", "Settings" };
                _rolePermissions[roleName] = modules.Select(m => new RolePermission { ModuleName = m, CanView = true }).ToList();
                SaveData();
            }
            return Json(_rolePermissions[roleName]);
        }

        [HttpPost]
        public IActionResult SavePermissions(string roleName, List<RolePermission> permissions)
        {
            if (string.IsNullOrEmpty(roleName) || permissions == null) return Json(new { success = false });

            _rolePermissions[roleName] = permissions;
            SaveData();
            return Json(new { success = true, message = "រក្សាទុកសិទ្ធិរួចរាល់!" });
        }

        public IActionResult ResetPassword(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return PartialView(user);
        }
    }

    // Helper Class សម្រាប់រៀបចំរចនាសម្ព័ន្ធ JSON
    public class SystemDataWrapper
    {
        public List<UserAccount> Users { get; set; }
        public Dictionary<string, List<RolePermission>> RolePermissions { get; set; }
    }
}