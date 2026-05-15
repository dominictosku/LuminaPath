using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers.Base
{
    public abstract class UserMediaController<TUserMedia, TUserMediaDto>(
        GenericMyModelService<TUserMedia> service,
        UserManager<LuminaUser> userManager,
        IObjectMapper mapper)
        : GenericMyController<TUserMedia, TUserMediaDto>(service, userManager, mapper)
        where TUserMedia : MyMedia, IMyMedia
        where TUserMediaDto : class, IBasicInfo
    {
        [HttpPut("{id}")]
        public override Task<IActionResult> PutAsync(int id, TUserMediaDto viewModel)
        {
            PrepareForUpdate(viewModel);
            return base.PutAsync(id, viewModel);
        }

        protected virtual void PrepareForUpdate(TUserMediaDto viewModel)
        {
        }
    }
}
