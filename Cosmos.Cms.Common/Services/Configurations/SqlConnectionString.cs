using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Connection string information
    /// </summary>
    public class SqlConnectionString
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SqlConnectionString()
        {
        }
        /// <summary>
        /// Constructor that deconstructs a connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cloudName"></param>
        /// <param name="isPrimary"></param>
        public SqlConnectionString(string connectionString, string cloudName, bool isPrimary)
        {
            CloudName = cloudName;
            IsPrimary = isPrimary;
            var builder = new SqlConnectionStringBuilder(connectionString);
            Hostname = builder.DataSource;
            InitialCatalog = builder.InitialCatalog;
            UserId = builder.UserID;
            Password = builder.Password;
        }

        /// <summary>
        ///     Cloud provider
        /// </summary>
        [Required]
        [UIHint("CloudProvider")]
        [Display(Name = "Cloud")]
        public string CloudName { get; set; }

        /// <summary>
        ///     Is the primary database
        /// </summary>
        [Required]
        [Display(Name = "Is Primary")]
        public bool IsPrimary { get; set; }

        /// <summary>
        ///     FQDN of SQL Server
        /// </summary>
        [Required]
        [Display(Name = "Hostname")]
        public string Hostname { get; set; }

        /// <summary>
        ///     Database name
        /// </summary>
        [Required]
        [Display(Name = "Database Name")]
        public string InitialCatalog { get; set; }

        /// <summary>
        ///     User Id used to connect to the database
        /// </summary>
        [Required]
        [Display(Name = "User Id")]
        public string UserId { get; set; }

        /// <summary>
        ///     Password to connect to the database
        /// </summary>
        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        ///     Uses <see cref="SqlConnectionStringBuilder" /> to generate connection string for this object.
        /// </summary>
        /// <returns>SQL connection string</returns>
        public override string ToString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Hostname,
                InitialCatalog = InitialCatalog,
                PersistSecurityInfo = true,
                UserID = UserId,
                Password = Password,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };
            return builder.ToString();
        }
    }
}