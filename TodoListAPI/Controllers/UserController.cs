using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using TodoListAPI.Models;
using PasswordProfile = Microsoft.Graph.PasswordProfile;

namespace TodoListAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private ClaimsPrincipal _currentPrincipal;

        /// <summary>
        /// We store the object id of the user/app derived from the presented Access token
        /// </summary>
        private string _currentPrincipalId = string.Empty;
        private readonly IConfiguration _configuration;
        public UserController(TodoContext context, IHttpContextAccessor contextAccessor, IConfiguration configuration)
        {
            _contextAccessor = contextAccessor;
            _configuration = configuration;

            // We seek the details of the user/app represented by the access token presented to this API, This can be empty unless authN succeeded
            // If a user signed-in, the value will be the unique identifier of the user.
            _currentPrincipal = GetCurrentClaimsPrincipal();

            if (!IsAppOnlyToken() && _currentPrincipal != null)
            {
                // The default behavior of the JwtSecurityTokenHandler is to map inbound claim names to new values in the generated ClaimsPrincipal. 
                // The result is that "sub" claim that identifies the subject of the incoming JWT token is mapped to a claim
                // named "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier". An alternative approach is to 
                // disable this mapping by setting JwtSecurityTokenHandler.DefaultMapInboundClaims to false in Startup.cs and
                // then calling _currentPrincipal.FindFirstValue(ClaimConstants.Sub) to obtain the value of the unmapped "sub" claim.
                _currentPrincipalId = _currentPrincipal.GetNameIdentifierId(); // use "sub" claim as a unique identifier in B2C
            }
        }

        // GET: api/todolist/getAll
        [HttpGet]
        [Route("getAll")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes:Read")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll()
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            var TenantId = _configuration.GetValue<string>("AzureAdB2C:TenantId");
            var ClientId = _configuration.GetValue<string>("AzureAdB2C:ClientId");
            var ClientSecret = _configuration.GetValue<string>("AzureAdB2C:ClientSecret");

            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
            GraphServiceClient graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            var users = await graphClient.Users.Request().Select(ts => new
            {
                ts.GivenName,
                ts.Surname,
                ts.DisplayName,
                ts.UserPrincipalName,
                ts.Id,
                ts.AccountEnabled
            }).GetAsync();

            return users.ToList();
        }
        [HttpPost]
        [Route("create")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes:Write")]
        public async Task<ActionResult<dynamic>> Create(AdUser objUser)
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(_configuration.GetValue<string>("AzureAdB2C:TenantId"), _configuration.GetValue<string>("AzureAdB2C:ClientId"), _configuration.GetValue<string>("AzureAdB2C:ClientSecret"));
            GraphServiceClient graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var user = new User
            {
                AccountEnabled = objUser.accountEnabled,
                DisplayName = objUser.displayName,
                MailNickname = objUser.mailNickname,
                UserPrincipalName = objUser.userPrincipalName,
                GivenName = objUser.givenName,
                Surname = objUser.surname,
                UsageLocation = objUser.usageLocation,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = objUser.passwordProfile.forceChangePasswordNextSignIn,
                    Password = objUser.passwordProfile.password,
                }
            };

            var users = await graphClient.Users.Request().AddAsync(user);

            return users;
        }
        [HttpPost]
        [Route("invite")]
        //[RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes:Write")]
        public async Task<ActionResult<dynamic>> Invite(UserInvitation invitee)
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(_configuration.GetValue<string>("AzureAdB2C:TenantId"), _configuration.GetValue<string>("AzureAdB2C:ClientId"), _configuration.GetValue<string>("AzureAdB2C:ClientSecret"));
            GraphServiceClient graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var user = new User
            {
                AccountEnabled = invitee.InvitedUser.accountEnabled,
                DisplayName = invitee.InvitedUser.displayName,
                MailNickname = invitee.InvitedUser.mailNickname,
                UserPrincipalName = invitee.InvitedUser.userPrincipalName.Replace('@','_')+"Ext#@covrizeb2c.onmicrosoft.com",
                GivenName = invitee.InvitedUser.givenName,
                Surname = invitee.InvitedUser.surname,
                UsageLocation = invitee.InvitedUser.usageLocation,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = invitee.InvitedUser.passwordProfile.forceChangePasswordNextSignIn,
                    Password = invitee.InvitedUser.passwordProfile.password,
                }
            };

            var invitation = new Invitation
            {
                InvitedUserEmailAddress = invitee.InvitedUserEmailAddress,
                InviteRedirectUrl = "https://account.activedirectory.windowsazure.com/?tenantid=acce8b0b-f2b9-4df5-b996-adc3424d0f40&login_hint="+ invitee.InvitedUserEmailAddress,
                SendInvitationMessage= invitee.SendInvitationMessage,
                InvitedUserDisplayName= invitee.InvitedUserDisplayName,
                InvitedUserType=invitee.InvitedUserType,
                InvitedUser= user,
                Status= invitee.Status

            };


            var users = await graphClient.Invitations.Request().AddAsync(invitation);

            return Ok(users);
        }



        [HttpPatch]
        [Route("update")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes:Write")]
        public async Task<ActionResult<dynamic>> Update(AdUser objUser)
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(_configuration.GetValue<string>("AzureAdB2C:TenantId"), _configuration.GetValue<string>("AzureAdB2C:ClientId"), _configuration.GetValue<string>("AzureAdB2C:ClientSecret"));
            GraphServiceClient graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var user = new User
            {
                DisplayName = objUser.displayName,
            };

            //var user = new User
            //{

            //    PasswordProfile = new PasswordProfile
            //    {
            //        ForceChangePasswordNextSignIn = objUser.passwordProfile.forceChangePasswordNextSignIn,
            //        Password = objUser.passwordProfile.password,
            //    }
            //};
            //var user = new User
            //{
            //    AccountEnabled = objUser.accountEnabled,
            //    DisplayName = objUser.displayName,
            //    MailNickname = objUser.mailNickname,
            //    UserPrincipalName = objUser.userPrincipalName,
            //    GivenName = objUser.givenName,
            //    Surname = objUser.surname,
            //    UsageLocation = objUser.usageLocation,
            //    PasswordProfile = new PasswordProfile
            //    {
            //        ForceChangePasswordNextSignIn = objUser.passwordProfile.forceChangePasswordNextSignIn,
            //        Password = objUser.passwordProfile.password,
            //    }
            //};


            var users = await graphClient.Users[objUser.id.ToString()].Request().UpdateAsync(user);

            return Ok();
        }

        [HttpDelete]
        [Route("delete")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes:Write")]
        public async Task<ActionResult<dynamic>> Delete(Guid objUser)
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(_configuration.GetValue<string>("AzureAdB2C:TenantId"), _configuration.GetValue<string>("AzureAdB2C:ClientId"), _configuration.GetValue<string>("AzureAdB2C:ClientSecret"));
            GraphServiceClient graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            await graphClient.Users[objUser.ToString()].Request().DeleteAsync();

            return Ok();
        }


        /// <summary>
        //  / returns the current claimsPrincipal (user/Client app) dehydrated from the Access token
        /// </summary>
        /// <returns></returns>
        private ClaimsPrincipal GetCurrentClaimsPrincipal()
        {
            // Irrespective of whether a user signs in or not, the AspNet security middleware dehydrates 
            // the claims in the HttpContext.User.Claims collection
            if (_contextAccessor.HttpContext != null && _contextAccessor.HttpContext.User != null)
            {
                return _contextAccessor.HttpContext.User;
            }

            return null;
        }

        /// <summary>
        /// Indicates of the AT presented was for an app-only token or not.
        /// </summary>
        /// <returns></returns>
        private bool IsAppOnlyToken()
        {
            // Add in the optional 'idtyp' claim to check if the access token is coming from an application or user.
            //
            // See: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-optional-claims

            if (GetCurrentClaimsPrincipal() != null)
            {
                return GetCurrentClaimsPrincipal().Claims.Any(c => c.Type == "idtyp" && c.Value == "app");
            }

            return false;
        }
    }
}
