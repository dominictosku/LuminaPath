using AutoMapper;
using LuminaPath.Core.Common.Features.User.Dto;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Infrastructure.Services
{
    public class LuminaUserService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly UserManager<LuminaUser> _userManager;
        public LuminaUserService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, UserManager<LuminaUser> userManager)
        {
            _dbContextFactory = dbContextFactory;
            _userManager = userManager;
        }

        protected async Task<LuminaPathDbContext> GetDbContextAsync()
        {
            return await _dbContextFactory.CreateDbContextAsync();
        }

        public async Task<(List<LuminaUser> items, int total)> GetPaginatedUsers(string searchString = "", int skip = 0, int take = 10)
        {
            Expression<Func<LuminaUser, bool>> searchPredicate = x =>
                x.UserName!.ToLower().Contains(searchString) ||
                x.Email!.ToLower().Contains(searchString);
            var query = _userManager.Users.Where(searchPredicate);

            var items = await query
                .OrderBy(u => u.UserName)
                .Skip(skip).Take(take).ToListAsync();
            var total = _userManager.Users.Count(searchPredicate);
            return (items, total);
        }

        public async Task<LuminaUser?> GetUser(string id)
        {
            using var context = await GetDbContextAsync();
            return await context.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IdentityResult?> CreateUser(UserDto model) 
        {
            var applicationUser = new LuminaUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                LockoutEnabled = model.LockoutEnabled
            };
            var password = model.Password;
            return await _userManager.CreateAsync(applicationUser, password!);
        }

        public async Task<IdentityResult> UpdateUser(UserDto model) 
        {
            var user = await _userManager.FindByIdAsync(model.Id!) ?? throw new Exception($"The application user [{model.Id}] was not found.");
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.UserName;
            user.LockoutEnabled = model.LockoutEnabled;
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> UpdateThirdParty(string userId, LuminaUserInfo model)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception($"The application user [{userId}] was not found.");
            user.LuminaUserInfo = model;
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> SetUserActive(string userId, bool active)
        {
            var user = await _userManager.FindByIdAsync(userId!) ?? throw new Exception($"Application user not found {userId}.");
            user.LockoutEnabled = active;
            return await _userManager.UpdateAsync(user);
        }
    }
}
