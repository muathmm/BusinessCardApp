using BusinessCardManager.Models.DTO;
using BusinessCardManager.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BusinessCardManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessCardsController : ControllerBase
    {
        private readonly IBusinessCard _service;

        public BusinessCardsController(IBusinessCard service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll(string name = null)
        {
            // Retrieve all business cards, optionally filtered by name
            var cards = await _service.GetAllAsync(name);
            return Ok(cards);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Retrieves a business card by its ID. 
            // Returns NotFound if the card does not exist; otherwise, returns the card data.

            var card = await _service.GetByIdAsync(id);
            if (card == null)
                return NotFound();
            return Ok(card);
        }



        [HttpPost]
       
        public async Task<IActionResult> Create([FromForm] BusinessCardReqDto cardDto)
        {
            // Validates the model and creates a new business card.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.AddBusinessCardAsync(cardDto);

            if (result == "ok")
                return Ok(new { Message = "Add card Successfully" }); 

            return BadRequest(new { Message = "Error occurred while adding the business card" });
        }




        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BusinessCardReqDto cardDto)
        {
            // Updates the business card with the specified id using the provided cardDto.
            // Returns NoContent if the update is successful.
            await _service.UpdateAsync(id, cardDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Deletes the business card with the specified id.
            // Returns NoContent if the deletion is successful.
            await _service.DeleteAsync(id);
            return NoContent();
        }


        [HttpGet("export/xml")]
        public async Task<IActionResult> ExportToXml()
        {
            // Exports the business cards data to XML format and returns it as a file.
            var xmlData = await _service.ExportToXmlAsync();
            var bytes = Encoding.UTF8.GetBytes(xmlData);
            return File(bytes, "application/xml", "BusinessCards.xml");
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv()
        {
            // Exports the business cards data to CSV format and returns it as a file.
            var csvData = await _service.ExportToCsvAsync();
            var bytes = Encoding.UTF8.GetBytes(csvData);
            return File(bytes, "text/csv", "BusinessCards.csv");
        }

        // Endpoint for exporting the database dump
        [HttpGet("export/dbbackup")]
        public async Task<IActionResult> ExportDatabaseBackup()
        {
            // Define the directory where the backup will be stored
            string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

            // Call the service to create the database backup
            string backupFilePath = await _service.CreateDatabaseBackupAsync(backupDirectory);

            // Return the backup file as a download
            var fileBytes = await System.IO.File.ReadAllBytesAsync(backupFilePath);
            return File(fileBytes, "application/octet-stream", Path.GetFileName(backupFilePath));
        }

    }
}
