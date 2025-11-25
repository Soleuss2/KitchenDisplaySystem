using BCrypt.Net;
using SelfOrderingSystemKiosk.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SelfOrderingSystemKiosk.Areas.Admin.Models;

namespace SelfOrderingSystemKiosk.Services
{
    public class UserService
    {
        private readonly IMongoCollection<AdminUser> _users;

        public UserService(IMongoDatabase authDatabase)
        {
            _users = authDatabase.GetCollection<AdminUser>("Users");
        }

        public async Task<AdminUser?> ValidateUserAsync(string username, string password)
        {
            var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (user == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            return isPasswordValid ? user : null;
        }

        public async Task CreateAdminAsync(AdminUser newUser)
        {
            await _users.InsertOneAsync(newUser);
        }


        
        public async Task CreateUserAsync(AdminUser user)
        {
            await _users.InsertOneAsync(user);
        }

        
        public async Task<AdminUser?> GetUserByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        
        public async Task<AdminUser?> GetUserByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        //  Validate login 
        public async Task<AdminUser?> ValidateLoginAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
                return null;

            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            return passwordValid ? user : null;
        }

       
        public async Task<long> GetUserCountAsync()
        {
            return await _users.CountDocumentsAsync(FilterDefinition<AdminUser>.Empty);
        }
    }
}
