﻿using CodeChallengeInc.SubmissionApi.Constants;
using CodeChallengeInc.SubmissionApi.Interfaces;
using CodeChallengeInc.SubmissionApi.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CodeChallengeInc.SubmissionApi.Services
{
	public class FileService : IFileService
	{
		public void BackupUserSubmission(string path)
		{
			string content = File.ReadAllText(path);
			FileInfo fi = new FileInfo(path);

			string filename = $"{fi.Name.Replace(FileInformation.LoneAntFileExtension, string.Empty)} - {fi.LastWriteTime.ToString(FileInformation.DateTimeFormat)}{fi.Extension}";
			string backupLocation = Path.Combine(GetBackupsPath(), filename);

			File.WriteAllText(backupLocation, content);
		}

		public void CreateOrOverwriteUserSubmission(string antName, string userName, string submission)
		{
			userName = userName.Replace("_", string.Empty);
			string fileName = $"{userName}_{antName}"; 
			if (UserSubmissionExists(fileName))
			{
				BackupUserSubmission(GetUserSubmissionPath(fileName));
			}

			using (FileStream fs = File.Create(GetUserSubmissionPath(fileName)))
			{
				byte[] content = new UTF8Encoding(true).GetBytes(submission);

				fs.Write(content, 0, content.Length);
			}
		}

		public void DeleteUserSubmission(string antName, string userName)
		{
			string fileName = $"{userName}_{antName}";
			BackupUserSubmission(GetUserSubmissionPath(fileName));
			File.Delete(GetUserSubmissionPath(fileName));
		}

		public bool UserSubmissionExists(string fileName)
		{
			return File.Exists(GetUserSubmissionPath(fileName));
		}

		public LoneAntSubmissionResponse GetUserSubmission(string antName, string userName)
		{
			string filePath = GetUserSubmissionPath($"{userName}_{antName}");
			return new LoneAntSubmissionResponse { Username = userName, Submission = File.ReadAllText(filePath), AntName = antName };
		}
		internal string GetSubmissionsPath()
		{
			return Path.Combine(FileInformation.LoneAntFolder, FileInformation.SubmissionFolder);
		}

		internal string GetBackupsPath()
		{
			return Path.Combine(FileInformation.LoneAntFolder, FileInformation.BackupSubmissionFolder);
		}

		internal string GetUserSubmissionPath(string fileName)
		{
			if (fileName.Contains(FileInformation.LoneAntFileExtension)) return Path.Combine(GetSubmissionsPath(), fileName);
			return Path.Combine(GetSubmissionsPath(), fileName + FileInformation.LoneAntFileExtension);
		}

		public List<LoneAntSubmissionResponse> GetSubmissionsJson()
		{
			List<LoneAntSubmissionResponse> submissions = new List<LoneAntSubmissionResponse>();
			foreach(string submissionName in GetSubmissionNames())
			{
				ExtractUsernameAndAntNameFromSubmissionName(submissionName, out string userName, out string antName);
				submissions.Add(GetUserSubmission(antName, userName));
			}

			return submissions;
		}

		public List<string> GetSubmissionNames()
		{
			List<string> submissionNames = new List<string>();
			
			foreach (string submissionPath in Directory.EnumerateFiles(GetSubmissionsPath()))
			{
				string submissionName = ExtractSubmissionNameFromPath(submissionPath);
				
				submissionNames.Add(submissionName.Replace(FileInformation.LoneAntFileExtension, string.Empty));
			}

			return submissionNames;
		}

		public void PurgeDefaultAnts()
		{
			foreach(string submissionPath in Directory.EnumerateFiles(GetSubmissionsPath()))
			{
				string submissionName = ExtractSubmissionNameFromPath(submissionPath);
				ExtractUsernameAndAntNameFromSubmissionName(submissionName, out string userName, out string antName);
				string submissionText = GetUserSubmission(antName, userName).Submission;
				if (submissionText.Equals(FileInformation.DefaultAntString))
				{
					DeleteUserSubmission(antName, userName);
				}
			}
		}

		internal string ExtractSubmissionNameFromPathForWindows(string path)
		{
			return path.Replace($"{GetSubmissionsPath()}\\", string.Empty);
		}

		internal string ExtractSubmissionNameFromPathForLinux(string path)
		{
			return path.Replace(FileInformation.DockerFilePathPrefix, string.Empty);
		}

		internal string ExtractSubmissionNameFromPath(string path)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return ExtractSubmissionNameFromPathForWindows(path);
			}
			else
			{
				return ExtractSubmissionNameFromPathForLinux(path);
			}
		}

		internal void ExtractUsernameAndAntNameFromSubmissionName(string submissionName, out string userName, out string antName)
		{
			List<string> fileNameParts = submissionName.Split('_').ToList();
			userName = fileNameParts[0];
			fileNameParts.RemoveAt(0);
			antName = fileNameParts.Join("_");
		}
	}
}
