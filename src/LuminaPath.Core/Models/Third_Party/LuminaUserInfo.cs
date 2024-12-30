using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LuminaPath.Core.Models.Third_Party
{
    public class LuminaUserInfo
    {
        public int Id { get; set; }
        public LuminaUser User { get; set; }
        public string UserId { get; set; }

        public string PSNOnlineId { get; set; }

        public string AccountId { get; set; }
    }
}
