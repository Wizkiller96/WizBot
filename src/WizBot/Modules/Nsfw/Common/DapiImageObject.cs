﻿using System.Text.Json.Serialization;

namespace WizBot.Modules.Nsfw.Common
{
    public class DapiImageObject : IImageData
    {
        [JsonPropertyName("File_Url")]
        public string FileUrl { get; set; }
        public string Tags { get; set; }
        [JsonPropertyName("Tag_String")]
        public string TagString { get; set; }
        public int Score { get; set; }
        public string Rating { get; set; }
        
        public ImageData ToCachedImageData(Booru type)
            => new ImageData(this.FileUrl, type, this.Tags?.Split(' ') ?? this.TagString?.Split(' '), Score.ToString() ?? Rating);
    }
}