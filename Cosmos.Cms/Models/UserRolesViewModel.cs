namespace Cosmos.Cms.Models
{
    /// <summary>
    /// User role view model
    /// </summary>
    public class UserRolesViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public UserRolesViewModel()
        {
            Administrator = false;
            Editor = false;
            Author = false;
            Reviewer = false;
            NoRole = false;
            RemoveAccount = false;
            TeamMember = false;
        }
        /// <summary>
        /// User email address
        /// </summary>
        public string UserEmailAddress { get; set; }
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// Is an Administrator?
        /// </summary>
        public bool Administrator { get; set; }
        /// <summary>
        /// Is an Editor?
        /// </summary>
        public bool Editor { get; set; }
        /// <summary>
        /// Is an Author?
        /// </summary>
        public bool Author { get; set; }
        /// <summary>
        /// Is a Reviewer?
        /// </summary>
        public bool Reviewer { get; set; }
        /// <summary>
        /// Is a member of a Team?
        /// </summary>
        public bool TeamMember { get; set; }
        /// <summary>
        /// Has no role?
        /// </summary>
        public bool NoRole { get; set; }
        /// <summary>
        /// Remove account from a role
        /// </summary>
        public bool RemoveAccount { get; set; }
        /// <summary>
        /// User role name
        /// </summary>
        public string UserRole { get; set; }
    }
}