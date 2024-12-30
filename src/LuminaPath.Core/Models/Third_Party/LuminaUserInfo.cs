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

        #region PSN
        public string PSNOnlineId { get; set; }

        public string PSNAccountId { get; set; }

        public int PSNTrophyLevel { get; set; }

        public int PSNBronze { get; set; }

        public int PSNSilver { get; set; }

        public int PSNGold { get; set; }

        public int PSNPlatinum { get; set; }
        #endregion
    }
}
