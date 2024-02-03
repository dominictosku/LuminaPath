using AutoMapper;
using Domain.Entities;
using Domain.Models;
using Domain.Models.Gaming;
using Infrastructure.Interfaces.Repositories;
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

		public async Task<Result<Dto, FailedResult>> PostAsync<Dto>(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;

			return await base.PostAsync<Dto>(viewModel);
		}

		public async Task<Result<Dto, FailedResult>> PutAsync<Dto>(MyGame viewModel, LuminaUser? user)
		{
			if (user == null)
				return new FailedResult("User not found, please login");
			if (IsMediaAlreadyAdded(viewModel.MediaId, viewModel.Id, user.Id))
			{
				return new FailedResult("This is game already added");
			}
			viewModel.LuminaUserId = user.Id;
			return await base.PutAsync<Dto>(viewModel);
		}

		protected bool IsMediaAlreadyAdded(int id, int myId, string userId)
		{
			var entities = _repository.GetAllNoTrack();
			return entities.Any(e => e.MediaId == id && e.Id != myId && e.LuminaUserId == userId);
		}

	}
}
