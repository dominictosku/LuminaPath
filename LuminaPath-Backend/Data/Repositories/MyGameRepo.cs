using Data.Classes;
using Data.Models;
using Data.Models.Dto;
using Data.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
	public class MyGameRepo : GenericRepo<MyGame>
	{
		public MyGameRepo(LuminaPathDbContext context): base(context) 
		{
		}
	}
}
