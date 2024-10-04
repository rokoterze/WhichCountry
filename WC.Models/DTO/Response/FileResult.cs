namespace WC.Models.DTO.Response
{
    public class FileResult
    {
        public string FileName { get; set; } = null!;
        public byte[]? Content { get; set; } = null!;
        private string mimeType = null!;
        
        public string MimeType
        {
            get
            {
                if (!String.IsNullOrEmpty(mimeType))
                {
                    return mimeType;
                }

                string fileExtension = FileName[(FileName.LastIndexOf('.') + 1)..].ToLower();

                mimeType = fileExtension switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "pdf" => "application/pdf",
                    "png" => "image/png",
                    "html" => "text/html",
                    "csv" => "text/csv",
                    "xsl" => "application/vnd.ms-excel",
                    "xslx" => "application/vnd.ms-excel",
                    _ => "text/plain",
                };
                return mimeType;
            }

            set
            {
                mimeType = value;
            }
        }
    }
}
