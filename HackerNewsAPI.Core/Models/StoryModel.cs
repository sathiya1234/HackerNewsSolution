namespace HackerNewsAPI.Core.Models
{
    /// <summary>
    /// Represents a story model from Hacker News
    /// </summary>
    public class StoryModel
    {
        /// <summary>
        /// Unique identifier of the story
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Title of the story
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// URL of the story (optional)
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
