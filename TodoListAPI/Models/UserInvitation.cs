using Microsoft.Graph;
using System.Text.Json.Serialization;

namespace TodoListAPI.Models
{
    public class UserInvitation
    {


            public string InvitedUserDisplayName { get; set; }

            public string InvitedUserEmailAddress { get; set; }

            
             public string InvitedUserType { get; set; }

            public string InviteRedeemUrl { get; set; }

            
            public string InviteRedirectUrl { get; set; }

            
            public bool? ResetRedemption { get; set; }

            
            public bool? SendInvitationMessage { get; set; }

            
            public string Status { get; set; }

          
            public AdUser InvitedUser { get; set; }

        

    }
}
