using BusinessCardManager.Models;
using BusinessCardManager.Models.DTO;

namespace BusinessCardManager.Repositories.Interfaces
{
    public interface IBusinessCard
    {
        Task<IEnumerable<BusinessCardResDto>> GetAllAsync(string name = null);
        Task<BusinessCardResDto> GetByIdAsync(int id);
        Task <string> AddBusinessCardAsync(BusinessCardReqDto card);
        Task  DeleteAsync(int id);
        Task UpdateAsync(int id,BusinessCardReqDto card);

        Task<string> ExportToXmlAsync();
        Task<string> ExportToCsvAsync();
        Task<string> CreateDatabaseBackupAsync(string backupDirectory);
    }
}
