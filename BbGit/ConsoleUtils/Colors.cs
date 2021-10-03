using System.Drawing;
using Pastel;

namespace BbGit.ConsoleUtils
{
    public static class Colors
    {
        public static readonly Color RepoColor = Color.LawnGreen;
        public static readonly Color BranchColor = Color.Yellow;
        public static readonly Color PathColor = Color.LightCoral;
        public static readonly Color DefaultColor = Color.Snow;

        public static string ColorRepo(this string text) => text.Pastel(RepoColor);
        public static string ColorBranch(this string text) => text.Pastel(BranchColor);
        public static string ColorPath(this string text) => text.Pastel(PathColor);
        public static string ColorDefault(this string text) => text.Pastel(DefaultColor);
    }
}