using Family_Business.Models;

namespace Family_Business.Helpers
{
    public static class Session
    {
        public static User? CurrentUser { get; private set; }
        public static void Set(User u) => CurrentUser = u;
        public static bool IsAdmin =>
            CurrentUser?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
    }
}