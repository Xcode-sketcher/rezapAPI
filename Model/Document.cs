using System.ComponentModel.DataAnnotations;

namespace rezapAPI.Model
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TeamId { get; set; }

        [Required]
        public string OwnerUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string ContentType { get; set; } = "application/octet-stream";

        [Required]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public long Size { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
