using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class DownloadGameProduct : GameProduct
    {
        [DataType(DataType.Url)]
        public string DownloadLink { get; set; }

        [Range(0, Int32.MaxValue)]
        public int ApproximateSizeInMB { get; set; }
    }
}