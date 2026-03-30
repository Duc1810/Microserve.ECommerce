using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Dtos.S3;
public class CompleteMutipartUpload
{
    public string Key { get; set; }
    public string UploadId { get; set; }
    public List<UploadedPartDto> Parts { get; set; }
}

