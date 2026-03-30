using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Dtos.S3;
public class UploadedPartDto
{
    public int PartNumber { get; set; }
    public string ETag { get; set; }
}

