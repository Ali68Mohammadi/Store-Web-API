using BestStoreApi.Models;
using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id",$"{user.Id}"),
                new Claim("role",user.Role),

            };

            string strkey = _configuration["JwtSettings:Key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strkey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issure"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

       

        //[HttpGet("TestToken")]
        //public IActionResult TestToken()
        //{
        //        User user = new User()
        //        {
        //            Id = 2,
        //            Role="admin"
        //        };
        //    var jwt= CreateJWToken(user);
        //    var reponse = new
        //    {
        //        JWToken = jwt,
        //    };
        //    return Ok (reponse);
        //}

        [HttpPost("Register")]
        public IActionResult Register(UserDto userDto)
        {
            //Check email Is exist
            if (_context.Users.Any(u => u.Email == userDto.Email))
            {
                ModelState.AddModelError("Email", "This email is already exist");
                return BadRequest(ModelState);
            }

            //encrypt the password
            var passwordHasher = new PasswordHasher<User>();
            var encryptetPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            //create new  Account
            var user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Password = encryptetPassword,
                Address = userDto.Address,
                CreatedAt = DateTime.Now,
                PhoneNumber = userDto.PhoneNumber ?? "",
                Role = "client",
            };

            _context.Add(user);
            _context.SaveChanges();
            var jwt = CreateJWToken(user);

            //create a userProfile to send to client 

            var userProfile = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                Role = user.Role,
            };
            var response = new
            {
                Token = jwt,
                User = userProfile
            };
            return Ok(response);
        }


        [HttpPost("Login")]
        public IActionResult Login(string email, string password)
        {
            //check user Eamil
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("Error", "Email or Password  not valid");
                return BadRequest(ModelState);
            }

            //check user Password
            var Passwordhasher = new PasswordHasher<User>();
            var result = Passwordhasher.VerifyHashedPassword(new User(), user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Wrong Password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);
            var userProfile = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                Role = user.Role,
            };
            var response = new
            {
                Token = jwt,
                User = userProfile
            };
            return Ok(response);

        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return NotFound();
            }
            //delete old password
            var oldpassReset = _context.ResetPasswords.FirstOrDefault(r => r.Email == email);
            if (oldpassReset != null)
            {
                _context.ResetPasswords.Remove(oldpassReset);
            }

            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var resetPassword = new ResetPassword()
            {
                CaretedAt = DateTime.Now,
                Email = email,
                Token = token
            };
            _context.Add(resetPassword);
            _context.SaveChanges();

            //todo send email to user

            return Ok(resetPassword);
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPasword(string token, string newPassword)
        {
            var resetPassword = _context.ResetPasswords.FirstOrDefault(r => r.Token == token);
            if (resetPassword == null)
            {
                ModelState.AddModelError("token", "Wrong or expired token");
                return BadRequest(ModelState);
            }
            var user = _context.Users.FirstOrDefault(u => u.Email == resetPassword.Email);
            if (user == null)
            {
                ModelState.AddModelError("token", "Wrong or expired token");
                return BadRequest(ModelState);
            }

            //encrypt the password
            var passwordHasher = new PasswordHasher<User>();
            var encryptetPassword = passwordHasher.HashPassword(new User(), newPassword);

            user.Password = encryptetPassword;

            _context.ResetPasswords.Remove(resetPassword);

            _context.SaveChanges();
            return Ok();
        }

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            //find login UserId
            var id = JwtReader.GetUserId(User);

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return Unauthorized();

            }

            UserProfileDto userProfile = new()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
            };

            return Ok(userProfile);
        }

        [Authorize]
        [HttpPut("UpdateProfile")]
        public IActionResult UpdateProfile(UserProfileUpdateDto userProfileUpdate)
        {
            var id = JwtReader.GetUserId(User);
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return Unauthorized();

            }

            user.FirstName = userProfileUpdate.FirstName;
            user.LastName = userProfileUpdate.LastName;
            user.Email = userProfileUpdate.Email;
            user.Address = userProfileUpdate.Address;
            user.PhoneNumber = userProfileUpdate.PhoneNumber ?? "";

            _context.SaveChanges();
            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                Address = user.Address,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? "",
                CreatedAt = user.CreatedAt,
                Role = user.Role,

            };
            return Ok(userProfileDto);
        }

        [Authorize]
        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword([Required,MinLength(8),MaxLength(100)]string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == JwtReader.GetUserId(User));
            if (user == null)
                return Unauthorized();

            //encrypt the password
            var passwordHasher = new PasswordHasher<User>();
            var encryptetPassword = passwordHasher.HashPassword(new User(), newPassword);
            user.Password = encryptetPassword;
            _context.SaveChanges();
            return Ok();
        }


        /*
        [Authorize]
        [HttpGet("GetTokenClaim")]
        public IActionResult GetTokenClaim()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                Dictionary<string, string> claims = new Dictionary<string, string>();
                foreach (var claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }
                return Ok(claims);
            }
            return Ok();
        }

       
        [Authorize]
        [HttpGet("AuthorizeAuthenticatedUser")]
        public IActionResult AuthorizeAuthenticatedUser()
        {

            return Ok("you are authorized");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("AuthorizeAdmin")]
        public IActionResult AuthorizeAdmin()
        {

            return Ok("you are authorized");
        }

        [Authorize(Roles = "seller,admin")]
        [HttpGet("AuthorizeAdminAndSeller")]
        public IActionResult AuthorizeAdminAndSeller()
        {

            return Ok("you are authorized");
        }
        */
    }
}
