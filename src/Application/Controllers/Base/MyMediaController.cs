using Application.Controllers.Base.Generic;
using AutoMapper;
using Domain.Common.Interfaces;
using Domain.Models;
using Domain.Models.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers.Base
{
	[Authorize]
	public abstract class MyMediaController<TEntity, TEntityDto> : GenericController<TEntity, TEntityDto> where TEntity : MyMedia, IMyMedia
	{
		private readonly UserManager<LuminaUser> _userManager;
		public MyMediaController(IGenericRepository<TEntity> service, IMapper mapper, UserManager<LuminaUser> userManager) : base(service, mapper)
		{
			_userManager = userManager;
			Mapper = mapper;
		}

		[HttpPost]
		public override async Task<ActionResult<TEntityDto>> PostAsync(TEntity viewModel)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, userId))
			{
				return BadRequest("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PostAsync(viewModel);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> PutAsync(int id, TEntity viewModel)
		{
			var (user, userId) = await GetUserAndUserIdAsync(_userManager);
			if (user == null)
				return NotFound("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, userId))
			{
				return BadRequest("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync(id, viewModel);
		}

		protected bool IsMediaAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _service.GetAllNoTrack();
			return entities.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
		}
	}
}
