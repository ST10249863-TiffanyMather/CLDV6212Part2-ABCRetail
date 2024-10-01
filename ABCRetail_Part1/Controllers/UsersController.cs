using ABCRetail_Part1.Models;
using ABCRetail_Part1.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ABCRetail_Part1.Controllers
{
    public class UsersController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        //constructor to initialize TableStorageService
        public UsersController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        //display all users (Index view)
        public async Task<IActionResult> Index()
        {
            var users = await _tableStorageService.GetAllUsersAsync();
            return View(users);
        }

        //display AddUser view for adding new user
        public IActionResult AddUser()
        {
            return View();
        }

        //handle AddUser form submission
        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            //check if email already exists
            var existingUsers = await _tableStorageService.GetAllUsersAsync();
            var existingUser = existingUsers.FirstOrDefault(u => u.Email == user.Email);

            if (existingUser != null)
            {
                //if email exists, add error to ModelState and return view
                ModelState.AddModelError("Email", "The email is already in use.");
                return View(user);
            }

            //****************
            //Code Attribution
            //The following coode was taken from StackOverflow:
            //Author: Pranay Rana
            //Link: https://stackoverflow.com/questions/34715501/validating-password-using-regex-c-sharp
            //****************

            //password validation
            if (!IsValidPassword(user.Password))
            {
                //if password is not valid, add error to ModelState and return view
                ModelState.AddModelError("Password", "Password must be at least 8 characters long, contain an uppercase letter, a lowercase letter, a number, and a special character.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                //set PartitionKey and RowKey for new user
                user.PartitionKey = "UsersPartition";
                user.RowKey = Guid.NewGuid().ToString();

                //hash the password before storing it
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                //assign new user ID
                int maxUserId = existingUsers.Any() ? existingUsers.Max(u => u.UserId) : 0;
                user.UserId = maxUserId + 1;

                //add new user to Table Storage
                await _tableStorageService.AddUserAsync(user);
                return RedirectToAction("Index");
            }
            
            //return view if model state is invalid
            return View(user);
        }

        //method for password validation
        private bool IsValidPassword(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;

            return true;
        }

        //delete user from the table storage
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteUserAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }

        //display details of a specific user
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var user = await _tableStorageService.GetUserAsync(partitionKey, rowKey);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        //register View
        public IActionResult Register()
        {
            return View();
        }

        //handle register Form Submission
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                user.PartitionKey = "UsersPartition";
                user.RowKey = Guid.NewGuid().ToString();
                user.Password = HashPassword(user.Password); 

                var users = await _tableStorageService.GetAllUsersAsync();
                int maxUserId = users.Any() ? users.Max(u => u.UserId) : 0;
                user.UserId = maxUserId + 1;

                //add new user to Table Storage
                await _tableStorageService.AddUserAsync(user);

                //redirect to home page after successful registration
                return RedirectToAction("Index", "Home");
            }

            //return view if model state is invalid
            return View(user);
        }

        //login View
        public IActionResult Login()
        {
            return View();
        }

        //handle login Form Submission
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var users = await _tableStorageService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email && VerifyPassword(u.Password, password));

            if (user != null)
            {
                //set authentication cookie or session (still do)

                //redirect to the appropriate page
                return RedirectToAction("Index", "Home");
            }

            //if login fails, add error to ModelState
            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        //method to hash user's password
        private string HashPassword(string password)
        {
            // Implement password hashing (to do)
            return password; 
        }

        //method to verify user's password
        private bool VerifyPassword(string hashedPassword, string password)
        {
            //implement password verification (to do)
            return hashedPassword == password; 
        }
    }
}
