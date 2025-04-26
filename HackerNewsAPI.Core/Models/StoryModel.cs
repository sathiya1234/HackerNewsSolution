namespace HackerNewsAPI.Core.Models
{
    /// <summary>
    /// Represents a collection of stories along with the total count.
    /// </summary>
    public class StoryModel
    {
        /// <summary>
        /// Gets or sets the list of stories.
        /// </summary>
        public List<Story> Stories { get; set; } = new List<Story>();

        /// <summary>
        /// Gets or sets the total count of stories.
        /// </summary>
        public int TotalCount { get; set; } = 0;
    }

    /// <summary>
    /// Represents an individual story from Hacker News.
    /// </summary>
    public class Story
    {
        /// <summary>
        /// Gets or sets the unique identifier of the story.
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Gets or sets the title of the story.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the story (optional).
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
