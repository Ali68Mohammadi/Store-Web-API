using BestStoreApi.Models;
using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BestStoreApi.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetUsers(int? page)
        {
            #region paging

            if (page == null || page < 1)
            {
                page = 1;
            }

            int pagesize = 5;
            int totalPage = 0;

            decimal userCount = _context.Users.Count();
            totalPage = (int)Math.Ceiling(userCount / pagesize);

            var users = _context.Users
                .OrderByDescending(u => u.Id)
                .Skip((int)(page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            #endregion


            List<UserProfileDto> userProfileDtos = new List<UserProfileDto>();

            foreach (var user in users)
            {
                userProfileDtos.Add(new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Address = user.Address,
                    CreatedAt = user.CreatedAt,
                    Role = user.Role,
                    PhoneNumber = user.PhoneNumber,
                });

            }
            var response = new
            {
                Users= userProfileDtos,
                TotalPage = totalPage,
                Pagesize = pagesize,
                Page=page,
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            UserProfileDto userProfileDtos = new() 
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
            };

            return Ok(userProfileDtos);
        }
    }
}
