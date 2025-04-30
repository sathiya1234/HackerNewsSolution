namespace HackerNewsAPI.Core.Models
{
    /// <summary>
    /// Represents a paginated story result set
    /// </summary>
    public class StoryModel
    {
        /// <summary>
        /// Current page of stories
        /// </summary>
        /// <example>[{ "id": 123, "title": "Sample Story", "url": "https://example.com" }]</example>
        public List<Story> Stories { get; set; } = new List<Story>();

        /// <summary>
        /// Total number of stories available (before pagination)
        /// </summary>
        /// <example>150</example>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Represents a Hacker News story
    /// </summary>
    public class Story
    {
        /// <summary>
        /// Unique story identifier
        /// </summary>
        /// <example>12345</example>
        public int Id { get; set; }

        /// <summary>
        /// Story title
        /// </summary>
        /// <example>Breaking News in Technology</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional URL to full story
        /// </summary>
        /// <example>https://example.com/news</example>
        public string Url { get; set; } = string.Empty;
    }
}
