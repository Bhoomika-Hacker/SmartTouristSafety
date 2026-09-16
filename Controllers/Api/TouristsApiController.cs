using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Data;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/tourists")]
    public class TouristsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TouristsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/tourists
        [HttpGet]
        public ActionResult<IEnumerable<Tourist>> GetAll()
        {
            return Ok(_db.Tourists.Include(t => t.DigitalIdentity).ToList());
        }

        // GET api/tourists/5
        [HttpGet("{id:int}")]
        public ActionResult<Tourist> GetById(int id)
        {
            var tourist = _db.Tourists
                .Include(t => t.DigitalIdentity)
                .Include(t => t.LocationLogs.OrderByDescending(l => l.Timestamp).Take(10))
                .FirstOrDefault(t => t.Id == id);

            if (tourist is null) return NotFound();
            return Ok(tourist);
        }

        // POST api/tourists
        [HttpPost]
        public ActionResult<Tourist> Create(Tourist tourist)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _db.Tourists.Add(tourist);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = tourist.Id }, tourist);
        }

        // PUT api/tourists/5
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, Tourist model)
        {
            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();

            tourist.FullName = model.FullName;
            tourist.Phone = model.Phone;
            tourist.Email = model.Email;
            tourist.EmergencyContactName = model.EmergencyContactName;
            tourist.EmergencyContactPhone = model.EmergencyContactPhone;
            tourist.IsActive = model.IsActive;

            _db.SaveChanges();
            return NoContent();
        }

        // DELETE api/tourists/5
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var tourist = _db.Tourists.Find(id);
            if (tourist is null) return NotFound();

            _db.Tourists.Remove(tourist);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
