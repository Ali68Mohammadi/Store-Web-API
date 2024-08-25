using BestStoreApi.Models;
using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("subjects")]
        public IActionResult GetSubjects()
        {

            return Ok(_context.Subjects.ToList());
        }

        [Authorize(Roles ="admin")]
        [HttpGet]
        public IActionResult GetContacts(int? page)
        {
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            decimal totalpages = 0;
            decimal count = _context.Contacts.Count();
            totalpages = (int)Math.Ceiling(count / pageSize);

            var contacts = _context.Contacts
                .Include(c => c.Subject)
                .OrderByDescending(c => c.Id)
                .Skip((int)(page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var responce = new
            {
                Contacts = contacts,
                TotalPages = totalpages,
                Page = page,
                PageSize = pageSize,
            };


            return Ok(responce);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public IActionResult GetContact(int id)
        {

            var contact = _context.Contacts.Include(c => c.Subject)
                .FirstOrDefault(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }
            return Ok(contact);
        }


        [HttpPost]
        public IActionResult CreateContact(ContactDto contactDto)
        {
            var subject = _context.Subjects.Find(contactDto.SubjectId);
            if (subject == null)
            {
                ModelState.AddModelError("Subject", "plaese select valid subject ");
                return BadRequest(ModelState);
            };

            Contact contact = new()
            {
                Message = contactDto.Message,
                Email = contactDto.Email,
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                PhoneNumber = contactDto.PhoneNumber ?? "",
                Subject = subject,
                CreatedAt = DateTime.Now,
            };

            _context.Contacts.Add(contact);
            _context.SaveChanges();
            return Ok(contact);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteContact(int id)
        {
            //           //Method1
            //    var contact = _context.Contacts.Find(id);
            //    if(contact == null)
            //    { return NotFound();
            //        }

            //   _context.Contacts.Remove(contact);   
            //    _context.SaveChanges(); 
            //    return Ok(contact);

            //Method2
            try
            {
                Contact contact = new() { Id = id, Subject = new Subject() };
                _context.Contacts.Remove(contact);
                _context.SaveChanges();

            }
            catch (Exception)
            {
                NotFound();

            }
            return Ok();
        }

    /*
        [HttpPut("{id}")]
        public IActionResult UpdateContact(int id, ContactDto contactDto)
        {
            var subject = _context.Subjects.Find(contactDto.SubjectId);
            if (subject == null)
            {
                ModelState.AddModelError("Subject", "plaese select valid subject ");
                return BadRequest(ModelState);
            };

            var contact = _context.Contacts.Find(id);
            if (contact == null)
            {
                return NotFound();
            }
            contact.FirstName = contactDto.FirstName;
            contact.LastName = contactDto.LastName;
            contact.Message = contactDto.Message;
            contact.Email = contactDto.Email;
            contact.PhoneNumber = contactDto.PhoneNumber ?? "";
            contact.Subject = subject;

            _context.SaveChanges();
            return Ok(contact);
            }
    */
    }
}
