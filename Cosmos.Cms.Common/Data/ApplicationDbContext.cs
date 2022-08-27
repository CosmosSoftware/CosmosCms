using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Database Context for Cosmos CMS
    /// </summary>
    public class ApplicationDbContext : AspNetCore.Identity.CosmosDb.CosmosIdentityDbContext<IdentityUser, IdentityRole>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="options"></param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsConfigured()
        {
            return await base.Database.CanConnectAsync();
        }

        #region OVERRIDES

        /// <summary>
        ///     On model creating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //
            // The following two converters ensure UTC date times are 
            // going into the database, and they are retried with
            // DateTime.Kind set to UTC
            // More information:
            // * https://stackoverflow.com/questions/4648540/entity-framework-datetime-and-utc
            // * https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=data-annotations

            modelBuilder.HasDefaultContainer("CosmosCms");

            modelBuilder.Entity<Article>()
                .ToContainer(nameof(Article))
                .HasPartitionKey(article => article.UrlPath)
                .HasKey(article => article.Id);


            modelBuilder.Entity<ArticleLog>()
                .ToContainer(nameof(ArticleLog))
                .HasPartitionKey(log => log.ArticleId)
                .HasKey(log => log.Id);

            modelBuilder.Entity<NodeScript>()
                .ToContainer(nameof(NodeScript))
                .HasPartitionKey(node => node.EndPoint)
                .HasKey(node => node.Id);

            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region DbContext

        /// <summary>
        ///     Articles
        /// </summary>
        public DbSet<Article> Articles { get; set; }

        /// <summary>
        /// Catalog of Articles
        /// </summary>
        public DbSet<CatalogEntry> ArticleCatalog { get; set; }

        /// <summary>
        /// Article locks
        /// </summary>
        public DbSet<ArticleLock> ArticleLocks { get; set; }

        /// <summary>
        ///     Article activity logs
        /// </summary>
        public DbSet<ArticleLog> ArticleLogs { get; set; }

        /// <summary>
        ///     Website layouts
        /// </summary>
        public DbSet<Layout> Layouts { get; set; }

        /// <summary>
        /// Node Scripts
        /// </summary>
        public DbSet<NodeScript> NodeScripts { get; set; }

        /// <summary>
        ///     Web page templates
        /// </summary>
        public DbSet<Template> Templates { get; set; }

        public DbSet<ScriptCatalogEntry> ScriptCatalog { get; set; }

        #endregion
    }
}