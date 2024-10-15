
using BusinessCardManager.data;
using BusinessCardManager.Models;
using BusinessCardManager.Models.DTO;
using BusinessCardManager.Repositories.Interfaces;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BusinessCardManager.Repositories.Services
{
    public class BusinessCardService : IBusinessCard
    {


        private readonly BusinessCardDbContext _context;
        private readonly string _connectionString;
        public BusinessCardService(BusinessCardDbContext context, string connectionString)
        {
            _context = context;
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<BusinessCardResDto>> GetAllAsync(string name = null)
        {
            // Start with an editable query
            var query = _context.BusinessCards.AsQueryable();

            // If a name is provided, add the filter condition directly
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(card => card.Name.Contains(name));
            }

            //Retrieve business cards from the database (only results that match the filter will be included)
            var cards = await query.ToListAsync();

            //Convert entities into DTOs
            return cards.Select(card =>
            {
                //Verify that PhotoBase64 is encoded correctly
                var photoBase64 = string.IsNullOrEmpty(card.PhotoBase64) || !IsBase64String(card.PhotoBase64)
                    ? "defaultBase64String" //Use a default value if it is invalid
                    : card.PhotoBase64; //Set the correct value

                return new BusinessCardResDto
                {
                    Id = card.Id,
                    Name = card.Name,
                    Gender = card.Gender,
                    DateOfBirth = card.DateOfBirth,
                    Email = card.Email,
                    Phone = card.Phone,
                    Address = card.Address,
                    PhotoBase64 = photoBase64 
                };
            }).ToList();
        }

        //Get  The Card Using depend on Id
        public async Task<BusinessCardResDto> GetByIdAsync(int id)
        {
            var card = await _context.BusinessCards.FindAsync(id);
            if (card == null) return null;

            return new BusinessCardResDto
            {
                Id = card.Id,
                Name = card.Name,
                Gender = card.Gender,
                DateOfBirth = card.DateOfBirth,
                Email = card.Email,
                Phone = card.Phone,
                Address = card.Address,
                PhotoBase64 = card.PhotoBase64
            };
        }
        // Add New  Business Card
        public async Task<string> AddBusinessCardAsync(BusinessCardReqDto cardDto)
        {
            //Check if the entry is a XML file 
            if (cardDto.XmlFile != null)
            {
                using (var stream = new StreamReader(cardDto.XmlFile.OpenReadStream()))
                {
                    var xmlData = await stream.ReadToEndAsync();
                    var cardsFromXml = ParseXmlData(xmlData);
                    if (cardsFromXml == null) {
                        return null;
                    
                    }
                    foreach (var card in cardsFromXml)
                    {
                        ValidatePhoto(card.PhotoBase64); //Check of  image is encoded using Base 64
                        var newCard = MapDtoToEntity(card);
                        await _context.BusinessCards.AddAsync(newCard);
                        

                    }
                }
            
            }

            //Check if the entry is a Csv file 
            else if (cardDto.CsvFile != null)
            {
                using (var stream = new StreamReader(cardDto.CsvFile.OpenReadStream()))
                {
                    var csvData = await stream.ReadToEndAsync();
                    var cardsFromCsv = ParseCsvData(csvData);
                    if (cardsFromCsv == null)
                    {
                       
                        return null;
                    }
                    foreach (var card in cardsFromCsv)
                    {
                      
                        ValidatePhoto(card.PhotoBase64); ////Check of  image is encoded using Base 64
                        var newCard = MapDtoToEntity(card);
                      
                        Console.WriteLine(card.Name);
                        await _context.BusinessCards.AddAsync(newCard);
                    }
                }
              
            }

            // procssing QR code
            else if (!string.IsNullOrEmpty(cardDto.QrCodeData)) 
            {
                var cardFromQr = ParseQrCodeData(cardDto.QrCodeData);
                
                if (cardFromQr != null)
                {
                    ValidatePhoto(cardFromQr.PhotoBase64); // Check of  image is encoded using Base 64
                    var newCard = MapDtoToEntity(cardFromQr);
                    await _context.BusinessCards.AddAsync(newCard);
              
                }
                else
                {
                    return null;
                }
            }
            else
            {
                ValidatePhoto(cardDto.PhotoBase64); //Check of  image is encoded using Base 64

                //create a new card instance
                var newCard = MapDtoToEntity(cardDto);
                if (newCard == null)
                {
                    return null;
                }


                await _context.BusinessCards.AddAsync(newCard);
              
            }
            // save  the changes on database
            await _context.SaveChangesAsync();
     
            return "ok";
        }

        public async Task DeleteAsync(int id)
        {
            var card = await _context.BusinessCards.FindAsync(id);
            if (card != null)
            {
                _context.BusinessCards.Remove(card);
                await _context.SaveChangesAsync();
            }
        }

        // update Card information  
        public async Task UpdateAsync(int id, BusinessCardReqDto cardDto)
        {
            var card = await _context.BusinessCards.FindAsync(id);
            if (card != null)
            {
                card.Name = cardDto.Name;
                card.Gender = cardDto.Gender;
                card.DateOfBirth = cardDto.DateOfBirth ?? DateTime.MinValue;
                card.Email = cardDto.Email;
                card.Phone = cardDto.Phone;
                card.Address = cardDto.Address;
                card.PhotoBase64 = cardDto.PhotoBase64;

                _context.BusinessCards.Update(card);
                await _context.SaveChangesAsync();
            }

        }
        // export  all cards as Xml File
        public async Task<string> ExportToXmlAsync()
        {
            var cards = await _context.BusinessCards.ToListAsync();

            var xmlSerializer = new XmlSerializer(typeof(List<BusinessCard>));
            using var stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, cards);

            return stringWriter.ToString();
        }

        // export  all cards as Csv File
        public async Task<string> ExportToCsvAsync()
        {
            var cards = await _context.BusinessCards.ToListAsync();

            using var stringWriter = new StringWriter();
            using var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture);

            csvWriter.WriteRecords(cards);

            return stringWriter.ToString();
        }


        // Method to create a database backup (dump)
        public async Task<string> CreateDatabaseBackupAsync(string backupDirectory)
        {
            try
            {
                string databaseName = _context.Database.GetDbConnection().Database;
                string backupFileName = $"{databaseName}_Backup_{DateTime.Now:yyyyMMddHHmmss}.bak";
                string backupFilePath = Path.Combine(backupDirectory, backupFileName);

                // Command to create backup
                string backupQuery = $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupFilePath}'";

                using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Return the actual file path, not just a success message
                return backupFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating backup: {ex.Message}");
            }
        }





        // This method takes an XML string as input, parses it, and returns a list of BusinessCardReqDto objects.
        private List<BusinessCardReqDto> ParseXmlData(string xmlData)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData); // Load the data from the XML string

            // Parse the nodes and convert them into BusinessCardReqDto objects
            var cards = new List<BusinessCardReqDto>();

            // Assume that you have a root node named BusinessCards
            var cardNodes = xmlDoc.SelectNodes("/BusinessCards/BusinessCard");

            // Process each node (BusinessCard)
            foreach (XmlNode cardNode in cardNodes)
            {
                var card = new BusinessCardReqDto
                {
                    Name = cardNode.SelectSingleNode("ame")?.InnerText,
                    Gender = cardNode.SelectSingleNode("gender")?.InnerText,
                    DateOfBirth = DateTime.Parse(cardNode.SelectSingleNode("DateOfBirth")?.InnerText),
                    Email = cardNode.SelectSingleNode("email")?.InnerText,
                    Phone = cardNode.SelectSingleNode("phone")?.InnerText,
                    PhotoBase64 = cardNode.SelectSingleNode("PhotoBase64")?.InnerText,
                    Address = cardNode.SelectSingleNode("address")?.InnerText,

                };

                // Add the card to the list
                cards.Add(card);
            }

            return cards; // Return the list of business cards
        }


        // Parses CSV data into a list of BusinessCardReqDto objects.
        private List<BusinessCardReqDto> ParseCsvData(string csvData)
        {
            var cards = new List<BusinessCardReqDto>();
            var lines = csvData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip the first line if it contains headers
            {
                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length == 6) // Ensure the number of columns matches
                    {
                        // Attempt to parse the date
                        if (DateTime.TryParse(values[2].Trim(), out var dateOfBirth))
                        {
                            var card = new BusinessCardReqDto
                            {
                                Name = values[0].Trim(),
                                Gender = values[1].Trim(),
                                DateOfBirth = dateOfBirth,
                                Email = values[3].Trim(),
                                Phone = values[4].Trim(),
                                PhotoBase64 = values[5].Trim(),
                                Address = values[6].Trim(),
                            };

                            cards.Add(card);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format in line: {line}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid number of columns in line: {line} (Expected: 7, Actual: {values.Length})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing line: {line}. Error: {ex.Message}");
                }
            }

            return cards; // Return the list of business cards
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes; // Toggle inQuotes
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            //Add the final Value
            if (currentValue.Length > 0)
            {
                values.Add(currentValue.ToString().Trim());
            }

            return values.ToArray();
        }


        private BusinessCardReqDto ParseQrCodeData(string qrCodeData)
        {
            // Attempt to parse the data from JSON
            var cardData = JsonConvert.DeserializeObject<BusinessCardReqDto>(qrCodeData);

            // Check if the data was parsed successfully
            if (cardData != null)
            {
                return new BusinessCardReqDto
                {
                    Name = cardData.Name,
                    Gender = cardData.Gender,
                    DateOfBirth = cardData.DateOfBirth,
                    Email = cardData.Email,
                    Phone = cardData.Phone,
                    PhotoBase64 = cardData.PhotoBase64,
                    Address = cardData.Address
                };
            }

            // In case of failure to parse, you can return a null value or handle the error as needed
            return null;
        }


        // Parses QR code data in JSON format into a BusinessCardReqDto object.
        private BusinessCard MapDtoToEntity(BusinessCardReqDto dto)
        {
            return new BusinessCard
            {
        

                Name = dto.Name,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth ?? DateTime.MinValue,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PhotoBase64 = !string.IsNullOrEmpty(dto.PhotoBase64) ? dto.PhotoBase64.Substring(dto.PhotoBase64.IndexOf(",") + 1) : dto.PhotoBase64
            };
            }
        // Function to validate the photo
        private void ValidatePhoto(string? photoBase64)
        {
            if (!string.IsNullOrEmpty(photoBase64))
            {
                // Remove the header if it exists
                if (photoBase64.StartsWith("data:image/"))
                {
                    photoBase64 = photoBase64.Substring(photoBase64.IndexOf(",") + 1);
                }

                // Convert the image from Base64 to a byte array
                byte[] imageBytes = Convert.FromBase64String(photoBase64);

                // Check that the image size does not exceed 1MB
                if (imageBytes.Length > 1_000_000)
                {
                    throw new ArgumentException("Photo exceeds the maximum allowed size of 1MB.");
                }
            }
        }


        // Function to check if the string represents a valid Base64 value
        private bool IsBase64String(string s)
        {
            // Check if the string contains invalid characters
            if (s.Length % 4 != 0 || s.Any(c => !char.IsLetterOrDigit(c) && c != '+' && c != '/' && c != '='))
            {
                return false;
            }

            // Attempt to convert; if the conversion fails, it is not a valid Base64 string
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

    
    }
}
