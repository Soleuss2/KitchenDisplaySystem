using SelfOrderingSystemKiosk.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SelfOrderingSystemKiosk.Areas.Admin.Models
{
    public class  AuthenticationSettings
    {
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
    }

    public class DataConSettings
    {
        public string ConnectionString { get; set; }
    }

}
