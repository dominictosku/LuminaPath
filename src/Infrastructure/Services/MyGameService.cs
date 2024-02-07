using Application.Common.Interfaces.Repositories;
using AutoMapper;
using Domain.Common.Entities.Results;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class MyGameService : GenericModelService<MyGame>
	{
		protected readonly new IMyGameRepository _repository;
		public MyGameService(IMyGameRepository repo, IMapper mapper) : base(repo, mapper)
		{
			_repository = repo;
		}

		public async Task<Result<MyGame, FailedResult>> PostAsync(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;

			return await base.PostAsync(viewModel);
		}

		public async Task<Result<MyGame, FailedResult>> PutAsync(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync(viewModel);
		}

		protected bool IsMediaAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _repository.GetAllNoTrack();
			return entities.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
		}

	}
}
