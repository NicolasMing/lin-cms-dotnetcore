﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LinCms.Application.Contracts.Cms.Files;
using LinCms.Application.Contracts.Cms.Files.Dtos;
using LinCms.Core.Common;
using LinCms.Core.Entities;
using LinCms.Core.Exceptions;
using LinCms.Core.IRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LinCms.Application.Cms.Files
{
    public class LocalFileService : IFileService
    {
        private readonly IWebHostEnvironment _hostingEnv;
        private readonly IAuditBaseRepository<LinFile> _fileRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public LocalFileService(IWebHostEnvironment hostingEnv, IConfiguration configuration, IHttpContextAccessor contextAccessor, IAuditBaseRepository<LinFile> fileRepository)
        {
            _hostingEnv = hostingEnv;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _fileRepository = fileRepository;
        }

        /// <summary>
        /// 本地文件上传，生成目录格式 /{STORE_DIR}/{year}/{month}/{day}/{guid}.文件后缀
        /// </summary>
        /// <param name="file"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private string LocalUpload(IFormFile file, out long len)
        {
            if (file.Length == 0)
            {
                throw new LinCmsException("文件为空");
            }
            string newSaveName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string fileDir = _configuration[LinConsts.File.STORE_DIR];

            string path = Path.Combine(fileDir, DateTime.Now.ToString("yyy/MM/dd"), newSaveName);

            string savePath = Path.Combine(_hostingEnv.WebRootPath, path);
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            using (FileStream fs = File.Create(Path.Combine(savePath, newSaveName)))
            {
                file.CopyTo(fs);
                len = fs.Length;
                fs.Flush();
            }

            return path.Replace("\\", "/");
        }

        /// <summary>
        /// 本地文件上传，秒传（根据lin_file表中的md5,与当前文件的路径是否在本地），如果不在，重新上传，覆盖文件表记录
        /// </summary>
        /// <param name="file"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<FileDto> UploadAsync(IFormFile file, int key = 0)
        {
            string md5 = LinCmsUtils.GetHash<MD5>(file.OpenReadStream());
            LinFile linFile = await _fileRepository.Where(r => r.Md5 == md5 && r.Type == 1).OrderByDescending(r => r.CreateTime).FirstAsync();

            HttpRequest request = _contextAccessor.HttpContext.Request;
            string Host = $"{request.Scheme}://{request.Host}";

            if (linFile != null && File.Exists(Path.Combine(_hostingEnv.WebRootPath, linFile.Path)))
            {
                return new FileDto
                {
                    Id = linFile.Id,
                    Key = "file_" + key,
                    Path = linFile.Path,
                    Url = Host + "/" + linFile.Path
                };
            }

            long id;

            string path = this.LocalUpload(file, out long len);

            if (linFile == null)
            {
                LinFile saveLinFile = new LinFile()
                {
                    Extension = Path.GetExtension(file.FileName),
                    Md5 = md5,
                    Name = file.FileName,
                    Path = path,
                    Type = 1,
                    Size = len
                };
                id = (await _fileRepository.InsertAsync(saveLinFile)).Id;
            }
            else
            {
                await _fileRepository.UpdateDiy.Set(a => a.Path, path).ExecuteAffrowsAsync();
                id = linFile.Id;
            }

            return new FileDto
            {
                Id = id,
                Key = "file_" + key,
                Path = path,
                Url = Host + "/" + path
            };

        }
    }
}
