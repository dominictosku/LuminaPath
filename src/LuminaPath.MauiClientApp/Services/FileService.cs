using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Common.Features.Files.Dto;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.MauiClientApp.Services
{
    public class FileService : IStorageService
    {
        public async Task<List<BlobDto>> ListAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<BlobDto> DownloadAsync(string blobFilename)
        {
            throw new NotImplementedException();
        }


        public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
        {
            throw new NotImplementedException();
        }

        public async Task<BlobResponseDto> UploadAsync(Stream blob, string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RenameAsync(string oldName, string newName)
        {
            throw new NotImplementedException();
        }
    }
}
