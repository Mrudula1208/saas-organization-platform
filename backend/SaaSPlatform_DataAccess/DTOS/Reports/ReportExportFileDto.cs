using System;

namespace SaaSPlatform.Application.DTOS.Reports
{
    /// <summary>A generated report file ready to be returned as a download.</summary>
    public class ReportExportFileDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileName { get; set; } = "report";
    }
}
