using LuminaPath.Core.Entities.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Interfaces
{
    public interface IBaseModelService<TEntity> where TEntity : class, IBasicInfo
    {
        Task<TEntity> GetById(int? id, IEnumerable<string>? includes = null);
        Task<Result<int, FailedResult>> DeleteAsync(int? id);
    }
}
