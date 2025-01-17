﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Core.Data;
using LMPlatform.Data.Repositories;
using LMPlatform.Models;

namespace Application.Infrastructure.FilesManagement
{
    public class FilesManagementService : IFilesManagementService
    {
        private readonly string _storageRoot = ConfigurationManager.AppSettings["FileUploadPath"];
        private readonly string _tempStorageRoot = ConfigurationManager.AppSettings["FileUploadPathTemp"];

        public string GetFileDisplayName(string guid)
        {
	        using var repositoriesContainer = new LmPlatformRepositoriesContainer();
	        return repositoriesContainer.AttachmentRepository.GetBy(new Query<Attachment>(e => e.FileName == guid)).Name;
        }

		public string GetPathName(string guid)
		{
			using var repositoriesContainer = new LmPlatformRepositoriesContainer();
			return repositoriesContainer.AttachmentRepository.GetBy(new Query<Attachment>(e => e.FileName == guid)).PathName;
		}

        public long? GetFileSize(Attachment attachment) 
        {
            var filePath = Path.Combine(_storageRoot, attachment.PathName, attachment.FileName);

            if (File.Exists(filePath)) 
            {
                return new FileInfo(filePath).Length;
            }

            return null;
        }

        public void SaveFiles(IEnumerable<Attachment> attachments, string folder = "")
        {
            foreach (var attach in attachments)
            {
                SaveFile(attach, folder);
            }
        }

        public void SaveFiles(IEnumerable<Attachment> attachments, Func<Attachment, string> property)
        {
            foreach (var attach in attachments)
            {
                SaveFile(attach, property.Invoke(attach));
            }
        }

        public IList<Attachment> GetAttachments(string path)
        {
            using var repositoriesContainer = new LmPlatformRepositoriesContainer();
            return string.IsNullOrEmpty(path) ? repositoriesContainer.AttachmentRepository.GetAll().ToList()
                : repositoriesContainer.AttachmentRepository.GetAll(new Query<Attachment>(e => e.PathName == path)).ToList()
                .Where(attachemnt => File.Exists(_storageRoot + $"{attachemnt.PathName}//{attachemnt.FileName}"))
                .ToList();
        }

        public IList<Attachment> GetAttachments(string filter, int filesPerPage, int page)
        {
	        var isFilterEmpty = string.IsNullOrWhiteSpace(filter);

            using var repositoriesContainer = new LmPlatformRepositoriesContainer();
	        return repositoriesContainer.AttachmentRepository
		        .GetAll(new Query<Attachment>(a => isFilterEmpty || a.Name.ToLower().Contains(filter.ToLower())))
		        .OrderBy(a => a.Name)
		        .Skip(filesPerPage * (page - 1))
		        .Take(filesPerPage)
		        .ToList();
        }



        public void DeleteFileAttachment(Attachment attachment)
        {
            var filePath = _storageRoot + attachment.PathName + "//" + attachment.FileName;
            using var repositoriesContainer = new LmPlatformRepositoriesContainer();
            repositoriesContainer.AttachmentRepository.Delete(attachment);
            repositoriesContainer.ApplyChanges();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void DeleteFileAttachment(string pathName, string fileName)
        {
            using var repositoriesContainer = new LmPlatformRepositoriesContainer();
            var attachment = repositoriesContainer.AttachmentRepository.GetBy(new Query<Attachment>(x => x.PathName == pathName && x.FileName == fileName));
            var tempFilePath = Path.Combine(_tempStorageRoot, attachment?.FileName ?? fileName);
            if (attachment != null)
            {
                DeleteFileAttachment(attachment);
            } else if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        public void SaveFile(Attachment attachment, string folder)
        {
            var targetDirectoty = Path.Combine(_storageRoot, folder);
            var tempFilePath = Path.Combine(_tempStorageRoot, attachment.FileName);
            var targetFilePath = Path.Combine(targetDirectoty, attachment.FileName);

            if (!Directory.Exists(targetDirectoty))
            {
                Directory.CreateDirectory(targetDirectoty);
            }

            if (!File.Exists(tempFilePath)) return;
            File.Copy(tempFilePath, targetFilePath, true);
            File.Delete(tempFilePath);
        }

        public string GetFullPath(Attachment attachment)
        {
            return _storageRoot + attachment.PathName + "//" + attachment.FileName;
        }

        public IList<Attachment> GetAttachmentsByIds(IEnumerable<int> ids)
        {
            using var repositoriesContainer = new LmPlatformRepositoriesContainer();

            return repositoriesContainer.AttachmentRepository.GetAll(new Query<Attachment>(x => ids.Contains(x.Id))).ToList();
        }
    }
}